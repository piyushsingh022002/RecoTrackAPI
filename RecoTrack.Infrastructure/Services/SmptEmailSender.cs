using Microsoft.Extensions.Options;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
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
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _opts;
        public SmtpEmailSender(IOptions<SmtpOptions> opts) => _opts = opts.Value;

        public async Task SendAsync(EmailMessage message)
        {
            using var smtp = new SmtpClient(_opts.Host, _opts.Port)
            {
                EnableSsl = _opts.EnableSsl
            };

            if (!string.IsNullOrEmpty(_opts.User))
                smtp.Credentials = new NetworkCredential(_opts.User!, _opts.Password);

            var mail = new MailMessage(_opts.From ?? _opts.User ?? "no-reply@example.com", message.To)
            {
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsBodyHtml
            };

            await smtp.SendMailAsync(mail);
        }
    }
}
