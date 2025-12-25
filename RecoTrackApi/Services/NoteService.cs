using RecoTrackApi.DTOs;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services.Interfaces;

namespace RecoTrackApi.Services
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;

        public NoteService(INoteRepository noteRepository)
        {
            _noteRepository = noteRepository;
        }

        public async Task<List<Note>> GetNotesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _noteRepository.GetNotesByUserIdAsync(userId);
        }

        public async Task<List<Note>> GetDeletedNotesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _noteRepository.GetDeletedNotesByUserIdAsync(userId);
        }

        public async Task<Note?> GetNoteByIdAsync(string id, string userId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID cannot be null or empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _noteRepository.GetNoteByIdAsync(id, userId);
        }

        public async Task CreateNoteAsync(Note note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note), "Note cannot be null.");

            if (string.IsNullOrWhiteSpace(note.UserId))
                throw new ArgumentException("User ID is required.", nameof(note.UserId));

            if (string.IsNullOrWhiteSpace(note.Title))
                throw new ArgumentException("Note title is required.", nameof(note.Title));

            note.DeletedAt = null;
            await _noteRepository.CreateNoteAsync(note);
        }

        public async Task<bool> UpdateNoteAsync(Note note, string userId)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            if (string.IsNullOrWhiteSpace(note.Id))
                throw new ArgumentException("Note ID cannot be null or empty.", nameof(note.Id));

            var existing = await _noteRepository.GetNoteByIdAsync(note.Id, userId);
            if (existing == null)
                return false;

            note.CreatedAt = existing.CreatedAt;
            note.UserId = userId;

            return await _noteRepository.UpdateNoteAsync(note);
        }

        public async Task<bool> UpdateNoteAsync(string id, NoteUpdateDto updateDto, string userId)
        {
            if (updateDto == null)
                throw new ArgumentNullException(nameof(updateDto));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID cannot be null or empty.", nameof(id));

            var existing = await _noteRepository.GetNoteByIdAsync(id, userId);
            if (existing == null)
                return false;

            existing.Title = updateDto.Title;
            existing.Content = updateDto.Content;
            existing.Tags = updateDto.Tags ?? new List<string>();
            existing.MediaUrls = updateDto.MediaUrls ?? new List<string>();
            existing.UpdatedAt = DateTime.UtcNow;

            return await _noteRepository.UpdateNoteAsync(existing);
        }

        public async Task<bool> DeleteNoteAsync(string id, string userId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _noteRepository.DeleteNoteAsync(id, userId);
        }

        public async Task<bool> RestoreNoteAsync(string id, string userId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID cannot be null or empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _noteRepository.RestoreNoteAsync(id, userId);
        }

        public async Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate)
        {
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
