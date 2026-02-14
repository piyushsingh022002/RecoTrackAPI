using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using RecoTrack.Infrastructure.ServicesV2;

namespace RecoTrackApi.Jobs
{
 public class SendGoogleUserJob
 {
 private readonly IEmailService _emailService;
 private readonly ILogger<SendGoogleUserJob> _logger;

 public SendGoogleUserJob(IEmailService emailService, ILogger<SendGoogleUserJob> logger)
 {
 _emailService = emailService;
 _logger = logger;
 }

 [AutomaticRetry(Attempts =0)]
 public async Task SendGoogleUserAsync(string toEmail, string userPassword, string username, string actionId)
 {
 if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(userPassword) || string.IsNullOrWhiteSpace(actionId))
 {
 _logger.LogWarning("Invalid data supplied to SendGoogleUserJob");
 return;
 }

 try
 {
 await _emailService.SendGoogleUserAsync(toEmail, userPassword, username, actionId);
 _logger.LogInformation("Queued google user email for {Email}", toEmail);
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "SendGoogleUserJob failed to send email to {Email}", toEmail);
 throw;
 }
 }
 }
}
