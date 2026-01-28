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
        private const string EmailServiceUrl = "https://recotrack-emailservice-java-program.com/api/email/send/critical";
        private const string OtpEmailServiceUrl = "https://recotrack-emailservice-java-program.onrender.com/api/email/send/forgot-password/otp";

        public EmailService(IInternalHttpClient httpClient, IServiceTokenGenerator serviceTokenGenerator)
        {
            _httpClient = httpClient;
            _serviceTokenGenerator = serviceTokenGenerator;
        }

        public async Task SendEmailAsync(string userJwt, string emailAction, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userJwt) || string.IsNullOrWhiteSpace(emailAction))
            {
                return;
            }

            var request = new { actionId = emailAction };

            await _httpClient.PostAsync<object, object>(
                EmailServiceUrl,
                request,
                userJwt,
                cancellationToken: cancellationToken);
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(otp))
                return;

            var request = new
            {
                email = toEmail,
                otp,
                actionType = "FORGOT_PASSWORD"
            };

            await _httpClient.PostAsync<object, object>(
                OtpEmailServiceUrl,
                request,
                userJwt: null,
                serviceJwt: _serviceTokenGenerator.GenerateToken(),
                cancellationToken: cancellationToken);
        }
    }
}
