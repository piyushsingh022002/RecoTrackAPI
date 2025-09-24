using StudentRoutineTrackerApi.Models;

namespace StudentRoutineTrackerApi.Services
{
    public interface IAuthService
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
        string GenerateJwtToken(User user);
         Task<RegisterResult> RegisterAsync(RegisterRequest request);
        Task<LoginResult> LoginAsync(LoginRequest request);
    }
}
