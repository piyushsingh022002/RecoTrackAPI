using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using RecoTrackApi.Controllers;
using RecoTrackApi.Services.Interfaces;
using RecoTrackApi.DTOs;
using RecoTrackApi.Models;

namespace RecoTrackApi.ControllersTest
{
    public class ActivityControllerTest
    {
        private readonly Mock<IActivityService> _activityServiceMock = new();
        private readonly Mock<INoteService> _noteServiceMock = new();
        private readonly Mock<ILogger<ActivityController>> _loggerMock = new();
        private readonly ActivityController _controller;
        //git checking
        public ActivityControllerTest()
        {
            _controller = new ActivityController(_activityServiceMock.Object, _noteServiceMock.Object, _loggerMock.Object);
        }

        [Fact(Skip = "Fixing in progress")]
        public async Task GetNoteActivity_ReturnsOk_WithNoteActivity()
        {
            var userId = "user123";
            var startDate = DateTime.UtcNow.AddDays(-7);
            var endDate = DateTime.UtcNow;
            var activities = new List<NoteActivityDto> { new NoteActivityDto { Date = startDate, NoteCount = 5 } };
            _activityServiceMock.Setup(s => s.GetNoteActivityAsync(userId, startDate, endDate)).ReturnsAsync(activities);

            // Simulate user context
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            httpContext.User = new System.Security.Claims.ClaimsPrincipal(
                new System.Security.Claims.ClaimsIdentity(new[] {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId)
                })
            );
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var result = await _controller.GetNoteActivity(startDate, endDate);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedActivities = Assert.IsType<List<NoteActivityDto>>(okResult.Value);
            Assert.Single(returnedActivities);
            Assert.Equal(5, returnedActivities[0].NoteCount);
        }
    }
}
