using RecoTrackApi.DTOs;
using RecoTrack.Application.Models.Notes;
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
                throw new ArgumentException("User ID is required.", nameof(userId));

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
                throw new ArgumentException("Note ID is required.", nameof(id));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            return await _noteRepository.GetNoteByIdAsync(id, userId);
        }

        public async Task CreateNoteAsync(Note note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            if (string.IsNullOrWhiteSpace(note.UserId))
                throw new ArgumentException("User ID is required.", nameof(note.UserId));

            if (string.IsNullOrWhiteSpace(note.Title))
                throw new ArgumentException("Title is required.", nameof(note.Title));

            note.DeletedAt = null;
            await _noteRepository.CreateNoteAsync(note);
        }

        public async Task<bool> UpdateNoteAsync(string noteId, UpdateNoteDto updateDto, string userId)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            var existingNote = await _noteRepository.GetNoteByIdAsync(noteId, userId);
            if (existingNote == null)
                return false;

            // Map only fields that can be updated
            if (updateDto.Title != null) existingNote.Title = updateDto.Title.Trim();
            if (updateDto.Content != null) existingNote.Content = updateDto.Content.Trim();
            if (updateDto.Tags != null) existingNote.Tags = updateDto.Tags;
            if (updateDto.Labels != null) existingNote.Labels = updateDto.Labels;
            if (updateDto.MediaUrls != null) existingNote.MediaUrls = updateDto.MediaUrls;
            if (updateDto.Status != null) existingNote.Status = updateDto.Status;
            if (updateDto.IsLocked.HasValue) existingNote.IsLocked = updateDto.IsLocked.Value;
            if (updateDto.ReminderAt.HasValue) existingNote.ReminderAt = updateDto.ReminderAt;

            existingNote.UpdatedAt = DateTime.UtcNow;
            return await _noteRepository.UpdateNoteAsync(existingNote);
        }

        public async Task<bool> DeleteNoteAsync(string id, string userId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            return await _noteRepository.DeleteNoteAsync(id, userId);
        }

        public async Task<bool> RestoreNoteAsync(string id, string userId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

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

        public async Task<List<Note>> GetAllFavouriteNotesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            return await _noteRepository.GetAllFavouriteNotesByUserIdAsync(userId);
        }

        public async Task<List<Note>> GetAllImportantNotesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            return await _noteRepository.GetAllImportantNotesByUserIdAsync(userId);
        }



    }
}
