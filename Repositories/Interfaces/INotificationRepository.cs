using System.Collections.Generic;
using System.Threading.Tasks;
using StudentRoutineTrackerApi.Models;

namespace StudentRoutineTrackerApi.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task AddNotificationAsync(Notification notification);
        Task<List<Notification>> GetNotificationsByUserIdAsync(string userId);
    }
}
