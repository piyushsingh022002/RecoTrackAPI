using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecoTrackApi.DTOs;
using RecoTrackApi.Models;
using RecoTrackApi.Services.Interfaces;
using System.Security.Claims;
using Serilog;

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

        public NotesController(
            INoteService noteService,
            ILogger<NotesController> logger,
            INotificationService notificationService,
            Microsoft.AspNetCore.SignalR.IHubContext<RecoTrackApi.Hubs.NotificationHub> notificationHub)
        {
            _noteService = noteService;
            _logger = logger;
            _notificationService = notificationService;
            _notificationHub = notificationHub;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet]
        public async Task<IActionResult> GetNotes()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Unauthorized access attempt at {Timestamp}", DateTime.UtcNow);
                return Unauthorized();
            }

            _logger.LogInformation("User {UserId} requested all notes at {Timestamp}", userId, DateTime.UtcNow);

            try
            {
                var notes = await _noteService.GetNotesAsync(userId);
                _logger.LogInformation("Returning {Count} notes for user {UserId}", notes.Count, userId);
                return Ok(notes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving notes for user {UserId} at {Timestamp}", userId, DateTime.UtcNow);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("deleted")]
        public async Task<IActionResult> GetDeletedNotes()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Unauthorized access attempt to deleted notes at {Timestamp}", DateTime.UtcNow);
                return Unauthorized();
            }

            try
            {
                var deletedNotes = await _noteService.GetDeletedNotesAsync(userId);
                _logger.LogInformation("Returning {Count} deleted notes for user {UserId}", deletedNotes.Count, userId);

                if(deletedNotes.Count == 0)
                {
                    return Ok("Deleted Notes are empty for now !!");
                }
                return Ok(deletedNotes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving deleted notes for user {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNote(string id)
        {
            var userId = GetUserId();

            if (userId is null) 
            {
                _logger.LogInformation("UserId cannot be null}");
                return Unauthorized();
            }

            _logger.LogInformation("User {UserId} requested note {NoteId} at {Timestamp}", userId, id, DateTime.UtcNow);
            var note = await _noteService.GetNoteByIdAsync(id, userId);
            if (note is null)
            {
                _logger.LogWarning("Note {NoteId} not found for user {UserId}", id, userId);
                return NotFound();
            }
            
            _logger.LogInformation("Successfully retrieved note {NoteId} for user {UserId}", id, userId);
            return Ok(note);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNote([FromBody] NoteCreateDto noteDto)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Create note attempt with invalid user ID at {Timestamp}", DateTime.UtcNow);
                return Unauthorized("Invalid or missing user ID in token.");
            }

            if (noteDto == null)
            {
                _logger.LogWarning("Null payload received for note creation at {Timestamp}", DateTime.UtcNow);
                return BadRequest("Note data is required.");
            }

            if (string.IsNullOrWhiteSpace(noteDto.Title))
            {
                _logger.LogWarning("Note creation failed due to missing title at {Timestamp}", DateTime.UtcNow);
                return BadRequest("Title is required.");
            }

            var note = new Note
            {
                UserId = userId,
                Title = noteDto.Title.Trim(),
                Content = noteDto.Content?.Trim(),
                Tags = noteDto.Tags ?? new List<string>(),
                MediaUrls = noteDto.MediaUrls ?? new List<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("User {UserId} creating new note with title '{Title}' at {Timestamp}",
                    userId, note.Title, DateTime.UtcNow);

                await _noteService.CreateNoteAsync(note);

                // Send notification via SignalR and store in DB
                var message = $"{note.Title} is created successfully at {note.CreatedAt:yyyy-MM-dd HH:mm:ss}";
                var notifications = await _notificationService.SendNotificationAsync(userId, message);
                await _notificationHub.Clients.User(userId).SendCoreAsync("ReceiveNotification",new object []{ notifications});

                _logger.LogInformation("Successfully created note {NoteId} for user {UserId} at {Timestamp}",
                    note.Id, userId, DateTime.UtcNow);

                return Ok(note);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create note for user {UserId} at {Timestamp}", userId, DateTime.UtcNow);
                return StatusCode(500, "An unexpected error occurred while creating the note.");
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(string id, [FromBody] NoteUpdateDto updateDto)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Unauthorized update attempt on note {NoteId} at {Timestamp}", id, DateTime.UtcNow);
                return Unauthorized("Invalid or missing user ID.");
            }

            if (updateDto == null)
            {
                _logger.LogWarning("Null payload received for update on note {NoteId} at {Timestamp}", id, DateTime.UtcNow);
                return BadRequest("Note data is required.");
            }

            _logger.LogInformation("User {UserId} attempting to update note {NoteId} at {Timestamp}", userId, id, DateTime.UtcNow);

            try
            {
                var success = await _noteService.UpdateNoteAsync(id, updateDto, userId);

                if (!success)
                {
                    _logger.LogWarning("Note {NoteId} not found or not updated for user {UserId}", id, userId);
                    return NotFound();
                }

                // Send notification via SignalR and store in DB
                var message = $"{updateDto.Title} is modified at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}";
               var notifications = await _notificationService.SendNotificationAsync(userId, message);
                await _notificationHub.Clients.User(userId).SendCoreAsync("ReceiveNotification", new object[] { notifications });

                _logger.LogInformation("Note {NoteId} successfully updated for user {UserId}", id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while updating note {NoteId} for user {UserId}", id, userId);
                return StatusCode(500, "An unexpected error occurred while updating the note.");
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(string id)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Unauthorized delete attempt on note {NoteId} at {Timestamp}", id, DateTime.UtcNow);
                return Unauthorized("Invalid or missing user ID.");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Delete attempt with empty note ID by user {UserId} at {Timestamp}", userId, DateTime.UtcNow);
                return BadRequest("Note ID is required.");
            }

            _logger.LogInformation("User {UserId} attempting to delete note {NoteId} at {Timestamp}", userId, id, DateTime.UtcNow);

            try
            {
                var noteToDelete = await _noteService.GetNoteByIdAsync(id, userId);
                var success = await _noteService.DeleteNoteAsync(id, userId);

                if (!success)
                {
                    _logger.LogWarning("Note {NoteId} not found or unauthorized deletion attempt by user {UserId}", id, userId);
                    return NotFound();
                }

                // Send notification via SignalR and store in DB
                var message = noteToDelete != null ? $"{noteToDelete.Title} is deleted" : "Note is deleted";
                var notifications = await _notificationService.SendNotificationAsync(userId, message);
                await _notificationHub.Clients.User(userId).SendCoreAsync("ReceiveNotification", new object[] { notifications });

                _logger.LogInformation("Note {NoteId} successfully deleted by user {UserId}", id, userId);

                var username = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
                return Ok(new { DeletedNoteId = id, Username = username });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting note {NoteId} for user {UserId}", id, userId);
                return StatusCode(500, "An unexpected error occurred while deleting the note.");
            }
        }

        [HttpPost("{id}/restore")]
        public async Task<IActionResult> RestoreNote(string id)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Unauthorized restore attempt on note {NoteId} at {Timestamp}", id, DateTime.UtcNow);
                return Unauthorized("Invalid or missing user ID.");
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Restore attempt with empty note ID by user {UserId} at {Timestamp}", userId, DateTime.UtcNow);
                return BadRequest("Note ID is required.");
            }

            _logger.LogInformation("User {UserId} attempting to restore note {NoteId} at {Timestamp}", userId, id, DateTime.UtcNow);

            try
            {
                var restored = await _noteService.RestoreNoteAsync(id, userId);
                if (!restored)
                {
                    _logger.LogWarning("Note {NoteId} not found or not deleted for user {UserId}", id, userId);
                    return NotFound();
                }

                var note = await _noteService.GetNoteByIdAsync(id, userId);
                var message = note != null ? $"{note.Title} is restored" : "Note is restored";
                var notifications = await _notificationService.SendNotificationAsync(userId, message);
                await _notificationHub.Clients.User(userId).SendCoreAsync("ReceiveNotification", new object[] { notifications });

                _logger.LogInformation("Note {NoteId} successfully restored by user {UserId}", id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while restoring note {NoteId} for user {UserId}", id, userId);
                return StatusCode(500, "An unexpected error occurred while restoring the note.");
            }
        }

    }
}
