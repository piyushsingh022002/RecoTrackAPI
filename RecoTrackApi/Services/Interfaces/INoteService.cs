using RecoTrackApi.DTOs;
using RecoTrack.Application.Models.Notes;

namespace RecoTrackApi.Services.Interfaces
{
    public interface INoteService
    {
        Task<List<Note>> GetNotesAsync(string userId);
        Task<List<Note>> GetNotesAsync(string userId, string? filter, string? search);
        Task<List<Note>> GetDeletedNotesAsync(string userId);
        Task<Note?> GetNoteByIdAsync(string id, string userId);
        Task CreateNoteAsync(Note note);
        Task<Guid> CreateOrRecordAsync(Note note, string saveOption, string? eventType, string userId);
        // Backward-compatible overload used by tests and controllers
        Task<bool> UpdateNoteAsync(string id, UpdateNoteDto updateDto, string userId);
        // New overload that accepts explicit presentFields (properties present in the JSON body)
        Task<bool> UpdateNoteAsync(string id, UpdateNoteDto updateDto, string userId, IReadOnlyCollection<string>? presentFields);
        Task<bool> DeleteNoteAsync(string id, string userId);
        Task<bool> RestoreNoteAsync(string id, string userId);
        Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate);
        Task<List<Note>> GetNotesByDateAsync(string userId, DateTime date);
        Task<int> GetNoteStreakAsync(string userId);
        Task<List<Note>> GetAllFavouriteNotesAsync(string userId);
        Task<List<Note>> GetAllImportantNotesAsync(string userId);
        // New sharing APIs
        Task<List<SharedNoteDto>> GetNotesSharedWithMeAsync(string userId);
        Task<bool> IsUserAuthorizedForNoteAsync(string noteId, string userId);
        Task<bool> ShareNoteAsync(string noteId, string sharedByUserId, string sharedWithUserId, string permission);
        Task<bool> ShareNoteByEmailsAsync(string noteId, string sharedByUserId, string sharedWithEmails, string permission);
        Task<bool> UnshareNoteAsync(string noteId, string sharedByUserId, string sharedWithUserId);
    }
}
