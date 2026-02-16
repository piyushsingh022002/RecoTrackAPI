using Hangfire;
using Microsoft.Extensions.Logging;
using RecoTrack.Application.Models.Notes;
using System.Threading.Tasks;

namespace RecoTrackApi.Jobs
{
 public class ImportNoteJob
 {
 private readonly ILogger<ImportNoteJob> _logger;

 public ImportNoteJob(ILogger<ImportNoteJob> logger)
 {
 _logger = logger;
 }

 [AutomaticRetry(Attempts =0)]
 public Task SendImportNoteAsync(string? userJwt, CreateNoteDto noteDto, string actionId, string? toEmail)
 {
 if (noteDto == null || string.IsNullOrWhiteSpace(actionId))
 {
 _logger.LogWarning("Invalid data supplied to ImportNoteJob");
 return Task.CompletedTask;
 }

 // Email sending removed. Log for observability.
 _logger.LogInformation("ImportNoteJob: email sending is disabled. Intended recipient: {Email}", toEmail ?? "(from JWT)");
 return Task.CompletedTask;
 }
 }
}
