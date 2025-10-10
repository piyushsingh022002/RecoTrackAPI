using StudentRoutineTrackerApi.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentRoutineTrackerApi.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string message);
        Task<List<Notification>> GetNotificationsAsync(string userId);
    }
}
