using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RecoTrack.Shared.Settings;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;

namespace RecoTrackApi.Repositories
{
    public class SecurityQuestionRepository : ISecurityQuestionRepository
    {
        private readonly IMongoCollection<SecurityQuestionEntry> _collection;

        public SecurityQuestionRepository(IOptions<MongoDbSettings> settings, IMongoDbService mongoDbService)
        {
            var collectionName = settings.Value.SecurityQuestionCollectionName;
            _collection = mongoDbService.GetDatabase().GetCollection<SecurityQuestionEntry>(collectionName);
        }

        public async Task SaveAsync(SecurityQuestionEntry entry)
        {
            if (entry is null)
                throw new ArgumentNullException(nameof(entry));

            entry.CreatedAtUtc = DateTime.UtcNow;
            entry.UpdatedAtUtc = entry.CreatedAtUtc;
            await _collection.InsertOneAsync(entry);
        }

        public async Task<SecurityQuestionEntry?> GetByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return null;

            var filter = Builders<SecurityQuestionEntry>.Filter.Eq(x => x.UserId, userId);
            return await _collection.Find(filter)
                .SortByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();
        }
    }
}
