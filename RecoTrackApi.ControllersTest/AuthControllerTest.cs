using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using RecoTrackApi.Controllers;
using RecoTrackApi.Services;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories;
using RecoTrackApi.Repositories.Interfaces;
namespace RecoTrackApi.ControllersTest
{
    public class AuthControllerTest
    {
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IAuthService> _authServiceMock = new();
        private readonly Mock<ILogger<AuthController>> _loggerMock = new();
        private readonly Mock<ILogRepository> _logRepoMock = new();
        private readonly AuthController _controller;

        public AuthControllerTest()
        {
            _controller = new AuthController(_userRepoMock.Object, _authServiceMock.Object, _loggerMock.Object, _logRepoMock.Object);
        }

        [Fact(Skip = "Fixing in progress")]
        public async Task Login_ReturnsUnauthorized_WhenLoginFails()
        {
            var loginRequest = new LoginRequest { Email = "test@example.com", Password = "wrong" };
            _authServiceMock.Setup(s => s.LoginAsync(loginRequest)).ReturnsAsync(LoginResult.Fail("Invalid credentials"));
            var result = await _controller.Login(loginRequest);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid credentials", unauthorized.Value);
        }

        [Fact(Skip = "Fixing in progress")]
        public async Task Register_ReturnsBadRequest_WhenRegisterFails()
        {
            var registerRequest = new RegisterRequest { Email = "test@example.com", Password = "pw", ConfirmPassword = "pw" };
            _authServiceMock.Setup(s => s.RegisterAsync(registerRequest)).ReturnsAsync(RegisterResult.Fail("Email exists"));
            var result = await _controller.Register(registerRequest);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Email exists", badRequest.Value);
        }
    }
}
