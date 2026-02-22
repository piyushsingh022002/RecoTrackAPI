using Hangfire;
using Microsoft.AspNetCore.Mvc;
using RecoTrack.Application.Models.Users;
using RecoTrack.Application.Models.AuthProviders;
using RecoTrack.Infrastructure.Services.GoogleAuthService;
using RecoTrackApi.DTOs;
using RecoTrackApi.Jobs;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Repositories;
using RecoTrackApi.Services;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace RecoTrackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IBackgroundJobClient _backgroundJob;
        private readonly ILogRepository _logRepository;
        private readonly GoogleAuthService? _googleAuthService;
        private readonly IUserRepository? _userRepository;
        private readonly IRefreshTokenRepository? _refreshTokenRepository;

        public AuthController(
            IAuthService authService,
            ILogRepository logRepository,
            IBackgroundJobClient backgroundJob,
            GoogleAuthService? googleAuthService = null,
            IUserRepository? userRepository = null,
            IRefreshTokenRepository? refreshTokenRepository = null)
        {
            _authService = authService;
            _logRepository = logRepository;
            _backgroundJob = backgroundJob;
            _googleAuthService = googleAuthService;
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
        }

        private void SetRefreshCookie(string refreshToken, DateTime expires)
        {
            var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = expires,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Path = "/api/auth/refresh"
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        private void ClearRefreshCookie()
        {
            Response.Cookies.Delete("refreshToken", new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict,
                Path = "/api/auth/refresh"
            });
        }

        private static string ComputeSha256Hash(string raw)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return BitConverter.ToString(bytes).Replace("-", string.Empty).ToLowerInvariant();
        }

        private async Task<(string token, DateTime expires, string hash)?> GenerateAndSaveRefreshTokenAsync(string userId)
        {
            if (_refreshTokenRepository == null)
                return null;
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var expires = DateTime.UtcNow.AddDays(7);
            var hash = ComputeSha256Hash(refreshToken);
            var entry = new RefreshTokenEntry
            {
                UserId = userId,
                TokenHash = hash,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = expires,
                Revoked = false
            };
            await _refreshTokenRepository.SaveAsync(entry);
            return (refreshToken, expires, hash);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest? request)
        {
            if (request is null)
                return BadRequest(new AuthResponseDto { Message = "Request body cannot be null" });

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
                return BadRequest(new AuthResponseDto { Message = result.ErrorMessage ?? "failure" });

            if (!string.IsNullOrWhiteSpace(result.Token))
            {
                _backgroundJob.Enqueue<WelcomeEmailJob>(job => job.SendEmailAsync(result.Token, "WELCOME"));
            }

            // generate and set refresh token cookie if repo available
            if (_refreshTokenRepository != null && result.User != null)
            {
                var maybe = await GenerateAndSaveRefreshTokenAsync(result.User.Id);
                if (maybe.HasValue)
                {
                    SetRefreshCookie(maybe.Value.token, maybe.Value.expires);
                }
            }

            return Ok(new AuthResponseDto
            {
                Token = result.Token,
                Message = "success"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest? request)
        {
            if (request is null)
                return BadRequest(new AuthResponseDto { Message = "Request body cannot be null" });

            var result = await _authService.LoginAsync(request);

            // If MFA is required, the service returns a LoginResult with ErrorMessage == "MFA_REQUIRED"
            if (result.ErrorMessage == "MFA_REQUIRED")
            {
                // enqueue MFA OTP email job using user's email and otp
                if (!string.IsNullOrWhiteSpace(result.Email) && !string.IsNullOrWhiteSpace(result.Otp))
                {
                    _backgroundJob.Enqueue<MfaOtpEmailJob>(job => job.SendMfaOtpEmailAsync(result.Email, result.Username ?? string.Empty, result.Otp));
                }

                // Return200 with message MFA_REQUIRED and provide temp token in Token field to client
                return Ok(new AuthResponseDto { Token = result.Token, Message = "MFA_REQUIRED" });
            }

            if (!result.Success)
                return Unauthorized(new AuthResponseDto { Message = result.ErrorMessage ?? "failure" });

            // set refresh cookie if repo available and user id can be resolved from token
            if (_refreshTokenRepository != null)
            {
                // extract user id claim from jwt token (result.Token)
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(result.Token);
                var userId = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var maybe = await GenerateAndSaveRefreshTokenAsync(userId);
                    if (maybe.HasValue)
                    {
                        SetRefreshCookie(maybe.Value.token, maybe.Value.expires);
                    }
                }
            }

            return Ok(new AuthResponseDto
            {
                Token = result.Token,
                Message = "success"
            });
        }

        [HttpPost("verify-mfa")]
        public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest? request)
        {
            if (request is null)
                return BadRequest(new AuthResponseDto { Message = "Request body cannot be null" });

            if (string.IsNullOrWhiteSpace(request.TempToken) || string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest(new AuthResponseDto { Message = "TempToken and Otp are required" });

            var result = await _authService.VerifyMfaAsync(request.TempToken, request.Otp);
            if (!result.Success)
                return Unauthorized(new AuthResponseDto { Message = result.ErrorMessage ?? "failure" });

            if (_refreshTokenRepository != null)
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(result.Token);
                var userId = jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    var maybe = await GenerateAndSaveRefreshTokenAsync(userId);
                    if (maybe.HasValue)
                    {
                        SetRefreshCookie(maybe.Value.token, maybe.Value.expires);
                    }
                }
            }

            return Ok(new AuthResponseDto { Token = result.Token, Message = "success" });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (_refreshTokenRepository == null || _userRepository == null)
            {
                return StatusCode(501, new { Message = "Refresh token functionality is not configured." });
            }

            var refreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Unauthorized(new { Message = "Refresh token missing." });

            var hash = ComputeSha256Hash(refreshToken);
            var entry = await _refreshTokenRepository.GetByTokenHashAsync(hash);
            if (entry == null || entry.Revoked || entry.ExpiresAtUtc < DateTime.UtcNow)
            {
                ClearRefreshCookie();
                return Unauthorized(new { Message = "Invalid refresh token." });
            }

            var user = await _userRepository.GetByIdAsync(entry.UserId);
            if (user == null)
            {
                ClearRefreshCookie();
                return Unauthorized(new { Message = "User not found for refresh token." });
            }

            var newJwt = _authService.GenerateJwtToken(user);

            // rotate refresh token
            var newTokenInfo = await GenerateAndSaveRefreshTokenAsync(user.Id);
            if (!newTokenInfo.HasValue)
            {
                return StatusCode(500, new { Message = "Failed to generate refresh token." });
            }

            await _refreshTokenRepository.RevokeAsync(entry.TokenHash, newTokenInfo.Value.hash);
            SetRefreshCookie(newTokenInfo.Value.token, newTokenInfo.Value.expires);

            return Ok(new { Token = newJwt });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (_refreshTokenRepository == null)
            {
                ClearRefreshCookie();
                return Ok(new { Message = "Logged out" });
            }

            var refreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var hash = ComputeSha256Hash(refreshToken);
                await _refreshTokenRepository.RevokeAsync(hash, null);
            }

            ClearRefreshCookie();
            return Ok(new { Message = "Logged out" });
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequest request)
        {
            if (_googleAuthService == null || _userRepository == null)
            {
                return StatusCode(501, new AuthResponseDto { Message = "Google authentication is not configured." });
            }

            if (request is null || string.IsNullOrWhiteSpace(request.AccessToken))
                return BadRequest(new AuthResponseDto { Message = "AccessToken is required" });

            Google.Apis.Auth.GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await _googleAuthService.VerifyToken(request.AccessToken);
            }
            catch (System.Exception)
            {
                return Unauthorized(new AuthResponseDto { Message = "Invalid Google token" });
            }

            if (payload == null || string.IsNullOrWhiteSpace(payload.Email))
                return Unauthorized(new AuthResponseDto { Message = "Unable to verify Google user" });

            // make a non-nullable local copy of email to satisfy the compiler
            var email = payload.Email!.Trim();

            // check existing user by email
            var user = await _userRepository.GetByEmailAsync(email);

            if (user == null)
            {
                // create a new user consistent with RegisterAsync model
                // Use given_name for username and name for full name. Fallback to email local-part for username.
                var username = !string.IsNullOrWhiteSpace(payload.GivenName)
                ? payload.GivenName
                : (email.Split('@')[0] ?? "user");

                // generate a strong random password and its hash
                var randomBytes = RandomNumberGenerator.GetBytes(18); //18 bytes ->24 base64 chars
                var strongPlain = System.Convert.ToBase64String(randomBytes);
                var hashedTemp = _authService.HashPassword(strongPlain);

                user = new User
                {
                    Username = username,
                    FullName = string.IsNullOrWhiteSpace(payload.Name) ? username : payload.Name,
                    Email = email,
                    PhoneNumber = string.Empty,
                    Dob = System.DateTime.UtcNow,
                    PasswordHash = hashedTemp,
                    IsOAuthUser = true,
                    Profile = new UserProfile { AvatarUrl = payload.Picture },
                    AuthProviders = new System.Collections.Generic.List<AuthProvider>
                    {
                        new AuthProvider
                        {
                            Provider = "google",
                            ProviderUserId = payload.Subject ?? string.Empty
                        }
                    },
                    Status = UserStatus.Active
                };

                await _userRepository.CreateUserAsync(user);

                // enqueue welcome email job for Google-registered users with auto-generated password
                _backgroundJob.Enqueue<SendGoogleUserJob>(job => job.SendGoogleUserAsync(email, strongPlain, username, "GOOGLE_WELCOME"));

                // generate JWT for the newly created user and return it
                var token = _authService.GenerateJwtToken(user);
                // generate refresh token via repo if available
                if (_refreshTokenRepository != null)
                {
                    var maybe = await GenerateAndSaveRefreshTokenAsync(user.Id);
                    if (maybe.HasValue)
                        SetRefreshCookie(maybe.Value.token, maybe.Value.expires);
                }

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Message = "success"
                });
            }
            else
            {
                // update avatar if available
                if (!string.IsNullOrWhiteSpace(payload.Picture))
                {
                    await _userRepository.UpdateAvatarUrlAsync(user.Id, payload.Picture);
                }

                // existing user -> generate JWT using existing auth service
                var token = _authService.GenerateJwtToken(user);
                if (_refreshTokenRepository != null)
                {
                    var maybe = await GenerateAndSaveRefreshTokenAsync(user.Id);
                    if (maybe.HasValue)
                        SetRefreshCookie(maybe.Value.token, maybe.Value.expires);
                }

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Message = "success"
                });
            }
        }


        [HttpDelete("clearMongoLogs")]
        public async Task<IActionResult> ClearLogs()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            if (userName == "Piyush Singh")
            {
                await _logRepository.ClearLogsAsync();
                return Ok(new { message = "Logs cleared successfully." });
            }

            return Forbid();
        }

        [Authorize]
        [HttpPost("mfa/toggle")]
        public async Task<IActionResult> ToggleMfa([FromBody] ToggleMfaRequest? request)
        {
            if (request is null)
                return BadRequest(new { Message = "Request body cannot be null" });

            if (_userRepository == null)
                return StatusCode(501, new { Message = "User repository not configured." });

            // Try several claim types to resolve the user id
            var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value
                 ?? User.FindFirst(ClaimTypes.Email)?.Value
                 ?? User.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrWhiteSpace(claimValue))
                return Unauthorized(new { Message = "Unable to resolve user from token." });

            string? userId = claimValue;

            // If claim looks like an email, resolve actual user id from repository
            if (claimValue.Contains("@"))
            {
                var user = await _userRepository.GetByEmailAsync(claimValue);
                if (user == null)
                    return Unauthorized(new { Message = "User not found from token claim." });

                userId = user.Id;
            }

            try
            {
                await _userRepository.UpdateIsMfaEnabledAsync(userId!, request.Enabled);
                return Ok(new { Message = "MFA setting updated.", IsMfaEnabled = request.Enabled });
            }
            catch (System.Exception)
            {
                // do not leak internal exception details
                return StatusCode(500, new { Message = "Failed to update MFA setting." });
            }
        }
    }

    public class VerifyMfaRequest
    {
        public string TempToken { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class ToggleMfaRequest
    {
        public bool Enabled { get; set; }
    }
}