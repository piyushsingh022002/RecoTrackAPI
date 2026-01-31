using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using RecoTrackApi.Controllers;
using RecoTrackApi.Models;
using RecoTrackApi.Services;
using ApiLogRepository = RecoTrackApi.Repositories.Interfaces.ILogRepository;

namespace RecoTrackApi.ControllersTest
{
    public class AuthControllerTest
    {
        private readonly Mock<IAuthService> _authServiceMock = new();
        private readonly Mock<ApiLogRepository> _logRepoMock = new();
        private readonly Mock<IBackgroundJobClient> _backgroundJobMock = new();
        private readonly AuthController _controller;

        public AuthControllerTest()
        {
            _controller = new AuthController(
                _authServiceMock.Object,
                _logRepoMock.Object,
                _backgroundJobMock.Object);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenLoginFails()
        {
            var loginRequest = new LoginRequest { Email = "test@example.com", Password = "wrong" };
            _authServiceMock.Setup(s => s.LoginAsync(loginRequest)).ReturnsAsync(LoginResult.Fail("Invalid credentials"));
            var result = await _controller.Login(loginRequest);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid credentials", ExtractMessage(unauthorized.Value));
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenRegisterFails()
        {
            var registerRequest = new RegisterRequest
            {
                FullName = "Test User",
                Username = "testuser",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                Password = "pw",
                ConfirmPassword = "pw",
                Dob = DateTime.UtcNow.AddYears(-25),
                SecurityQuestion = "Favorite color?",
                SecurityAnswer = "Blue"
            };

            _authServiceMock.Setup(s => s.RegisterAsync(registerRequest)).ReturnsAsync(RegisterResult.Fail("Email exists"));
            var result = await _controller.Register(registerRequest);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Email exists", ExtractMessage(badRequest.Value));
        }

        private static string? ExtractMessage(object? payload)
        {
            if (payload == null)
                return null;

            return payload.GetType().GetProperty("Message")?.GetValue(payload) as string;
        }
    }
}
