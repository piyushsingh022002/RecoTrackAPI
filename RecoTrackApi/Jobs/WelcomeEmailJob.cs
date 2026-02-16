using Hangfire;
using Microsoft.Extensions.Logging;
using RecoTrack.Infrastructure.ServicesV2;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RecoTrackApi.Jobs
{
    public class WelcomeEmailJob
    {
        private readonly EmailService _emailService;
        private readonly ILogger<WelcomeEmailJob> _logger;

        public WelcomeEmailJob(EmailService emailService, ILogger<WelcomeEmailJob> logger)
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
                // extract email and username from JWT
                string email = string.Empty;
                string name = string.Empty;
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var token = handler.ReadJwtToken(userJwt);
                    email = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value ?? string.Empty;
                    name = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value ?? string.Empty;
                }
                catch
                {
                    // ignore parse errors
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("WelcomeEmailJob: no recipient email found in JWT");
                    return;
                }

                await _emailService.SendWelcomeEmailAsync(email, name);

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
