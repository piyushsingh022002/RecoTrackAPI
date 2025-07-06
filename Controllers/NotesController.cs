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
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var notes = await _noteService.GetNotesAsync(userId);
            return Ok(notes);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNote(string id)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var note = await _noteService.GetNoteByIdAsync(id, userId);
            return note is null ? NotFound() : Ok(note);
        }

        [HttpPost]
        public async Task<IActionResult> CreateNote([FromBody] Note note)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            note.UserId = userId;
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;

            await _noteService.CreateNoteAsync(note);
            return StatusCode(201);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(string id, [FromBody] Note updatedNote)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            updatedNote.Id = id;

            var success = await _noteService.UpdateNoteAsync(updatedNote, userId);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(string id)
        {
            var userId = GetUserId();
            if (userId is null) return Unauthorized();

            var success = await _noteService.DeleteNoteAsync(id, userId);
            return success ? NoContent() : NotFound();
        }
    }
}
