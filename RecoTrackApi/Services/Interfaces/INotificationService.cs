using RecoTrackApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecoTrackApi.Services.Interfaces
{
    public interface INotificationService
    {
        Task<Notification> SendNotificationAsync(string userId, string message);
        Task<List<Notification>> GetNotificationsAsync(string userId);
    }
}
