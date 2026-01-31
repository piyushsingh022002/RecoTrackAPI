namespace RecoTrackApi.DTOs
{
    public class AuthResponseDto
    {
        public string? Token { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
