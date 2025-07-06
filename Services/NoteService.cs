using StudentRoutineTrackerApi.Models;
using StudentRoutineTrackerApi.Repositories.Interfaces;
using StudentRoutineTrackerApi.Services.Interfaces;
using YourApp.Models;

namespace StudentRoutineTrackerApi.Services
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;

        public NoteService(INoteRepository noteRepository)
        {
            _noteRepository = noteRepository;
        }

        public async Task<List<Note>> GetNotesAsync(string userId) =>
            await _noteRepository.GetNotesByUserIdAsync(userId);

        public async Task<Note?> GetNoteByIdAsync(string id, string userId) =>
            await _noteRepository.GetNoteByIdAsync(id, userId);

        public async Task CreateNoteAsync(Note note) =>
            await _noteRepository.CreateNoteAsync(note);

        public async Task<bool> UpdateNoteAsync(Note note, string userId)
        {
            var existing = await _noteRepository.GetNoteByIdAsync(note.Id!, userId);
            if (existing == null) return false;

            note.UserId = userId;
            note.CreatedAt = existing.CreatedAt;
            return await _noteRepository.UpdateNoteAsync(note);
        }

        public async Task<bool> DeleteNoteAsync(string id, string userId) =>
            await _noteRepository.DeleteNoteAsync(id, userId);
    }
}
