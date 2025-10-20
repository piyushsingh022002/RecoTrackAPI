using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using RecoTrackApi.DTOs;
using RecoTrackApi.Services.Interfaces;
using System.Security.Claims;

namespace RecoTrackApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ActivityController : ControllerBase
    {
        private readonly IActivityService _activityService;
        private readonly INoteService _noteService;
        private readonly ILogger<ActivityController> _logger;

        public ActivityController(IActivityService activityService, INoteService noteService, ILogger<ActivityController> logger)
        {
            _activityService = activityService;
            _noteService = noteService;
            _logger = logger;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        [HttpGet]
        public async Task<IActionResult> GetNoteActivity([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                _logger.LogWarning("Note activity request with invalid user ID");
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid or missing user ID in token." });
            }

            if (endDate < startDate)
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "endDate must be after startDate." });
            }

            _logger.LogInformation("User {UserId} requested note activity from {Start} to {End}", userId, startDate, endDate);
            var activity = await _activityService.GetNoteActivityAsync(userId, startDate, endDate);
            return Ok(new ApiResponse<List<NoteActivityDto>>
            {
                Success = true,
                Data = activity,
                Message = "Note activity fetched successfully."
            });
        }

        // New endpoint: Get all notes for a specific date
        [HttpGet("notes-by-date")]
        public async Task<IActionResult> GetNotesByDate([FromQuery] DateTime date)
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid or missing user ID in token." });
            }
            var notes = await _noteService.GetNotesByDateAsync(userId, date);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = notes,
                Message = "Notes fetched for the selected date."
            });
        }

        // New endpoint: Get current note creation streak
        [HttpGet("streak")]
        public async Task<IActionResult> GetNoteStreak()
        {
            var userId = GetUserId();
            if (userId is null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid or missing user ID in token." });
            }
            var streak = await _noteService.GetNoteStreakAsync(userId);
            return Ok(new ApiResponse<int>
            {
                Success = true,
                Data = streak,
                Message = "Current note creation streak fetched successfully."
            });
        }
    }
}