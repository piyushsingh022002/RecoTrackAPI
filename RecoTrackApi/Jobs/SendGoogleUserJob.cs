using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using RecoTrack.Infrastructure.ServicesV2;

namespace RecoTrackApi.Jobs
{
    public class SendGoogleUserJob
    {
        private readonly ILogger<SendGoogleUserJob> _logger;
        private readonly EmailService _emailService;

        public SendGoogleUserJob(ILogger<SendGoogleUserJob> logger, EmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        [AutomaticRetry(Attempts =0)]
        public async Task SendGoogleUserAsync(string toEmail, string userPassword, string username, string actionId)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(userPassword) || string.IsNullOrWhiteSpace(actionId))
            {
                _logger.LogWarning("Invalid data supplied to SendGoogleUserJob");
                return;
            }

            try
            {
                await _emailService.SendGoogleWelcomeEmailAsync(toEmail, username, userPassword);
                _logger.LogInformation("SendGoogleUserJob: welcome email queued for {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendGoogleUserJob failed for {Email}", toEmail);
                throw;
            }
        }
    }
}
