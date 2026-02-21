using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using RecoTrackApi.DTOs;
using RecoTrackApi.Services.Interfaces;
using System.Security.Claims;
using RecoTrack.Infrastructure.ServicesV2;
using RecoTrack.Shared.Settings;
using Microsoft.Extensions.Options;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Models;
using System.Text;

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
        private readonly EmailService? _emailService;
        private readonly BrevoSettings _brevoSettings;
        private readonly ISupportRequestRepository? _supportRepo;

        // Keep original constructor signature backwards-compatible by providing optional new deps
        public ActivityController(IActivityService activityService, INoteService noteService, ILogger<ActivityController> logger, EmailService? emailService = null, IOptions<BrevoSettings>? brevoOptions = null, ISupportRequestRepository? supportRepo = null)
        {
            _activityService = activityService;
            _noteService = noteService;
            _logger = logger;
            _emailService = emailService;
            _brevoSettings = brevoOptions?.Value ?? new BrevoSettings();
            _supportRepo = supportRepo;
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        private string? GetUserEmail() => User.FindFirst(ClaimTypes.Email)?.Value;
        private string? GetUserName() => User.FindFirst(ClaimTypes.Name)?.Value;

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

        // New endpoint to submit support/feedback/help request
        public class SupportRequestDto
        {
            public string Category { get; set; } = string.Empty; // CustomerSupport, Feedback, Help
            public string Subject { get; set; } = string.Empty;
            public string IssueType { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
        }

        [HttpPost("support")]
        public async Task<IActionResult> SubmitSupport([FromBody] SupportRequestDto request)
        {
            var userId = GetUserId();
            var userEmail = GetUserEmail();
            var userName = GetUserName();

            if (userId is null)
            {
                return Unauthorized(new ApiResponse<object> { Success = false, Message = "Invalid or missing user ID in token." });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Category) || string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new ApiResponse<object> { Success = false, Message = "Category, Subject and Message are required." });
            }

            // Persist to DB if repository available
            var entry = new SupportRequestEntry
            {
                UserId = userId,
                UserEmail = userEmail,
                Category = request.Category,
                Subject = request.Subject,
                IssueType = request.IssueType,
                Message = request.Message,
                CreatedAtUtc = DateTime.UtcNow
            };

            if (_supportRepo != null)
            {
                try
                {
                    await _supportRepo.SaveAsync(entry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save support request for user {UserId}", userId);
                    return StatusCode(500, new ApiResponse<object> { Success = false, Message = "Failed to save request." });
                }
            }

            // Build email to admin
            var adminEmail = "workspace.piyush01@gmail.com";
            var ownerSubject = $"[{request.Category}] {request.Subject} - from user {userEmail ?? userId}";
            var sb = new StringBuilder();
            sb.Append($"<p><strong>UserId:</strong> {System.Net.WebUtility.HtmlEncode(userId)}</p>");
            sb.Append($"<p><strong>UserEmail:</strong> {System.Net.WebUtility.HtmlEncode(userEmail)}</p>");
            sb.Append($"<p><strong>Category:</strong> {System.Net.WebUtility.HtmlEncode(request.Category)}</p>");
            sb.Append($"<p><strong>Issue Type:</strong> {System.Net.WebUtility.HtmlEncode(request.IssueType)}</p>");
            sb.Append($"<p><strong>Message:</strong><br/>{System.Net.WebUtility.HtmlEncode(request.Message).Replace("\n", "<br/>")}</p>");

            var ownerHtml = CommonEmailTemplate.BuildHtml(ownerSubject, sb.ToString(), "Reply to user", $"mailto:{System.Net.WebUtility.HtmlEncode(userEmail ?? string.Empty)}");

            if (_emailService != null)
            {
                try
                {
                    await _emailService.SendCustomEmailAsync(adminEmail, "Admin", ownerSubject, ownerHtml, null, _brevoSettings.SenderName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send support email to admin for user {UserId}", userId);
                    // don't fail; continue to send ack to user
                }
            }

            // Send acknowledgement to user using existing template mechanism
            if (!string.IsNullOrWhiteSpace(userEmail) && _emailService != null)
            {
                var ackSubject = "We received your request";
                var ackSb = new StringBuilder();
                var recipientName = !string.IsNullOrWhiteSpace(userName) ? userName : (userEmail.Contains("@") ? userEmail.Split('@')[0] : userEmail);
                ackSb.Append($"<p>Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>");
                ackSb.Append("<p>Thanks for contacting RecoTrack. We have received your message and will get back to you shortly.</p>");
                ackSb.Append("<p>Best,<br/>RecoTrack Team</p>");

                var ackHtml = CommonEmailTemplate.BuildHtml(ackSubject, ackSb.ToString(), "Visit RecoTrack", "https://recotrackpiyushsingh.vercel.app/");

                try
                {
                    await _email_service_senduser(userEmail, recipientName, ackSubject, ackHtml);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send acknowledgement email to {Email}", userEmail);
                }
            }

            return Ok(new ApiResponse<object> { Success = true, Message = "Support request submitted." });
        }

        // helper to reuse EmailService SendCustomEmailAsync
        private Task _email_service_senduser(string toEmail, string toName, string subject, string html)
        {
            if (_emailService == null)
                return Task.CompletedTask;

            return _emailService.SendCustomEmailAsync(toEmail, toName, subject, html, null, _brevoSettings.SenderName);
        }
    }
}