using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;
using RecoTrackApi.Jobs;
using System.Security.Claims;
using System.Net.Mail;

namespace RecoTrackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;
        private readonly ILogRepository _logRepository;
        private readonly IBackgroundJobClient _backgroundJob;

        public AuthController(
            IUserRepository userRepository,
            IAuthService authService,
            ILogger<AuthController> logger,
            ILogRepository logRepository,
            IBackgroundJobClient backgroundJob)
        {
            _userRepository = userRepository;
            _authService = authService;
            _logger = logger;
            _logRepository = logRepository;
            _backgroundJob = backgroundJob;
        }

        [Authorize]
        [HttpGet("user")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;

            return Ok(new { userId, email, name });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest? request)
        {
            if (request is null)
                return BadRequest(new { Message = "Request body cannot be null" });

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
                return BadRequest(new { Message = result.ErrorMessage });

            if (!string.IsNullOrWhiteSpace(result.Token))
            {
                _backgroundJob.Enqueue<WelcomeEmailJob>(job => job.SendEmailAsync(result.Token, "WELCOME"));
            }

            return Ok(result);

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request is null)
                return BadRequest(new { Message = "Request body cannot be null" });

            var result = await _authService.LoginAsync(request);

            if (!result.Success)
                return Unauthorized(new { Message = result.ErrorMessage });

            return Ok(new
            {
                Token = result.Token,
                Username = result.Username,
                Email = result.Email
            });
        }

        [HttpPost("password/send-otp")]
        public async Task<IActionResult> SendPasswordResetOtp([FromBody] PasswordResetRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { Message = "Email is required." });

            if (!IsValidEmail(request.Email))
                return BadRequest(new { Message = "Email is not valid" });

            try
            {
                var result = await _authService.SendPasswordResetOtpAsync(request.Email);
                return Ok(new
                {
                    result.Message,
                    result.ExpiresAtUtc
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Password reset requested for missing email {Email}", request.Email);
                return NotFound(new { Message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate OTP for {Email}", request.Email);
                return StatusCode(500, "An unexpected error occurred while generating the OTP.");
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                _ = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        //Clearing the Logs 
        [HttpDelete("clearMongoLogs")]
        public async Task<IActionResult> ClearLogs()
        {
            //only user with UserName Piyush Singh
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            if (userName == "Piyush Singh")
            {
                await _logRepository.ClearLogsAsync();
                return Ok(new { message = "Logs cleared successfully." });
            }

            return Forbid(); // 403 if not authorized
        }

        [HttpPost("password/verify-otp")]
        public async Task<IActionResult> VerifyPasswordResetOtp([FromBody] VerifyPasswordResetOtpRequest? request)
        {
            if (request is null)
                return BadRequest(new { Message = "Request body cannot be null." });

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest(new { Message = "Email and OTP are required." });

            if (!IsValidEmail(request.Email))
                return BadRequest(new { Message = "Email is not valid." });

            try
            {
                var result = await _authService.VerifyPasswordResetOtpAsync(request.Email, request.Otp);
                if (!result.Success)
                    return BadRequest(new { Message = result.Message });

                return Ok(new { result.Message, result.SuccessCode });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OTP verification failed for {Email}", request.Email);
                return StatusCode(500, "An unexpected error occurred while verifying the OTP.");
            }
        }

        [HttpPost("password/reset")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.SuccessCode))
                return BadRequest(new { Message = "Email and success code are required." });

            if (string.IsNullOrWhiteSpace(request.NewPassword) || string.IsNullOrWhiteSpace(request.ConfirmPassword))
                return BadRequest(new { Message = "New password and confirmation are required." });

            if (request.NewPassword != request.ConfirmPassword)
                return BadRequest(new { Message = "Passwords do not match." });

            if (!IsValidEmail(request.Email))
                return BadRequest(new { Message = "Email is not valid." });

            try
            {
                var result = await _authService.ResetPasswordAsync(request);
                if (!result.Success)
                    return BadRequest(new { Message = result.ErrorMessage });

                return Ok(new
                {
                    Token = result.Token,
                    Username = result.Username,
                    Email = result.Email
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password reset failed for {Email}", request.Email);
                return StatusCode(500, "An unexpected error occurred while resetting the password.");
            }
        }
    }
}
// End of file: Controllers/AuthController.cs
// This file contains the AuthController class which handles user registration and login.
// It uses IUserRepository to interact with the user data and IAuthService for password hashing and JWT token generation.
// The controller provides endpoints for registering a new user and logging in an existing user.