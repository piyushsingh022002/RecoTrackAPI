using System.Collections.Generic;
using System.Threading.Tasks;
using RecoTrackApi.Models;

namespace RecoTrackApi.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task AddNotificationAsync(Notification notification);
        Task<List<Notification>> GetNotificationsByUserIdAsync(string userId);
    }
}
