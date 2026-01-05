using RecoTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Infrastructure.ServicesV2
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string userJwt, string to, string subject, string body, CancellationToken cancellationToken = default);
    }

    public class EmailService : IEmailService
    {
        private readonly IInternalHttpClient _httpClient;
        private const string EmailServiceUrl = "https://emailservice.example.com/api/send";

        public EmailService(IInternalHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> SendEmailAsync(string userJwt, string to, string subject, string body, CancellationToken cancellationToken = default)
        {
            var request = new
            {
                To = to,
                Subject = subject,
                Body = body
            };

            var response = await _httpClient.PostAsync<object, EmailResponse>(
                EmailServiceUrl,
                request,
                userJwt,
                cancellationToken
            );

            return response.Success;
        }
    }

    public class EmailResponse
    {
        public bool Success { get; set; }
    }
}
