using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentRoutineTrackerApi.Models;
using StudentRoutineTrackerApi.Repositories;
using StudentRoutineTrackerApi.Services;

namespace StudentRoutineTrackerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IUserRepository userRepository,
            IAuthService authService,
            ILogger<AuthController> logger)
        {
            _userRepository = userRepository;
            _authService = authService;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("user")]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            var name = User.FindFirst(ClaimTypes.Name)?.Value;

            return Ok(new { userId, email, name });
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request.Password != request.ConfirmPassword)
                return BadRequest(new { Message = "Passwords do not match" });

            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return BadRequest(new { Message = "Email is already registered" });

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = _authService.HashPassword(request.Password)
            };

            await _userRepository.CreateUserAsync(user);
            _logger.LogInformation($"User registered with email: {request.Email}");

            return Ok(new { Message = "User registered successfully" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new { Message = "Invalid credentials" });

            if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
                return Unauthorized(new { Message = "Invalid credentials" });

            var token = _authService.GenerateJwtToken(user);
            Console.WriteLine(token);
            _logger.LogInformation($"User logged in: {request.Email}");

            return Ok(new
            {
                Token = token,
                Username = user.Username,
                Email = user.Email
            });
        }
    }
}
// End of file: Controllers/AuthController.cs
// This file contains the AuthController class which handles user registration and login.
// It uses IUserRepository to interact with the user data and IAuthService for password hashing and JWT token generation.
// The controller provides endpoints for registering a new user and logging in an existing user.