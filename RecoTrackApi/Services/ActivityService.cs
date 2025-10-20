using RecoTrackApi.DTOs;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services.Interfaces;

namespace RecoTrackApi.Services
{
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _activityRepository;

        public ActivityService(IActivityRepository activityRepository)
        {
            _activityRepository = activityRepository;
        }

        public async Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate)
        {
            return await _activityRepository.GetNoteActivityAsync(userId, startDate, endDate);
        }
    }
}