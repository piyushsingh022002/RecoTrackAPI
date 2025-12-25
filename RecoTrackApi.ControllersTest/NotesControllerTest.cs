using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrackApi.ControllersTest
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;
    using RecoTrackApi.Controllers;
    using RecoTrackApi.DTOs;
    using RecoTrackApi.Models;
    using RecoTrackApi.Services.Interfaces;
    using Microsoft.AspNetCore.SignalR;
    using System.Security.Claims;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class NotesControllerTest
    {
        private readonly Mock<INoteService> _noteServiceMock = new();
        private readonly Mock<ILogger<NotesController>> _loggerMock = new();
        private readonly Mock<INotificationService> _notificationServiceMock = new();
        private readonly Mock<IHubContext<RecoTrackApi.Hubs.NotificationHub>> _hubContextMock = new();
        private readonly NotesController _controller;

        public NotesControllerTest()
        {
            _controller = new NotesController(
                _noteServiceMock.Object,
                _loggerMock.Object,
                _notificationServiceMock.Object,
                _hubContextMock.Object
            );
        }

        private void SetUser(string userId)
        {
            var httpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity(new[] {
                    new Claim(ClaimTypes.NameIdentifier, userId)
                })
            );
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task GetNotes_ReturnsUnauthorized_WhenUserIdIsNull()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext() };

            // Act
            var result = await _controller.GetNotes();

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task GetNotes_ReturnsOk_WithNotes()
        {
            // Arrange
            var userId = "user123";
            var notes = new List<Note> { new Note { Id = "1", UserId = userId, Title = "Test Note", Content = "Content" } };
            _noteServiceMock.Setup(s => s.GetNotesAsync(userId)).ReturnsAsync(notes);
            SetUser(userId);

            // Act
            var result = await _controller.GetNotes();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedNotes = Assert.IsType<List<Note>>(okResult.Value);
            Assert.Single(returnedNotes);
            Assert.Equal("Test Note", returnedNotes[0].Title);
        }

        [Fact]
        public async Task GetNote_ReturnsNotFound_WhenNoteDoesNotExist()
        {
            // Arrange
            var userId = "user123";
            _noteServiceMock.Setup(s => s.GetNoteByIdAsync("noteId", userId)).ReturnsAsync((Note?)null);
            SetUser(userId);

            // Act
            var result = await _controller.GetNote("noteId");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreateNote_ReturnsBadRequest_WhenTitleIsMissing()
        {
            // Arrange
            var userId = "user123";
            var noteDto = new NoteCreateDto { Title = null };
            SetUser(userId);

            // Act
            var result = await _controller.CreateNote(noteDto);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Title is required.", badRequest.Value);
        }

        [Fact]
        public async Task UpdateNote_ReturnsNotFound_WhenNoteNotUpdated()
        {
            // Arrange
            var userId = "user123";
            var updateDto = new NoteUpdateDto { Title = "Updated", Content = "", Tags = new List<string>(), MediaUrls = new List<string>(), CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _noteServiceMock.Setup(s => s.UpdateNoteAsync("noteId", updateDto, userId)).ReturnsAsync(false);
            SetUser(userId);

            // Act
            var result = await _controller.UpdateNote("noteId", updateDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteNote_ReturnsBadRequest_WhenIdIsEmpty()
        {
            // Arrange
            var userId = "user123";
            SetUser(userId);

            // Act
            var result = await _controller.DeleteNote("");

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Note ID is required.", badRequest.Value);
        }
    }
}
