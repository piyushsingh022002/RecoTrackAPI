namespace RecoTrackApi.Models
{
    public class LoginResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? Token { get; private set; }
        public string? Username { get; private set; }
        public string? Email { get; private set; }
        // OTP for MFA flows (not returned to client directly by controller)
        public string? Otp { get; private set; }

        // New fields for refresh token
        public string? RefreshToken { get; private set; }
        public DateTime? RefreshExpiresAtUtc { get; private set; }

        private LoginResult(bool success, string? token = null, string? username = null, string? email = null, string? errorMessage = null, string? otp = null, string? refreshToken = null, DateTime? refreshExpires = null)
        {
            Success = success;
            Token = token;
            Username = username;
            Email = email;
            ErrorMessage = errorMessage;
            Otp = otp;
            RefreshToken = refreshToken;
            RefreshExpiresAtUtc = refreshExpires;
        }

        public static LoginResult Fail(string message) =>
            new LoginResult(false, errorMessage: message);
        public static LoginResult SuccessResult(string token, string username, string email, string? refreshToken = null, DateTime? refreshExpires = null) =>
            new LoginResult(true, token, username, email, null, null, refreshToken, refreshExpires);
        // Special result when MFA is required: include a temporary token (not the final JWT) and the OTP (for server-side job to send)
        public static LoginResult MfaRequired(string tempToken, string username, string email, string otp) =>
            new LoginResult(false, tempToken, username, email, "MFA_REQUIRED", otp);
    }
}
