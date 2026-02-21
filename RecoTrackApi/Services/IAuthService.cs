using RecoTrack.Application.Models.Users;
using RecoTrackApi.Models;
using System.Threading;

namespace RecoTrackApi.Services
{
    public interface IAuthService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
        string GenerateJwtToken(User user);
        Task<RegisterResult> RegisterAsync(RegisterRequest request);
        Task<LoginResult> LoginAsync(LoginRequest request);
        Task<PasswordOtpResult> SendPasswordResetOtpAsync(string email, CancellationToken cancellationToken = default);
        Task<PasswordOtpVerificationResult> VerifyPasswordResetOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
        Task<LoginResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
        Task<LoginResult> VerifyMfaAsync(string successCode, string otp);
    }
}
