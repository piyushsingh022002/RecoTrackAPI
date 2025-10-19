using StudentRoutineTrackerApi.DTOs;

namespace StudentRoutineTrackerApi.Services.Interfaces
{
    public interface IActivityService
    {
        Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate);
    }
}