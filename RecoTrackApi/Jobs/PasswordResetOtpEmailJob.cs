using Hangfire;
using Microsoft.Extensions.Logging;
using RecoTrack.Infrastructure.ServicesV2;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RecoTrackApi.Jobs
{
    public class PasswordResetOtpEmailJob
    {
        private readonly ILogger<PasswordResetOtpEmailJob> _logger;
        private readonly EmailService _emailService;

        public PasswordResetOtpEmailJob(ILogger<PasswordResetOtpEmailJob> logger, EmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        // Allow retries for transient failures
        [AutomaticRetry(Attempts =3)]
        public async Task SendOtpEmailAsync(string jwtOrEmail, string otp)
        {
            if (string.IsNullOrWhiteSpace(jwtOrEmail) || string.IsNullOrWhiteSpace(otp))
            {
                _logger.LogWarning("PasswordResetOtpEmailJob invoked with invalid payload. Arg: {Arg}", jwtOrEmail);
                return;
            }

            string email = string.Empty;
            string name = string.Empty;

            try
            {
                // If it looks like a JWT (contains a dot) or starts with Bearer, try to parse and extract claims
                var tokenCandidate = jwtOrEmail.Trim();
                if (tokenCandidate.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    tokenCandidate = tokenCandidate.Substring(7).Trim();

                if (tokenCandidate.Contains('.'))
                {
                    try
                    {
                        var handler = new JwtSecurityTokenHandler();
                        var token = handler.ReadJwtToken(tokenCandidate);
                        email = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email")?.Value ?? string.Empty;
                        name = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name || c.Type == "name")?.Value ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse JWT supplied to PasswordResetOtpEmailJob");
                    }
                }

                // If parsing didn't produce an email, treat jwtOrEmail as direct email address
                if (string.IsNullOrWhiteSpace(email))
                {
                    email = jwtOrEmail;
                }

                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("PasswordResetOtpEmailJob: no recipient email found");
                    return;
                }

                _logger.LogInformation("PasswordResetOtpEmailJob: sending OTP email to {Email}", email);

                // Use EmailService which already uses Brevo and templates
                await _emailService.SendOtpEmailAsync(email, name, otp).ConfigureAwait(false);

                _logger.LogInformation("PasswordResetOtpEmailJob: OTP email sent to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PasswordResetOtpEmailJob failed to send OTP email for recipient {Recipient}", jwtOrEmail);
                throw; // let Hangfire mark job as failed and retry according to policy
            }
        }
    }
}
