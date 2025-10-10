using MongoDB.Driver;
using StudentRoutineTrackerApi.Models;
using StudentRoutineTrackerApi.Repositories.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StudentRoutineTrackerApi.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IMongoCollection<Notification> _notifications;

        public NotificationRepository(IMongoDatabase database)
        {
            _notifications = database.GetCollection<Notification>("Notifications");
        }

        public async Task AddNotificationAsync(Notification notification)
        {
            await _notifications.InsertOneAsync(notification);
        }

        public async Task<List<Notification>> GetNotificationsByUserIdAsync(string userId)
        {
            return await _notifications.Find(n => n.UserId == userId)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
    }
}
