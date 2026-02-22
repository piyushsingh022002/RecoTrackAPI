using MongoDB.Driver;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;
using System;
using System.Threading.Tasks;

namespace RecoTrackApi.Repositories
{
 public class RefreshTokenRepository : IRefreshTokenRepository
 {
 private readonly IMongoCollection<RefreshTokenEntry> _tokens;

 public RefreshTokenRepository(IMongoDbService mongoDbService)
 {
 var db = mongoDbService.GetDatabase();
 _tokens = db.GetCollection<RefreshTokenEntry>("RefreshTokens");
 }

 public async Task SaveAsync(RefreshTokenEntry entry)
 {
 entry.CreatedAtUtc = DateTime.UtcNow;
 await _tokens.InsertOneAsync(entry);
 }

 public async Task<RefreshTokenEntry?> GetByTokenHashAsync(string tokenHash)
 {
 return await _tokens.Find(t => t.TokenHash == tokenHash).FirstOrDefaultAsync();
 }

 public async Task RevokeAsync(string tokenHash, string? replacedByTokenHash = null)
 {
 var filter = Builders<RefreshTokenEntry>.Filter.Eq(t => t.TokenHash, tokenHash);
 var update = Builders<RefreshTokenEntry>.Update
 .Set(t => t.Revoked, true)
 .Set(t => t.RevokedAtUtc, DateTime.UtcNow)
 .Set(t => t.ReplacedByTokenHash, replacedByTokenHash);

 await _tokens.UpdateOneAsync(filter, update);
 }

 public async Task DeleteExpiredAsync()
 {
 var filter = Builders<RefreshTokenEntry>.Filter.Lt(t => t.ExpiresAtUtc, DateTime.UtcNow);
 await _tokens.DeleteManyAsync(filter);
 }
 }
}
