using RecoTrackApi.Models;

namespace RecoTrackApi.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByEmailAsync(string email);
        Task CreateUserAsync(User user);
    }
}
