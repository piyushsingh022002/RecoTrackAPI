using RecoTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using RecoTrack.Application.Models;

namespace RecoTrack.Infrastructure.Services
{
    public class LogRepository : ILogRepository
    {
        private readonly IMongoCollection<LogEntry> _collection;

        public LogRepository(IMongoCollection<LogEntry> collection)
        {
            _collection = collection;
        }

        public async Task<long> DeleteAllAsync()
        {
            var result = await _collection.DeleteManyAsync(Builders<LogEntry>.Filter.Empty);
            return result.DeletedCount;
        }

        public async Task<long> DeleteOlderThanAsync(DateTime cutoffUtc)
        {
            var filter = Builders<LogEntry>.Filter.Lt(x => x.Timestamp, cutoffUtc);
            var result = await _collection.DeleteManyAsync(filter);
            return result.DeletedCount;
        }
    }
}
