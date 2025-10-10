using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace StudentRoutineTrackerApi.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendToUser(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }
    }
}
