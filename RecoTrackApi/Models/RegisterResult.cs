using RecoTrack.Application.Models.Users;
using System;

namespace RecoTrackApi.Models
{
    public class RegisterResult
    {
        public bool Success { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? Token { get; private set; }
        public User User { get; private set; } = null!;

        // New refresh token fields
        public string? RefreshToken { get; private set; }
        public DateTime? RefreshExpiresAtUtc { get; private set; }

        private RegisterResult() { }
        private RegisterResult(bool success, string? errorMessage = null)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }

        public static RegisterResult Ok(string token, string? refreshToken = null, DateTime? refreshExpires = null)
        {
            return new RegisterResult
            {
                Success = true,
                ErrorMessage = "",
                Token = token,
                RefreshToken = refreshToken,
                RefreshExpiresAtUtc = refreshExpires
            };
        }

        public static RegisterResult Fail(string message)
        {
            return new RegisterResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }


}
