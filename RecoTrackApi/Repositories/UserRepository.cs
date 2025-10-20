using MongoDB.Driver;
using RecoTrackApi.Models;
using RecoTrackApi.Services;

namespace RecoTrackApi.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IMongoCollection<User> _users;

        public UserRepository(IMongoDbService mongoDbService)
        {
            var database = mongoDbService.GetDatabase();
            _users = database.GetCollection<User>("Users");
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        }

        public async Task CreateUserAsync(User user)
        {
            await _users.InsertOneAsync(user);
        }
    }
}
