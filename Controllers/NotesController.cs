using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRoutineTrackerApi.Models;
using StudentRoutineTrackerApi.Services.Interfaces;
using System.Security.Claims;
using YourApp.Models;

namespace StudentRoutineTrackerApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;

        public NotesController(INoteService noteService)
        {
            _noteService = noteService;
        }

        private string? GetUserId() => User.FindFirst("id")?.Value;

        [HttpGet]
        public async Task<IActionResult> GetNotes()
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId is null) return Unauthorized();

            var notes = await _noteService.GetNotesAsync(userId);
            return Ok(notes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNote(string id)
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId is null) return Unauthorized();

            var note = await _noteService.GetNoteByIdAsync(id, userId);
            return note is null ? NotFound() : Ok(note);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNote([FromBody] NoteCreateDto noteDto)
        {
            // var userId = GetUserId();
            // Console.WriteLine("USER ID: " + userId);
            // if (userId is null) return Unauthorized();

            // note.UserId = userId;
            // note.CreatedAt = DateTime.UtcNow;
            // note.UpdatedAt = DateTime.UtcNow;

            var userId = User.FindFirst("userId")?.Value;
            if(string.IsNullOrEmpty(userId))
            {
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

            await _noteService.CreateNoteAsync(note);
            // return StatusCode(201);
            return Ok(note);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(string id, [FromBody] Note updatedNote)
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId is null) return Unauthorized();

            updatedNote.Id = id;

            var success = await _noteService.UpdateNoteAsync(updatedNote, userId);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(string id)
        {
            var userId = User.FindFirst("userId")?.Value;
            if (userId is null) return Unauthorized();

            var success = await _noteService.DeleteNoteAsync(id, userId);
            return success ? NoContent() : NotFound();
        }
    }
}
