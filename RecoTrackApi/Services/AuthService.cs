using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories;
using RecoTrackApi.Repositories.Interfaces;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using RecoTrack.Application.Models.Users;
using RecoTrack.Application.Models.AuthProviders;

namespace RecoTrackApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly string _jwtKey;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordResetRepository _passwordResetRepository;
        private readonly ISecurityQuestionRepository _securityQuestionRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration,
            IUserRepository userRepository,
            IPasswordResetRepository passwordResetRepository,
            ISecurityQuestionRepository securityQuestionRepository,
            ILogger<AuthService> logger)
        {
            _jwtKey = configuration["JwtSettings:SecretKey"]!;
            _issuer = configuration["JwtSettings:Issuer"] ?? "RecoTrackAPI - Service";
            _audience = configuration["JwtSettings:Audience"] ?? "RecoTrackWeb - Client";

            _userRepository = userRepository;
            _passwordResetRepository = passwordResetRepository;
            _securityQuestionRepository = securityQuestionRepository;
            _logger = logger;
        }

        public async Task<RegisterResult> RegisterAsync(RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                return RegisterResult.Fail("Passwords do not match");

            if (string.IsNullOrWhiteSpace(request.FullName))
                return RegisterResult.Fail("Full name is required");

            if (string.IsNullOrWhiteSpace(request.Username))
                return RegisterResult.Fail("Username is required");

            if (string.IsNullOrWhiteSpace(request.Email))
                return RegisterResult.Fail("Email is required");

            if (request.Dob == default)
                return RegisterResult.Fail("Date of birth is required");

            var email = request.Email.Trim();
            var username = request.Username.Trim();

            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
                return RegisterResult.Fail("Email is already registered");

            var now = DateTime.UtcNow;
            var user = new User
            {
                Username = username,
                FullName = request.FullName.Trim(),
                Email = email,
                PhoneNumber = request.PhoneNumber?.Trim() ?? string.Empty,
                Dob = request.Dob,
                PasswordHash = HashPassword(request.Password),

                // Explicitly mark as a non-OAuth user and initialize related fields
                IsOAuthUser = false,
                AuthProviders = new List<AuthProvider>
                {
                    new AuthProvider
                    {
                        Provider = "register_api",
                        ProviderUserId = email
                    }
                },

                // Ensure profile exists (avatar may be null/empty for standard registration)
                Profile = new UserProfile { AvatarUrl = null },

                // status
                Status = UserStatus.Active,

                // timestamps
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.CreateUserAsync(user);

            var securityEntry = new SecurityQuestionEntry
            {
                UserId = user.Id,
                Question = string.IsNullOrWhiteSpace(request.SecurityQuestion) ? "SecretCode" : request.SecurityQuestion.Trim(),
                AnswerHash = HashPassword(string.IsNullOrWhiteSpace(request.SecurityAnswer) ? request.FullName.Trim() : request.SecurityAnswer.Trim())
            };

            await _securityQuestionRepository.SaveAsync(securityEntry);

            var token = GenerateJwtToken(user);

            _logger.LogInformation("User registered with ID: {UserId}", user.Id);

          
            return RegisterResult.Ok(token);
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

        public async Task<PasswordOtpResult> SendPasswordResetOtpAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                throw new InvalidOperationException("Email not found. Please register first.");

            await _passwordResetRepository.DeactivateActiveOtpsAsync(email);

            var otp = GenerateNumericOtp();
            var entry = new PasswordResetEntry
            {
                Email = email,
                Otp = otp,
                Active = 1,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
            };

            await _passwordResetRepository.SaveAsync(entry);

            return new PasswordOtpResult
            {
                Message = "OTP generated and sent to email",
                Otp = otp,
                ExpiresAtUtc = entry.ExpiresAtUtc
            };
        }

        public async Task<PasswordOtpVerificationResult> VerifyPasswordResetOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required", nameof(email));

            if (string.IsNullOrWhiteSpace(otp))
                throw new ArgumentException("OTP is required", nameof(otp));

            var entry = await _passwordResetRepository.GetActiveUnexpiredEntryAsync(email, otp);
            if (entry == null)
            {
                return new PasswordOtpVerificationResult
                {
                    Success = false,
                    Message = "Invalid or inactive OTP."
                };
            }

            if (entry.ExpiresAtUtc < DateTime.UtcNow)
            {
                return new PasswordOtpVerificationResult
                {
                    Success = false,
                    Message = "OTP has expired."
                };
            }

            var successCode = GenerateSuccessCode();
            await _passwordResetRepository.SetSuccessCodeAsync(email, otp, successCode);

            return new PasswordOtpVerificationResult
            {
                Success = true,
                Message = "OTP verified successfully.",
                SuccessCode = successCode
            };
        }

        public async Task<LoginResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.SuccessCode))
                return LoginResult.Fail("Email and success code are required.");

            if (string.IsNullOrWhiteSpace(request.NewPassword) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
                return LoginResult.Fail("New password and confirmation are required.");

            if (request.NewPassword != request.ConfirmPassword)
                return LoginResult.Fail("Passwords do not match.");

            var entry = await _passwordResetRepository.GetBySuccessCodeAsync(request.Email, request.SuccessCode);
            if (entry == null || entry.ExpiresAtUtc < DateTime.UtcNow)
                return LoginResult.Fail("Invalid or expired success code.");

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return LoginResult.Fail("User not found.");

            var newHash = HashPassword(request.NewPassword);
            await _userRepository.UpdatePasswordHashAsync(request.Email, newHash);
            user.PasswordHash = newHash;

            await _passwordResetRepository.DeactivateActiveOtpsAsync(request.Email);

            var token = GenerateJwtToken(user);
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

        private static string GenerateNumericOtp(int length = 6)
        {
            var maxValue = (int)Math.Pow(10, length);
            var number = RandomNumberGenerator.GetInt32(0, maxValue);
            return number.ToString($"D{length}");
        }

        private static string GenerateSuccessCode()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}