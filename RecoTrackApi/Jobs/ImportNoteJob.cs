using Hangfire;
using Microsoft.Extensions.Logging;
using RecoTrack.Application.Models.Notes;
using RecoTrack.Infrastructure.ServicesV2;

namespace RecoTrackApi.Jobs
{
 public class ImportNoteJob
 {
 private readonly IEmailService _emailService;
 private readonly ILogger<ImportNoteJob> _logger;

 public ImportNoteJob(IEmailService emailService, ILogger<ImportNoteJob> logger)
 {
 _emailService = emailService;
 _logger = logger;
 }

 [AutomaticRetry(Attempts =0)]
 public async Task SendImportNoteAsync(string? userJwt, CreateNoteDto noteDto, string actionId, string? toEmail)
 {
 if (noteDto == null || string.IsNullOrWhiteSpace(actionId))
 {
 _logger.LogWarning("Invalid data supplied to ImportNoteJob");
 return;
 }

 try
 {
 await _emailService.SendImportNoteAsync(userJwt, noteDto, actionId, toEmail);
 _logger.LogInformation("Queued import note email to {To}", toEmail ?? "(from JWT)");
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "ImportNoteJob failed to send import note email to {To}", toEmail ?? "(from JWT)");
 throw;
 }
 }
 }
}
