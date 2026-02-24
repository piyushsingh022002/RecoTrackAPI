using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecoTrack.Application.Models.Notes;
using RecoTrackApi.Services.Interfaces;
using System.Security.Claims;
using RecoTrackApi.DTOs;

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
        public async Task<IActionResult> GetNotes([FromQuery] string? filter, [FromQuery] string? search)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to GetNotes");
                return Unauthorized();
            }

            _logger.LogInformation("GetNotes requested by UserId {UserId} with filter {Filter} and search {Search}", userId, filter, search);

            try
            {
                List<Note> notes;
                // If caller didn't provide filter or search, call the single-arg service method so breakpoints there are hit
                if (string.IsNullOrWhiteSpace(filter) && string.IsNullOrWhiteSpace(search))
                {
                    notes = await _noteService.GetNotesAsync(userId);
                }
                else
                {
                    notes = await _noteService.GetNotesAsync(userId, filter, search);
                }

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
                // Persist note when SaveOption == SAVE, otherwise only record activity
                if (string.Equals(saveOption, "SAVE", StringComparison.OrdinalIgnoreCase))
                {
                    await _noteService.CreateNoteAsync(note);
                }
                else
                {
                    // JUST_DOWNLOAD or other: do not persist, but record activity
                    await _noteService.CreateOrRecordAsync(note, saveOption, eventType, userId);
                }

                // If IMPORT_EMAIL and SaveOption JUST_DOWNLOAD or SAVE, send email via background job
                if (string.Equals(eventType, "IMPORT_EMAIL", StringComparison.OrdinalIgnoreCase))
                {
                    var authHeader = Request.Headers["Authorization"].ToString();
                    string? userJwt = null;
                    if (!string.IsNullOrWhiteSpace(authHeader))
                    {
                        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            userJwt = authHeader[7..].Trim();
                        }
                        else
                        {
                            userJwt = authHeader;
                        }
                    }

                    var externalEmail = noteDto.ExternalEmail?.Trim();

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
                // Call service overload that uses null presentFields so it updates only non-null properties
                var success = await _noteService.UpdateNoteAsync(id, updateDto, userId);

                if (!success)
                {
                    _logger.LogInformation("Note not found or not updated. UserId {UserId}, NoteId {NoteId}", userId, id);
                    return NotFound();
                }

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

        [HttpGet("shared-with-me")]
        public async Task<IActionResult> GetNotesSharedWithMe()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to GetNotesSharedWithMe");
                return Unauthorized();
            }

            _logger.LogInformation("GetNotesSharedWithMe requested by UserId {UserId}", userId);
            try
            {
                var shared = await _noteService.GetNotesSharedWithMeAsync(userId);
                return Ok(new ApiResponse<List<SharedNoteDto>> { Success = true, Data = shared, Message = "Shared notes fetched successfully." });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input in GetNotesSharedWithMe for UserId {UserId}", userId);
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in GetNotesSharedWithMe for UserId {UserId}", userId);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An unexpected error occurred." });
            }
        }

        [HttpPost("{id}/share")]
        public async Task<IActionResult> ShareNote(string id, [FromBody] ShareNoteRequestDto request)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to ShareNote");
                return Unauthorized();
            }

            if (request == null)
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Note ID is required." });

            // Validate permission
            var perm = request.Permission?.Trim().ToUpperInvariant();
            if (perm != "VIEW" && perm != "EDIT")
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Permission must be VIEW or EDIT." });

            try
            {
                bool shared = false;

                // If emails provided, use email-based sharing (service resolves emails to users)
                if (!string.IsNullOrWhiteSpace(request.SharedWithEmails))
                {
                    shared = await _noteService.ShareNoteByEmailsAsync(id, userId, request.SharedWithEmails, perm);
                }
                else if (!string.IsNullOrWhiteSpace(request.SharedWithUserId))
                {
                    shared = await _noteService.ShareNoteAsync(id, userId, request.SharedWithUserId, perm);
                }
                else
                {
                    return BadRequest(new ApiResponse<object> { Success = false, Message = "Either SharedWithUserId or SharedWithEmails must be provided." });
                }

                if (!shared)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Note not found or you are not the owner / recipients not resolved." });

                return Ok(new ApiResponse<object> { Success = true, Message = "Note shared successfully." });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input in ShareNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in ShareNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An unexpected error occurred." });
            }
        }

        [HttpDelete("{id}/share/{sharedWithUserId}")]
        public async Task<IActionResult> UnshareNote(string id, string sharedWithUserId)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogInformation("Unauthorized request to UnshareNote");
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(sharedWithUserId))
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Note ID and sharedWithUserId are required." });

            try
            {
                var removed = await _noteService.UnshareNoteAsync(id, userId, sharedWithUserId);
                if (!removed)
                    return NotFound(new ApiResponse<object> { Success = false, Message = "Share relationship not found or you're not owner." });

                return Ok(new ApiResponse<object> { Success = true, Message = "Note unshared successfully." });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid input in UnshareNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return BadRequest(new ApiResponse<object> { Success = false, Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in UnshareNote. UserId {UserId}, NoteId {NoteId}", userId, id);
                return StatusCode(500, new ApiResponse<object> { Success = false, Message = "An unexpected error occurred." });
            }
        }

    }
}
