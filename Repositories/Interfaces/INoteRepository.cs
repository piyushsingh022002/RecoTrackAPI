using StudentRoutineTrackerApi.DTOs;
using StudentRoutineTrackerApi.Models;
using YourApp.Models;

namespace StudentRoutineTrackerApi.Repositories.Interfaces
{
    public interface INoteRepository
    {
        Task<List<Note>> GetNotesByUserIdAsync(string userId);
        Task<Note?> GetNoteByIdAsync(string id, string userId);
        Task CreateNoteAsync(Note note);
        Task<bool> UpdateNoteAsync(Note note);
        Task<bool> DeleteNoteAsync(string id, string userId);
        Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate);
        Task<List<Note>> GetNotesByDateAsync(string userId, DateTime date);
        Task<int> GetNoteStreakAsync(string userId);
    }
}
