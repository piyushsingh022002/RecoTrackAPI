using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RecoTrack.Shared.Settings;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;
using System;

namespace RecoTrackApi.Repositories
{
 public class SupportRequestRepository : ISupportRequestRepository
 {
 private readonly IMongoCollection<SupportRequestEntry> _collection;

 public SupportRequestRepository(IOptions<MongoDbSettings> settings, IMongoDbService mongoDbService)
 {
 var collectionName = "support_requests";
 _collection = mongoDbService.GetDatabase().GetCollection<SupportRequestEntry>(collectionName);
 }

 public async Task SaveAsync(SupportRequestEntry entry)
 {
 if (entry is null)
 throw new ArgumentNullException(nameof(entry));

 entry.CreatedAtUtc = DateTime.UtcNow;
 await _collection.InsertOneAsync(entry);
 }
 }
}
