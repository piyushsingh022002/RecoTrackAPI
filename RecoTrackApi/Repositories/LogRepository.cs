using MongoDB.Driver;
using StudentRoutineTrackerApi.Repositories.Interfaces;
using StudentRoutineTrackerApi.Models;

namespace StudentRoutineTrackerApi.Repositories
{
    public class LogRepository : ILogRepository
    {
        private readonly IMongoCollection<LogEntry> _logs;

        public LogRepository(IMongoClient client, IConfiguration config)
        {
            // Use the same MongoDbSettings you already load in Program.cs
            var mongoSettings = config.GetSection(nameof(Configurations.MongoDbSettings))
                                      .Get<Configurations.MongoDbSettings>();

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
