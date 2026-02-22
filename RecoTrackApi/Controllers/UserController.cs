using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecoTrack.Application.Interfaces;
using RecoTrackApi.DTOs;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories;

namespace RecoTrackApi.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IObjectStorageService _objectStorageService;

        private static readonly IReadOnlySet<string> AllowedAvatarExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

        private static readonly IReadOnlyDictionary<string, string> ContentTypeExtensionMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = "jpg",
            ["image/png"] = "png",
            ["image/webp"] = "webp"
        };

        private const long MaxAvatarSizeBytes = 5 * 1024 * 1024; // 5MB
        private static readonly TimeSpan AvatarUploadExpiry = TimeSpan.FromMinutes(15);

        public UserController(IUserRepository userRepository, IObjectStorageService objectStorageService)
        {
            _userRepository = userRepository;
            _objectStorageService = objectStorageService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user is null)
                return NotFound(new { Message = "User not found." });

            var responseUser = new RecoTrack.Application.Models.Users.User
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Dob = user.Dob,
                PasswordHash = string.Empty,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Profile = user.Profile,
                AuthProviders = user.AuthProviders,
                IsMfaEnabled = user.IsMfaEnabled
            };

            return Ok(responseUser);
        }

        [Authorize]
        [HttpPost("avatar/upload-url")]
        public async Task<IActionResult> RequestAvatarUploadUrl([FromBody] AvatarUploadUrlRequest? request)
        {
            if (request is null)
                return BadRequest(new { Message = "Request body is required." });

            if (string.IsNullOrWhiteSpace(request.ContentType) || string.IsNullOrWhiteSpace(request.FileName))
                return BadRequest(new { Message = "File name and content type are required." });

            if (request.FileSizeBytes <= 0 || request.FileSizeBytes > MaxAvatarSizeBytes)
                return BadRequest(new { Message = "File size must be between 1 byte and 5MB." });

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user is null)
                return NotFound(new { Message = "User not found." });

            var extension = ResolveAvatarExtension(request);
            if (string.IsNullOrWhiteSpace(extension))
                return BadRequest(new { Message = "Unsupported file type. Allowed types: jpg, png, webp." });

            var objectKey = $"avatars/{userId}/avatar.{extension}";
            var uploadResult = await _objectStorageService.GenerateUploadUrlAsync(objectKey, request.ContentType, AvatarUploadExpiry);

            return Ok(new AvatarUploadUrlResponse
            {
                UploadUrl = uploadResult.UploadUrl,
                PublicUrl = uploadResult.PublicUrl  
            });
        }

        [Authorize]
        [HttpPut("avatar")]
        public async Task<IActionResult> UpdateAvatar([FromBody] AvatarUpdateRequest? request)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.AvatarUrl))
                return BadRequest(new { Message = "AvatarUrl is required." });

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user is null)
                return NotFound(new { Message = "User not found." });

            var trimmedUrl = request.AvatarUrl.Trim();
            await _userRepository.UpdateAvatarUrlAsync(userId, trimmedUrl);

            return Ok(new { AvatarUrl = trimmedUrl });
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        private static string? ResolveAvatarExtension(AvatarUploadUrlRequest request)
        {
            var extension = Path.GetExtension(request.FileName ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                extension = extension.TrimStart('.');
                if (extension.Equals("jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    extension = "jpg";
                }

                if (AllowedAvatarExtensions.Contains($".{extension}"))
                {
                    return extension.ToLowerInvariant();
                }
            }

            if (ContentTypeExtensionMap.TryGetValue(request.ContentType, out var mapped))
            {
                return mapped;
            }

            return null;
        }
    }
}
