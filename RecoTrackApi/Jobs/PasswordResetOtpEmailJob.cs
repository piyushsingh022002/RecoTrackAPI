using Hangfire;
using Microsoft.Extensions.Logging;
using RecoTrack.Infrastructure.ServicesV2;
using System;
using System.Threading.Tasks;

namespace RecoTrackApi.Jobs
{
    public class PasswordResetOtpEmailJob
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordResetOtpEmailJob> _logger;

        public PasswordResetOtpEmailJob(IEmailService emailService, ILogger<PasswordResetOtpEmailJob> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SendOtpEmailAsync(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            {
                _logger.LogWarning("Password reset OTP job invoked with invalid payload. Email: {Email}", email);
                return;
            }

            try
            {
                await _emailService.SendOtpEmailAsync(email, otp);
                _logger.LogInformation("Password reset OTP enqueued for {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue password reset OTP for {Email}", email);
                throw;
            }
        }
    }
}
