using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RecoTrackApi.Jobs;
using RecoTrackApi.Models;
using RecoTrackApi.Services;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace RecoTrackApi.Controllers
{
    [ApiController]
    [Route("api/password")]
    public class PasswordController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<PasswordController> _logger;
        private readonly IBackgroundJobClient _backgroundJob;

        public PasswordController(
            IAuthService authService,
            ILogger<PasswordController> logger,
            IBackgroundJobClient backgroundJob)
        {
            _authService = authService;
            _logger = logger;
            _backgroundJob = backgroundJob;
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendPasswordResetOtp([FromBody] PasswordResetRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Email))
                return BadRequest(new { Message = "Email is required." });

            if (!IsValidEmail(request.Email))
                return BadRequest(new { Message = "Email is not valid" });

            try
            {
                var result = await _authService.SendPasswordResetOtpAsync(request.Email);

                if (!string.IsNullOrWhiteSpace(result.Otp))
                {
                    _backgroundJob.Enqueue<PasswordResetOtpEmailJob>(job => job.SendOtpEmailAsync(request.Email, result.Otp));
                }

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

        [HttpPost("verify-otp")]
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

        [HttpPost("reset")]
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
    }
}
