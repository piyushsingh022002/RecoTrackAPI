using StudentRoutineTrackerApi.Models;
using StudentRoutineTrackerApi.Repositories.Interfaces;
using StudentRoutineTrackerApi.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentRoutineTrackerApi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public async Task SendNotificationAsync(string userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };
            await _notificationRepository.AddNotificationAsync(notification);
        }

        public async Task<List<Notification>> GetNotificationsAsync(string userId)
        {
            return await _notificationRepository.GetNotificationsByUserIdAsync(userId);
        }
    }
}
