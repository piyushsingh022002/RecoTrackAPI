using Hangfire;
using Microsoft.Extensions.Logging;
using RecoTrack.Infrastructure.ServicesV2;

namespace RecoTrackApi.Jobs
{
    public class WelcomeEmailJob
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<WelcomeEmailJob> _logger;

        public WelcomeEmailJob(IEmailService emailService, ILogger<WelcomeEmailJob> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [AutomaticRetry(Attempts = 0)]
        public async Task SendEmailAsync(string userJwt, string actionId)
        {
            if (string.IsNullOrWhiteSpace(userJwt) || string.IsNullOrWhiteSpace(actionId))
            {
                _logger.LogWarning("Invalid data supplied to WelcomeEmailJob");
                return;
            }

            try
            {
                await _emailService.SendEmailAsync(userJwt, actionId);
                _logger.LogInformation("Queued welcome email for token ending with {LastChars} for welcome email",
                    userJwt.Length > 10 ? userJwt[^10..] : userJwt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WelcomeEmailJob failed to send email for token ending with {LastChars} for welcome email",
                    userJwt.Length > 10 ? userJwt[^10..] : userJwt);
                throw;
            }
        }
    }
}
