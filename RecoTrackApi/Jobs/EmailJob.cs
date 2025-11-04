using RecoTrack.Application.Dtos;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;

namespace RecoTrackApi.Jobs
{
    public class EmailJob : IEmailJob
    {

        private readonly IEmailSender _emailSender;
        private readonly IEmailAuditRepository _auditRepo;
        private readonly ILogger<EmailJob> _logger;

        public EmailJob(IEmailSender emailSender, IEmailAuditRepository auditRepo, ILogger<EmailJob> logger)
        {
            _emailSender = emailSender;
            _auditRepo = auditRepo;
            _logger = logger;
        }

        public async Task SendEmailAsync(EmailRequestDto request)
        {
            try 
            {
                var message = new EmailMessage
                {
                    To = request.ToEmail,
                    Subject = request.Subject,
                    Body = request.BodyMarkdown,
                    IsBodyHtml = false
                };

                await _emailSender.SendAsync(message);

                // Persist audit record (only minimal fields)
                await _auditRepo.AddAsync(new EmailAuditRecord
                {
                    UserId = request.UserId,
                    UserName = request.UserName,
                    ToEmail = request.ToEmail,
                    SentAtUtc = DateTime.UtcNow
                });

                _logger.LogInformation("EmailJob: Sent email to {Email} for user {UserId}", request.ToEmail, request.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EmailJob failed for {Email} user {UserId}", request.ToEmail, request.UserId);
                throw; // let Hangfire mark job as failed & trigger retries

            }

        }
    }
}
