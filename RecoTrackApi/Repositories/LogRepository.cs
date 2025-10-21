using MongoDB.Driver;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Models;

namespace RecoTrackApi.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly IMongoCollection<LogEntry> _logs;

        public LogRepository(IMongoClient client, IConfiguration config)
        {
            var mongoSettings = config.GetSection(nameof(Configurations.MongoDbSettings))
                                      .Get<Configurations.MongoDbSettings>();
            if (mongoSettings == null)
                throw new ArgumentNullException(nameof(mongoSettings), "MongoDbSettings section is missing in configuration.");

            var database = client.GetDatabase(mongoSettings.DatabaseName);
            _logs = database.GetCollection<LogEntry>("Logs");
        }


        public void Insert(LogEntry entry) => _logs.InsertOne(entry);

        public Task InsertAsync(LogEntry entry) => _logs.InsertOneAsync(entry);

        //deleting all the logs from the collection if needed
        public async Task ClearLogsAsync() =>
        await _logs.DeleteManyAsync(Builders<LogEntry>.Filter.Empty);
    }
}
