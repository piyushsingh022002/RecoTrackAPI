using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RecoTrack.Shared.Settings;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;
using System;
using System.Threading.Tasks;

namespace RecoTrackApi.Repositories
{
    public class PasswordResetRepository : IPasswordResetRepository
    {
        private readonly IMongoCollection<PasswordResetEntry> _collection;

        public PasswordResetRepository(IOptions<MongoDbSettings> settings, IMongoDbService mongoDbService)
        {
            var collectionName = settings.Value.PasswordResetCollectionName;
            _collection = mongoDbService.GetDatabase().GetCollection<PasswordResetEntry>(collectionName);
        }

        public async Task SaveAsync(PasswordResetEntry entry)
        {
            if (entry is null)
                throw new ArgumentNullException(nameof(entry));

            await _collection.InsertOneAsync(entry);
        }

        public async Task DeactivateActiveOtpsAsync(string email)
        {
            var filter = Builders<PasswordResetEntry>.Filter.Eq(x => x.Email, email) &
                         Builders<PasswordResetEntry>.Filter.Eq(x => x.Active, 1);
            var update = Builders<PasswordResetEntry>.Update.Set(x => x.Active, 0);
            await _collection.UpdateManyAsync(filter, update);
        }

        public async Task<PasswordResetEntry?> GetActiveUnexpiredEntryAsync(string email, string otp)
        {
            var filter = Builders<PasswordResetEntry>.Filter.Eq(x => x.Email, email) &
                         Builders<PasswordResetEntry>.Filter.Eq(x => x.Otp, otp) &
                         Builders<PasswordResetEntry>.Filter.Eq(x => x.Active, 1);

            return await _collection.Find(filter)
                .SortByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync();
        }

        public async Task SetSuccessCodeAsync(string email, string otp, string successCode)
        {
            var filter = Builders<PasswordResetEntry>.Filter.Eq(x => x.Email, email) &
                         Builders<PasswordResetEntry>.Filter.Eq(x => x.Otp, otp) &
                         Builders<PasswordResetEntry>.Filter.Eq(x => x.Active, 1);

            var update = Builders<PasswordResetEntry>.Update
                .Set(x => x.SuccessCode, successCode)
                .Set(x => x.SuccessCodeGeneratedAtUtc, DateTime.UtcNow);

            await _collection.UpdateOneAsync(filter, update);
        }

        public async Task<PasswordResetEntry?> GetBySuccessCodeAsync(string email, string successCode)
        {
            var filter = Builders<PasswordResetEntry>.Filter.Eq(x => x.Email, email) &
                         Builders<PasswordResetEntry>.Filter.Eq(x => x.SuccessCode, successCode) &
                         Builders<PasswordResetEntry>.Filter.Eq(x => x.Active, 1);

            return await _collection.Find(filter)
                .SortByDescending(x => x.SuccessCodeGeneratedAtUtc)
                .FirstOrDefaultAsync();
        }

        public async Task<PasswordResetEntry?> GetBySuccessCodeOnlyAsync(string successCode)
        {
            var filter = Builders<PasswordResetEntry>.Filter.Eq(x => x.SuccessCode, successCode) &
                         Builders<PasswordResetEntry>.Filter.Eq(x => x.Active, 1);

            return await _collection.Find(filter)
                .SortByDescending(x => x.SuccessCodeGeneratedAtUtc)
                .FirstOrDefaultAsync();
        }
    }
}
