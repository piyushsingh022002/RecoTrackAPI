using RecoTrackApi.DTOs;

namespace RecoTrackApi.Repositories.Interfaces
{
    public interface IActivityRepository
    {
        Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate);
    }
}