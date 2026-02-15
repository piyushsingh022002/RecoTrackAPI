using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecoTrack.Application.Models.Notes;
using RecoTrackApi.Services.Interfaces;
using System.Security.Claims;

namespace RecoTrackApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NotesController> _logger;
        private readonly INotificationService _notificationService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<RecoTrackApi.Hubs.NotificationHub> _notificationHub;
        private readonly IBackgroundJobClient? _backgroundJob;

        public NotesController(
            INoteService noteService,
            ILogger<NotesController> logger,
            INotificationService notificationService,
            Microsoft.AspNetCore.SignalR.IHubContext<RecoTrackApi.Hubs.NotificationHub> notificationHub,
            IBackgroundJobClient? backgroundJob = null)
        {
            _noteService = noteService;
            _logger = logger;
            _notificationService = notificationService;
            _notificationHub = notificationHub;
            _backgroundJob = backgroundJob;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet]
        public async Task<IActionResult> GetNotes()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to GetNotes");
                return Unauthorized();
            }

            _logger.LogInformation("GetNotes requested by UserId {UserId}", userId);

            try
            {
                var notes = await _noteService.GetNotesAsync(userId);
                _logger.LogDebug("GetNotes returned {Count} notes for UserId {UserId}", notes.Count, userId);
                return Ok(notes);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input in GetNotes for UserId {UserId}", userId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GetNotes for UserId {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedNotes()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to GetDeletedNotes");
                return Unauthorized();
            }
            _logger.LogInformation("GetDeletedNotes requested by UserId {UserId}", userId);

            try
            {
                var deletedNotes = await _noteService.GetDeletedNotesAsync(userId);
                _logger.LogDebug("GetDeletedNotes returned {Count} notes for UserId {UserId}", deletedNotes.Count, userId);
                return Ok(deletedNotes);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input in GetDeletedNotes for UserId {UserId}", userId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GetDeletedNotes for UserId {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNote(string id)
        {
            var userId = GetUserId();

            if (string.IsNullOrWhiteSpace(userId)) 
            {
                _logger.LogInformation("Unauthorized request to GetNote");
                return Unauthorized();
            }

            _logger.LogInformation("GetNote requested. UserId {UserId}, NoteId {NoteId}",userId, id);
            try
            {

                var note = await _noteService.GetNoteByIdAsync(id, userId);

                if (note is null)
                {
                    _logger.LogInformation(
                    "Note not found. UserId {UserId}, NoteId {NoteId}",
                    userId, id
                );
                    return NotFound();
                }

                return Ok(note);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input in GetNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GetNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateNote([FromBody] CreateNoteDto noteDto)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to CreateNote");
                return Unauthorized();
            }

            if (noteDto == null)
            {
                return BadRequest("Note data is required.");
            }

            if (string.IsNullOrWhiteSpace(noteDto.Title) && !string.Equals(noteDto.SaveOption, "JUST_DOWNLOAD", System.StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Title is required.");
            }

            var note = new Note
            {
                UserId = userId,
                Title = noteDto.Title?.Trim(),
                Content = noteDto.Content?.Trim(),
                Tags = noteDto.Tags ?? new List<string>(),
                Labels = noteDto.Labels ?? new List<string>(),
                MediaUrls = noteDto.MediaUrls ?? new List<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                DeletedAt = null
            };

            // SaveOption: SAVE | JUST_DOWNLOAD
            var saveOption = noteDto.SaveOption ?? "SAVE";
            var eventType = noteDto.EventType ?? "DOWNLOAD";

            _logger.LogInformation("CreateNote requested by UserId {UserId} with SaveOption {SaveOption} and EventType {EventType}", userId, saveOption, eventType);

            try
            {
                // Use service helper to create or just record activity
                if (_noteService is RecoTrackApi.Services.NoteService concrete)
                {
                    var noteRef = await concrete.CreateOrRecordAsync(note, saveOption, eventType, userId);

                    // If IMPORT_EMAIL and SaveOption JUST_DOWNLOAD or SAVE, send email via background job
                    if (string.Equals(eventType, "IMPORT_EMAIL", System.StringComparison.OrdinalIgnoreCase))
                    {
                        // Resolve JWT from Authorization header (if present)
                        var authHeader = Request.Headers["Authorization"].ToString();
                        string? userJwt = null;
                        if (!string.IsNullOrWhiteSpace(authHeader))
                        {
                            if (authHeader.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
                            {
                                userJwt = authHeader[7..].Trim();
                            }
                            else
                            {
                                userJwt = authHeader;
                            }
                        }

                        var externalEmail = noteDto.ExternalEmail?.Trim();

                        // Enqueue the import note job. Job will resolve fallback to user's email from JWT if externalEmail is null/empty.
                        if (_backgroundJob != null)
                        {
                            try
                            {
                                _backgroundJob.Enqueue<RecoTrackApi.Jobs.ImportNoteJob>(job => job.SendImportNoteAsync(userJwt, noteDto, "IMPORT_EMAIL", externalEmail));
                                _logger.LogInformation("Enqueued ImportNoteJob for UserId {UserId} to email {Email}", userId, externalEmail ?? "(from JWT)");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to enqueue ImportNoteJob for UserId {UserId}", userId);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Background job client is not configured; import email will not be queued for UserId {UserId}", userId);
                        }
                    }

                    // JUST_DOWNLOAD: return NoteRefId so frontend can track the download
                    return Ok(note);
                }

                // Fallback: use interface method (create and return note)
                await _noteService.CreateNoteAsync(note);
                return Ok(note);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input in CreateNote. UserId {UserId}", userId);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in CreateNote. UserId {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred while creating the note.");
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(string id, [FromBody] UpdateNoteDto updateDto)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to UpdateNote");
                return Unauthorized("Invalid or missing user ID.");
            }

            if (updateDto == null)
            {
                return BadRequest("Note data is required.");
            }

            _logger.LogInformation("UpdateNote requested. UserId {UserId}, NoteId {NoteId}", userId, id);

            try
            {
                var success = await _noteService.UpdateNoteAsync(id, updateDto, userId);

                if (!success)
                {
                    _logger.LogInformation("Note not found or not updated. UserId {UserId}, NoteId {NoteId}", userId, id);
                    return NotFound();
                }

                // Send notification via SignalR and store in DB
               // var message = $"{updateDto.Title} is modified at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
               //var notifications = await _notificationService.SendNotificationAsync(userId, message);
               // await _notificationHub.Clients.User(userId).SendCoreAsync("ReceiveNotification", new object[] { notifications });
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input in UpdateNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in UpdateNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return StatusCode(500, "An unexpected error occurred while updating the note.");
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(string id)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to DeleteNote");
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Note ID is required.");

            _logger.LogInformation("DeleteNote requested. UserId {UserId}, NoteId {NoteId}", userId, id);

            try
            {
                var noteToDelete = await _noteService.GetNoteByIdAsync(id, userId);
                var success = await _noteService.DeleteNoteAsync(id, userId);

                if (!success)
                {
                    _logger.LogInformation("Note not found or unauthorized deletion. UserId {UserId}, NoteId {NoteId}", userId, id);
                    return NotFound();
                }

                // Optional: SignalR notification
                if (noteToDelete != null)
                {
                    var message = $"{noteToDelete.Title} is deleted";
                    var notifications = await _notificationService.SendNotificationAsync(userId, message);
                    await _notificationHub.Clients.User(userId).SendCoreAsync("ReceiveNotification", new object[] { notifications });
                }

                _logger.LogInformation("Note {NoteId} successfully deleted by UserId {UserId}", id, userId);

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                return Ok(new { DeletedNoteId = id, Username = username });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input in DeleteNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in DeleteNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return StatusCode(500, "An unexpected error occurred while deleting the note.");
            }
        }


        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreNote(string id)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to RestoreNote");
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Note ID is required.");

            _logger.LogInformation("RestoreNote requested. UserId {UserId}, NoteId {NoteId}", userId, id);

            try
            {
                var restored = await _noteService.RestoreNoteAsync(id, userId);
                if (!restored)
                {
                    _logger.LogInformation("Note not found or not deleted. UserId {UserId}, NoteId {NoteId}", userId, id);
                    return NotFound();
                }

                var note = await _noteService.GetNoteByIdAsync(id, userId);
                if (note != null)
                {
                    var message = $"{note.Title} is restored";
                    var notifications = await _notificationService.SendNotificationAsync(userId, message);
                    await _notificationHub.Clients.User(userId).SendCoreAsync("ReceiveNotification", new object[] { notifications });
                }

                _logger.LogInformation("Note {NoteId} successfully restored by UserId {UserId}", id, userId);

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                return Ok(new { RestoredNoteId = id, Username = username });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input in RestoreNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in RestoreNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return StatusCode(500, "An unexpected error occurred while restoring the note.");
            }
        }

        //get all favourite notes 
        [HttpGet("favourites")]
        public async Task<IActionResult> GetAllFavouriteNotes()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to GetAllFavouriteNotes");
                return Unauthorized();
            }

            _logger.LogInformation("GetAllFavouriteNotes requested by UserId {UserId}", userId);

            try
            {
                var notes = await _noteService.GetAllFavouriteNotesAsync(userId);
                _logger.LogInformation("Returning {Count} favourite notes for UserId {UserId}", notes.Count, userId);

                if (notes.Count ==0)
                    return Ok("No favourite notes found.");

                return Ok(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GetAllFavouriteNotes. UserId {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred while retrieving favourite notes.");
            }
        }

        //get all important notes 
        [HttpGet("important")]
        public async Task<IActionResult> GetAllImportantNotes()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to GetAllImportantNotes");
                return Unauthorized();
            }

            _logger.LogInformation("GetAllImportantNotes requested by UserId {UserId}", userId);

            try
            {
                var notes = await _noteService.GetAllImportantNotesAsync(userId);
                _logger.LogInformation("Returning {Count} important notes for UserId {UserId}", notes.Count, userId);

                if (notes.Count ==0)
                    return Ok("No important notes found.");

                return Ok(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GetAllImportantNotes. UserId {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred while retrieving important notes.");
            }
        }

    }
}
