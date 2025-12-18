using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Options;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;
using System;
using System.Threading.Tasks;

namespace RecoTrack.Infrastructure.Services
{
    public class SmtpOptions
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 25;
        public bool EnableSsl { get; set; } = false;
        public string? User { get; set; }
        public string? Password { get; set; }
        public string? From { get; set; }
    }
    public class MailKitEmailSender : IEmailSender
    {
        private readonly SmtpOptions _opts;

        public MailKitEmailSender(IOptions<SmtpOptions> opts)
        {
            _opts = opts.Value;
        }

        public async Task SendAsync(EmailMessage message)
        {
            var apiKey = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("SENDGRID_API_KEY environment variable is not set.");

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(_opts.From ?? "no-reply@example.com", "RecoTrack");
            var to = new EmailAddress(message.To);
            var subject = message.Subject;
            var plainTextContent = message.IsBodyHtml ? null : message.Body;
            var htmlContent = message.IsBodyHtml ? message.Body : null;
            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent ?? message.Body,
                htmlContent ?? message.Body
            );
            try
            {
                var response = await client.SendEmailAsync(msg);
                if (!response.IsSuccessStatusCode)
                {
                    // Optionally log the error details
                    var errorBody = await response.Body.ReadAsStringAsync();
                    // You can use your logging framework here
                    Console.Error.WriteLine($"SendGrid send failed: {response.StatusCode} {errorBody}");
                }
            }
            catch (Exception ex)
            {
                // Log or handle exception gracefully
                Console.Error.WriteLine($"Exception sending email via SendGrid: {ex.Message}");
                throw;
            }
        }
    }
}
