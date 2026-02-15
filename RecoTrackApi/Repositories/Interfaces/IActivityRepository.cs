using RecoTrackApi.DTOs;

namespace RecoTrackApi.Repositories.Interfaces
{
    public interface IActivityRepository
    {
        Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate);

        // New method to record an activity
        Task RecordActivityAsync(string userId, Guid noteRefId, string eventType);
    }
}