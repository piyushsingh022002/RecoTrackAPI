using RecoTrackApi.DTOs;

namespace RecoTrackApi.Services.Interfaces
{
    public interface IActivityService
    {
        Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate);
    }
}