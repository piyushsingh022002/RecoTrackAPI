using RecoTrack.Application.Dtos;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace RecoTrackApi.Jobs
{
    public class EmailJob : IEmailJob
    {
        private readonly IEmailAuditRepository _auditRepo;
        private readonly ILogger<EmailJob> _logger;

        public EmailJob(IEmailAuditRepository auditRepo, ILogger<EmailJob> logger)
        {
            _auditRepo = auditRepo;
            _logger = logger;
        }

        public async Task SendEmailAsync(EmailRequestDto request)
        {
            try
            {
                // Email sending removed. Persist audit record so system can track attempts.
                await _auditRepo.AddAsync(new EmailAuditRecord
                {
                    UserId = request.UserId,
                    UserName = request.UserName,
                    ToEmail = request.ToEmail,
                    SentAtUtc = DateTime.UtcNow
                });

                _logger.LogInformation("EmailJob: Email sending disabled. Audit recorded for {Email} user {UserId}", request.ToEmail, request.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailJob failed for {Email} user {UserId}", request.ToEmail, request.UserId);
                throw; // let Hangfire mark job as failed & trigger retries
            }
        }
    }
}
