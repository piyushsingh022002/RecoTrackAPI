using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRoutineTrackerApi.Models;
using StudentRoutineTrackerApi.Services.Interfaces;
using System.Security.Claims;
using YourApp.Models;
using Serilog;

namespace StudentRoutineTrackerApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;
        private readonly ILogger<NotesController> _logger;

        public NotesController(INoteService noteService, ILogger<NotesController> logger)
        {
            _noteService = noteService;
            _logger = logger;
        }

        private string? GetUserId() => User.FindFirst("userId")?.Value;

        [HttpGet]
        public async Task<IActionResult> GetNotes()
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            _logger.LogInformation("User {UserId} requested all notes at {Timestamp}", userId, DateTime.UtcNow);
            var notes = await _noteService.GetNotesAsync(userId);
            _logger.LogInformation("Returning {Count} notes for user {UserId}", notes.Count, userId);
            return Ok(notes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNote(string id)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

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
            if(string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Create note attempt with invalid user ID");
                return Unauthorized("Invalid or missing user ID in token.");
            }

            var note = new Note
            {
                UserId = userId,
                Title = noteDto.Title,
                Content = noteDto.Content,
                Tags = noteDto.Tags ?? new List<string>(),
                MediaUrls = noteDto.MediaUrls ?? new List<string>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("User {UserId} creating new note with title '{Title}' at {Timestamp}", 
                userId, note.Title, DateTime.UtcNow);

            await _noteService.CreateNoteAsync(note);
            _logger.LogInformation("Successfully created note {NoteId} for user {UserId}", note.Id, userId);
            return Ok(note);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(string id, [FromBody] Note updatedNote)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            updatedNote.Id = id;
            _logger.LogInformation("User {UserId} updating note {NoteId} at {Timestamp}", userId, id, DateTime.UtcNow);

            var success = await _noteService.UpdateNoteAsync(updatedNote, userId);
            if (!success)
            {
                _logger.LogWarning("Failed to update note {NoteId} for user {UserId}", id, userId);
                return NotFound();
            }

            _logger.LogInformation("Successfully updated note {NoteId} for user {UserId}", id, userId);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(string id)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            _logger.LogInformation("User {UserId} deleting note {NoteId} at {Timestamp}", userId, id, DateTime.UtcNow);

            var success = await _noteService.DeleteNoteAsync(id, userId);
            if (!success)
            {
                _logger.LogWarning("Failed to delete note {NoteId} for user {UserId}", id, userId);
                return NotFound();
            }

            _logger.LogInformation("Successfully deleted note {NoteId} for user {UserId}", id, userId);
            return NoContent();
        }
    }
}
