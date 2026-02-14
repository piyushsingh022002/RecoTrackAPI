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

        public AuthController(
            IAuthService authService,
            ILogRepository logRepository,
            IBackgroundJobClient backgroundJob,
            GoogleAuthService? googleAuthService = null,
            IUserRepository? userRepository = null)
        {
            _authService = authService;
            _logRepository = logRepository;
            _backgroundJob = backgroundJob;
            _googleAuthService = googleAuthService;
            _userRepository = userRepository;
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

            if (!result.Success)
                return Unauthorized(new AuthResponseDto { Message = result.ErrorMessage ?? "failure" });

            return Ok(new AuthResponseDto
            {
                Token = result.Token,
                Message = "success"
            });
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

                // enqueue job to send the plain strong password to user's email
                _backgroundJob.Enqueue<SendGoogleUserJob>(job => job.SendGoogleUserAsync(email, strongPlain, username, "WELCOME_GOOGLE"));

                // generate JWT for the newly created user and return it
                var token = _authService.GenerateJwtToken(user);

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
    }
}