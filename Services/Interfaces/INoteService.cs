using StudentRoutineTrackerApi.DTOs;
using StudentRoutineTrackerApi.Models;
using YourApp.Models;

namespace StudentRoutineTrackerApi.Services.Interfaces
{
    public interface INoteService
    {
        Task<List<Note>> GetNotesAsync(string userId);
        Task<Note?> GetNoteByIdAsync(string id, string userId);
        Task CreateNoteAsync(Note note);
        Task<bool> UpdateNoteAsync(Note note, string userId);
        Task<bool> DeleteNoteAsync(string id, string userId);
        Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate);
        Task<List<Note>> GetNotesByDateAsync(string userId, DateTime date);
        Task<int> GetNoteStreakAsync(string userId);
    }
}
