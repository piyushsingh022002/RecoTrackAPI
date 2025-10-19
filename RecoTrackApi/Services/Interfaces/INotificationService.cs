using StudentRoutineTrackerApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentRoutineTrackerApi.Services.Interfaces
{
    public interface INotificationService
    {
        Task<Notification> SendNotificationAsync(string userId, string message);
        Task<List<Notification>> GetNotificationsAsync(string userId);
    }
}
