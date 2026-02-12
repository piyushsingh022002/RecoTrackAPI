using Microsoft.Extensions.Options;
using RecoTrack.Application.Interfaces;
using RecoTrack.Shared.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RecoTrack.Infrastructure.ServicesV2
{
    public interface IEmailService
    {
        Task SendEmailAsync(string userJwt, string emailAction, CancellationToken cancellationToken = default);
        Task SendOtpEmailAsync(string toEmail, string otp, CancellationToken cancellationToken = default);
    }

    public class EmailService : IEmailService
    {
        private readonly IInternalHttpClient _httpClient;
        private readonly IServiceTokenGenerator _serviceTokenGenerator;
        private readonly EmailServiceSettings _settings;

        public EmailService(IInternalHttpClient httpClient, IServiceTokenGenerator serviceTokenGenerator, IOptions<EmailServiceSettings> options)
        {
            _httpClient = httpClient;
            _serviceTokenGenerator = serviceTokenGenerator;
            _settings = options?.Value ?? new EmailServiceSettings();
        }

        public async Task SendEmailAsync(string userJwt, string emailAction, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userJwt) || string.IsNullOrWhiteSpace(emailAction))
            {
                return;
            }

            // Try to read email and name from provided JWT
            string email = string.Empty;
            string name = string.Empty;
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(userJwt);
                email = token.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email || c.Type == "email")?.Value ?? string.Empty;
                name = token.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name || c.Type == "name")?.Value ?? string.Empty;
            }
            catch
            {
                // ignore parsing errors - fallbacks will be used
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                // If we couldn't extract email from the JWT, we cannot send the welcome email
                return;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                // fallback to local-part of email
                var parts = email.Split('@');
                name = parts.Length >0 ? parts[0] : email;
            }

            var request = new
            {
                to = email,
                type = emailAction,
                data = new { name }
            };

            var serviceToken = _settings.ServiceToken;
            // Pass the configured service token as both Authorization (userJwt) and X-Service-Token so external service receives it
            await _httpClient.PostAsync<object, object>(
                _settings.Url,
                request,
                userJwt: serviceToken,
                serviceJwt: serviceToken,
                cancellationToken: cancellationToken);
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(otp))
                return;

            var request = new
            {
                to = toEmail,
                type = "OTP",
                data = new { otp }
            };

            var serviceToken = _settings.ServiceToken;

            await _httpClient.PostAsync<object, object>(
                _settings.Url,
                request,
                userJwt: serviceToken,
                serviceJwt: serviceToken,
                cancellationToken: cancellationToken);
        }
    }
}
