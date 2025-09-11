using StudentRoutineTrackerApi.DTOs;
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

        public async Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate)
        {
            // You may want to inject IActivityRepository instead for separation, but for now, call NoteRepository if it implements the method
            if (_noteRepository is IActivityRepository activityRepo)
                return await activityRepo.GetNoteActivityAsync(userId, startDate, endDate);
            throw new NotImplementedException("Note activity not implemented in repository");
        }

        public async Task<List<Note>> GetNotesByDateAsync(string userId, DateTime date)
        {
            return await _noteRepository.GetNotesByDateAsync(userId, date);
        }

        public async Task<int> GetNoteStreakAsync(string userId)
        {
            return await _noteRepository.GetNoteStreakAsync(userId);
        }
    }
}
