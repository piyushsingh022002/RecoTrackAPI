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
    }
}
