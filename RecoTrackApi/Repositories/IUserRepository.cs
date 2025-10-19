using StudentRoutineTrackerApi.Models;

namespace StudentRoutineTrackerApi.Repositories
{
    public interface IUserRepository
    {
        Task<User> GetByEmailAsync(string email);
        Task CreateUserAsync(User user);
    }
}
