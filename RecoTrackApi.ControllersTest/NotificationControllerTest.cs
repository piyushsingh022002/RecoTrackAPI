using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using StudentRoutineTrackerApi.Controllers;
using StudentRoutineTrackerApi.Services.Interfaces;
using StudentRoutineTrackerApi.Models;
using System.Collections.Generic;

namespace StudentRoutineTracketApi.ControllersTest
{
    public class NotificationControllerTest
    {
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly NotificationController _controller;

        public NotificationControllerTest()
        {
            _controller = new NotificationController(_notificationServiceMock.Object);
        }

        [Fact]
        public async Task GetNotifications_ReturnsOk_WithNotifications()
        {
            var userId = "user123";
            var notifications = new List<Notification> { new Notification { Id = "1", UserId = userId, Message = "Test" } };
            _notificationServiceMock.Setup(s => s.GetNotificationsAsync(userId)).ReturnsAsync(notifications);

            // Simulate user context
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new[] {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
                })
            );
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await _controller.GetNotifications();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedNotifications = Assert.IsType<List<Notification>>(okResult.Value);
            Assert.Single(returnedNotifications);
            Assert.Equal("Test", returnedNotifications[0].Message);
        }

        // Remove invalid test for GetNotification
    }
}
