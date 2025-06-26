using Microsoft.Extensions.Options;
using MongoDB.Driver;
using StudentRoutineTrackerApi.Configurations;

namespace StudentRoutineTrackerApi.Services
{
    public class MongoDbService : IMongoDbService
    {
        private readonly IMongoDatabase _database;

        public MongoDbService(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }

        public IMongoDatabase GetDatabase() => _database;
    }
}
