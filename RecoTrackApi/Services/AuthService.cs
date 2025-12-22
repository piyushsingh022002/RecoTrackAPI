using Microsoft.IdentityModel.Tokens;
using RecoTrackApi.Controllers;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RecoTrackApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly string _jwtKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration, IUserRepository userRepository, ILogger<AuthService> logger)
        {
            _jwtKey = configuration["JwtSettings:SecretKey"]!;
            // Read issuer and audience from configuration; fall back to existing literals if not present
            _issuer = configuration["JwtSettings:Issuer"] ?? "RecoTrackAPI - Service";
            _audience = configuration["JwtSettings:Audience"] ?? "RecoTrackWeb - Client";

            //Console.WriteLine("GENERATOR JWT KEY: " + _jwtKey);
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<RegisterResult> RegisterAsync (RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                return RegisterResult.Fail("Passwords do not match");

            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return RegisterResult.Fail("Email is already registered");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password)
            };

            await _userRepository.CreateUserAsync(user);
            var token = GenerateJwtToken(user);

            _logger.LogInformation("User registered with ID: {UserId}", user.Id);

            var newUser = new User
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email
            };
            return RegisterResult.Ok(newUser, token);
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return LoginResult.Fail("Invalid credentials");

            if (!VerifyPassword(request.Password, user.PasswordHash))
                return LoginResult.Fail("Invalid credentials");

            var token = GenerateJwtToken(user);

            _logger.LogInformation("User logged in: {Email}", user.Email);

            return LoginResult.SuccessResult(token, user.Username, user.Email);
        }


        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}