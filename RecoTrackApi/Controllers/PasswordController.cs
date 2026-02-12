using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RecoTrackApi.Jobs;
using RecoTrackApi.Models;
using RecoTrackApi.Services;
using RecoTrackApi.Repositories;
using RecoTrackApi.DTOs;
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
        private readonly IUserRepository _userRepository;

        public PasswordController(
            IAuthService authService,
            ILogger<PasswordController> logger,
            IBackgroundJobClient backgroundJob,
            IUserRepository userRepository)
        {
            _authService = authService;
            _logger = logger;
            _backgroundJob = backgroundJob;
            _userRepository = userRepository;
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

                return Ok(new AuthResponseDto
                {
                    Token = result.Token,
                    Message = "success"
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

        // New endpoint: set password for user created via OAuth
        [HttpPost("set-from-oauth")]
        public async Task<IActionResult> SetPasswordFromOauth([FromBody] SetPasswordFromOAuthRequest? request)
        {
            if (request is null)
                return BadRequest(new { Message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.HashedTempPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                return BadRequest(new { Message = "Email, HashedTempPassword and NewPassword are required." });

            if (!IsValidEmail(request.Email))
                return BadRequest(new { Message = "Email is not valid." });

            try
            {
                var user = await _userRepository.GetByEmailAsync(request.Email);
                if (user == null)
                    return NotFound(new { Message = "User not found." });

                if (!user.IsOAuthUser)
                    return BadRequest(new { Message = "User is not an OAuth-created user." });

                // Compare provided hashed temp password with stored password hash
                if (user.PasswordHash != request.HashedTempPassword)
                    return BadRequest(new { Message = "Invalid temporary password." });

                var newHash = _authService.HashPassword(request.NewPassword);

                // Update password and clear OAuth flag
                await _userRepository.UpdatePasswordAndClearOAuthFlagAsync(request.Email, newHash);

                // generate JWT using updated user info
                // reload user to ensure updated fields are present
                var updatedUser = await _userRepository.GetByEmailAsync(request.Email);
                var token = _authService.GenerateJwtToken(updatedUser!);

                return Ok(new AuthResponseDto
                {
                    Token = token,
                    Message = "success"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set password from OAuth for {Email}", request?.Email);
                return StatusCode(500, "An unexpected error occurred.");
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