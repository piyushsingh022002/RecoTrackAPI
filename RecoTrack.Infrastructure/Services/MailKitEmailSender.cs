using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
    public class MailKitEmailSender : IEmailSender
    {
        private readonly SmtpOptions _opts;

        public MailKitEmailSender(IOptions<SmtpOptions> opts)
        {
            _opts = opts.Value;
        }

        public async Task SendAsync(EmailMessage message)
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(MailboxAddress.Parse(_opts.From ?? _opts.User ?? "no-reply@example.com"));
            mimeMessage.To.Add(MailboxAddress.Parse(message.To));
            mimeMessage.Subject = message.Subject;

            mimeMessage.Body = new TextPart(message.IsBodyHtml ? "html" : "plain")
            {
                Text = message.Body
            };

            // Use explicit type to resolve ambiguity
            using (var client = new MailKit.Net.Smtp.SmtpClient())
            {
                await client.ConnectAsync(_opts.Host, _opts.Port, SecureSocketOptions.StartTls);

                if (!string.IsNullOrEmpty(_opts.User))
                    await client.AuthenticateAsync(_opts.User, _opts.Password);

                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
            }
        }
    }
}
