using Microsoft.Extensions.Options;
using RecoTrack.Application.Interfaces;
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
        private readonly string _emailServiceUrl;
        private readonly string _otpEmailServiceUrl;

        public EmailService(IInternalHttpClient httpClient, IServiceTokenGenerator serviceTokenGenerator, Microsoft.Extensions.Options.IOptions<EmailServiceSettings> emailOptions)
        {
            _httpClient = httpClient;
            _serviceTokenGenerator = serviceTokenGenerator;
            _emailServiceUrl = emailOptions.Value.EmailServiceUrl;
            _otpEmailServiceUrl = emailOptions.Value.OtpEmailServiceUrl;
        }

        public async Task SendEmailAsync(string userJwt, string emailAction, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userJwt) || string.IsNullOrWhiteSpace(emailAction) || string.IsNullOrWhiteSpace(_emailServiceUrl))
            {
                return;
            }

            var request = new { actionId = emailAction };

            await _httpClient.PostAsync<object, object>(
                _emailServiceUrl,
                request,
                userJwt,
                cancellationToken: cancellationToken);
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(otp) || string.IsNullOrWhiteSpace(_otpEmailServiceUrl))
                return;

            var request = new
            {
                email = toEmail,
                otp,
                actionType = "FORGOT_PASSWORD"
            };

            await _httpClient.PostAsync<object, object>(
                _otpEmailServiceUrl,
                request,
                userJwt: null,
                serviceJwt: _serviceTokenGenerator.GenerateToken(),
                cancellationToken: cancellationToken);
        }
    }
}
