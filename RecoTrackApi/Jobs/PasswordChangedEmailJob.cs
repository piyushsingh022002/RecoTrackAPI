using Hangfire;
using Microsoft.Extensions.Logging;
using RecoTrack.Infrastructure.ServicesV2;
using System;
using System.Threading.Tasks;

namespace RecoTrackApi.Jobs
{
 public class PasswordChangedEmailJob
 {
 private readonly ILogger<PasswordChangedEmailJob> _logger;
 private readonly EmailService _emailService;

 public PasswordChangedEmailJob(ILogger<PasswordChangedEmailJob> logger, EmailService emailService)
 {
 _logger = logger;
 _emailService = emailService;
 }

 [AutomaticRetry(Attempts =3)]
 public async Task SendNotificationAsync(string toEmail, string username)
 {
 if (string.IsNullOrWhiteSpace(toEmail))
 {
 _logger.LogWarning("PasswordChangedEmailJob invoked with empty recipient");
 return;
 }

 try
 {
 _logger.LogInformation("Sending password changed notification to {Email}", toEmail);
 await _emailService.SendPasswordChangedEmailAsync(toEmail, username).ConfigureAwait(false);
 _logger.LogInformation("Password changed notification sent to {Email}", toEmail);
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Failed to send password changed notification to {Email}", toEmail);
 throw; // allow Hangfire to record and retry
 }
 }
 }
}
