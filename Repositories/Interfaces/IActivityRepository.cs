using StudentRoutineTrackerApi.DTOs;

namespace StudentRoutineTrackerApi.Repositories.Interfaces
{
    public interface IActivityRepository
    {
        Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate);
    }
}