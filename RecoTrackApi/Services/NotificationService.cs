using Microsoft.AspNetCore.SignalR;
using RecoTrackApi.Hubs;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RecoTrackApi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(INotificationRepository notificationRepository, IHubContext<NotificationHub> hubContext)
        {
            _notificationRepository = notificationRepository;
            _hubContext = hubContext;
        }

        public async Task<Notification> SendNotificationAsync(string userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };

            await _notificationRepository.AddNotificationAsync(notification);
            return notification;
        }

        public async Task<List<Notification>> GetNotificationsAsync(string userId)
        {
            return await _notificationRepository.GetNotificationsByUserIdAsync(userId);
        }
    }
}
