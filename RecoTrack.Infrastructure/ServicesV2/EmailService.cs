using RecoTrack.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace RecoTrack.Infrastructure.ServicesV2
{
    public interface IEmailService
    {
        Task SendEmailAsync(string userJwt, string emailAction, CancellationToken cancellationToken = default);
    }

    public class EmailService : IEmailService
    {
        private readonly IInternalHttpClient _httpClient;
        private const string EmailServiceUrl = "https://recotrack-emailservice-java-program.onrender.com/api/email/send/critical";

        public EmailService(IInternalHttpClient httpClient)
        {
            _httpClient = httpClient;
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
    }
}
