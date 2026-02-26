using RecoTrackApi.DTOs;
using RecoTrack.Application.Models.Notes;

namespace RecoTrackApi.Repositories.Interfaces
{
    public interface INoteRepository
    {
        Task<List<Note>> GetNotesByUserIdAsync(string userId);
        Task<List<Note>> GetNotesByUserIdAsync(string userId, string? filter, string? search);
        Task<List<Note>> GetDeletedNotesByUserIdAsync(string userId);
        Task<Note?> GetNoteByIdAsync(string id, string userId);
        Task CreateNoteAsync(Note note);
        Task<bool> UpdateNoteAsync(Note note);
        Task<bool> DeleteNoteAsync(string id, string userId);
        Task<bool> RestoreNoteAsync(string id, string userId);
        Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate);
        Task<List<Note>> GetNotesByDateAsync(string userId, DateTime date);
        Task<int> GetNoteStreakAsync(string userId);

        Task<List<Note>> GetAllFavouriteNotesByUserIdAsync(string userId);
        Task<List<Note>> GetAllImportantNotesByUserIdAsync(string userId);

        // New for sharing
        Task<List<(Note note, NoteShare share)>> GetNotesSharedWithUserAsync(string userId);
        Task<bool> IsUserAuthorizedForNoteAsync(string noteId, string userId);

        // New: create/remove share
        Task<bool> CreateNoteShareAsync(string noteId, string sharedWithUserId, string sharedByUserId, string permission);
        Task<bool> RemoveNoteShareAsync(string noteId, string sharedWithUserId);
    }
}
