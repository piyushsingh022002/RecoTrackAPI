using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using System;

namespace RecoTrackApi.Jobs
{
    public class SendGoogleUserJob
    {
        private readonly ILogger<SendGoogleUserJob> _logger;

        public SendGoogleUserJob(ILogger<SendGoogleUserJob> logger)
        {
            _logger = logger;
        }

        [AutomaticRetry(Attempts =0)]
        public Task SendGoogleUserAsync(string toEmail, string userPassword, string username, string actionId)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(userPassword) || string.IsNullOrWhiteSpace(actionId))
            {
                _logger.LogWarning("Invalid data supplied to SendGoogleUserJob");
                return Task.CompletedTask;
            }

            _logger.LogInformation("SendGoogleUserJob: email sending disabled. Recipient would be {Email}", toEmail);
            return Task.CompletedTask;
        }
    }
}
