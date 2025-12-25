using RecoTrackApi.DTOs;
using RecoTrackApi.Models;

namespace RecoTrackApi.Services.Interfaces
{
    public interface INoteService
    {
        Task<List<Note>> GetNotesAsync(string userId);
        Task<List<Note>> GetDeletedNotesAsync(string userId);
        Task<Note?> GetNoteByIdAsync(string id, string userId);
        Task CreateNoteAsync(Note note);
        Task<bool> UpdateNoteAsync(string id, NoteUpdateDto updateDto, string userId);
        Task<bool> DeleteNoteAsync(string id, string userId);
        Task<bool> RestoreNoteAsync(string id, string userId);
        Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate);
        Task<List<Note>> GetNotesByDateAsync(string userId, DateTime date);
        Task<int> GetNoteStreakAsync(string userId);
    }
}
