namespace RecoTrackApi.DTOs
{
    public class AvatarUploadUrlRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
    }

    public sealed class AvatarUploadUrlResponse
    {
        public string UploadUrl { get; init; } = string.Empty;
        public string PublicUrl { get; init; } = string.Empty;
    }

    public sealed class AvatarUpdateRequest
    {
        public string AvatarUrl { get; set; } = string.Empty;
    }

    public sealed class UserProfileResponseDto
    {
        public string Username { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public string? AvatarUrl { get; init; }
    }
}
