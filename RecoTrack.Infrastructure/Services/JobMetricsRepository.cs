using MongoDB.Bson;
using MongoDB.Driver;
using RecoTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Infrastructure.Services
{
    public class JobMetricsRepository : IJobMetricsRepository
    {
        private readonly IMongoCollection<BsonDocument> _collection;


        public JobMetricsRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<BsonDocument>("job_metrics");
        }


        public async Task AddMetricAsync(string jobName, DateTime runAtUtc, long deletedCount)
        {
            var doc = new BsonDocument
            {
                ["jobName"] = jobName,
                ["runAtUtc"] = runAtUtc,
                ["deletedCount"] = deletedCount
            };
            await _collection.InsertOneAsync(doc);
        }


        public async Task<JobMetric?> GetLatestAsync(string jobName)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("jobName", jobName);
            var sort = Builders<BsonDocument>.Sort.Descending("runAtUtc");
            var doc = await _collection.Find(filter).Sort(sort).Limit(1).FirstOrDefaultAsync();
            if (doc == null) return null;
            return new JobMetric(doc["jobName"].AsString, doc["runAtUtc"].ToUniversalTime(), doc["deletedCount"].ToInt64());
        }
    }
}
