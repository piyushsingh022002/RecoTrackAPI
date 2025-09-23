using Microsoft.IdentityModel.Tokens;
using StudentRoutineTrackerApi.Controllers;
using StudentRoutineTrackerApi.Models;
using StudentRoutineTrackerApi.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StudentRoutineTrackerApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly string _jwtKey;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration, IUserRepository userRepository, ILogger<AuthService> logger)
        {
            _jwtKey = configuration["JwtSettings:SecretKey"]!;
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

            _logger.LogInformation("User registered with ID: {UserId}", user.Id);

            return RegisterResult.Ok();
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
                issuer:"RecoTrackAPI",
                audience:"RecoTrackWeb",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}