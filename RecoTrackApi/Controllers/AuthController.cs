using Hangfire;
using Microsoft.AspNetCore.Mvc;
using RecoTrackApi.DTOs;
using RecoTrackApi.Jobs;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RecoTrackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IBackgroundJobClient _backgroundJob;
        private readonly ILogRepository _logRepository;

        public AuthController(
            IAuthService authService,
            ILogRepository logRepository,
            IBackgroundJobClient backgroundJob)
        {
            _authService = authService;
            _logRepository = logRepository;
            _backgroundJob = backgroundJob;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest? request)
        {
            if (request is null)
                return BadRequest(new AuthResponseDto { Message = "Request body cannot be null" });

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
                return BadRequest(new AuthResponseDto { Message = result.ErrorMessage ?? "failure" });

            if (!string.IsNullOrWhiteSpace(result.Token))
            {
                _backgroundJob.Enqueue<WelcomeEmailJob>(job => job.SendEmailAsync(result.Token, "WELCOME"));
            }

            return Ok(new AuthResponseDto
            {
                Token = result.Token,
                Message = "success"
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest? request)
        {
            if (request is null)
                return BadRequest(new AuthResponseDto { Message = "Request body cannot be null" });

            var result = await _authService.LoginAsync(request);

            if (!result.Success)
                return Unauthorized(new AuthResponseDto { Message = result.ErrorMessage ?? "failure" });

            return Ok(new AuthResponseDto
            {
                Token = result.Token,
                Message = "success"
            });
        }

        [HttpDelete("clearMongoLogs")]
        public async Task<IActionResult> ClearLogs()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            if (userName == "Piyush Singh")
            {
                await _logRepository.ClearLogsAsync();
                return Ok(new { message = "Logs cleared successfully." });
            }

            return Forbid();
        }
    }
}