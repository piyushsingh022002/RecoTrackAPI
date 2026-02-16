using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace RecoTrackApi.Jobs
{
    public class PasswordResetOtpEmailJob
    {
        private readonly ILogger<PasswordResetOtpEmailJob> _logger;

        public PasswordResetOtpEmailJob(ILogger<PasswordResetOtpEmailJob> logger)
        {
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 0)]
        public Task SendOtpEmailAsync(string email, string otp)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(otp))
            {
                _logger.LogWarning("PasswordResetOtpEmailJob invoked with invalid payload. Email: {Email}", email);
                return Task.CompletedTask;
            }

            // Email sending has been removed. Keep a trace log for observability.
            _logger.LogInformation("PasswordResetOtpEmailJob: email sending is disabled in this build. Intended recipient: {Email}", email);
            return Task.CompletedTask;
        }
    }
}
