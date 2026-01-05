using RecoTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly IServiceTokenGenerator _serviceTokenGenerator;
        private const string EmailServiceUrl = "https://emailservice.example.com/api/send";

        public EmailService(IInternalHttpClient httpClient, IServiceTokenGenerator serviceTokenGenerator)
        {
            _httpClient = httpClient;
            _serviceTokenGenerator = serviceTokenGenerator;
        }

        public async Task SendEmailAsync(string userJwt, string emailAction, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(emailAction))
            {
                return;
            }

            var serviceToken = _serviceTokenGenerator.GenerateToken();
            var request = new
            {
                ServiceToken = serviceToken,
                UserJwtToken = userJwt,
                Email_Action = emailAction
            };

            await _httpClient.PostAsync<object, object>(
                EmailServiceUrl,
                request,
                userJwt,
                serviceToken,
                cancellationToken);
        }
    }
}
