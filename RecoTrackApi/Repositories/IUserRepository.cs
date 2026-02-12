using RecoTrack.Application.Models.Users;

namespace RecoTrackApi.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(string id);
        Task CreateUserAsync(User user);
        Task UpdatePasswordHashAsync(string email, string passwordHash);
        Task UpdateAvatarUrlAsync(string userId, string? avatarUrl);
        Task UpdatePasswordAndClearOAuthFlagAsync(string email, string passwordHash);
    }
}
