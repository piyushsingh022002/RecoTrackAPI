using MongoDB.Driver;

namespace StudentRoutineTrackerApi.Services
{
    public interface IMongoDbService
    {
        IMongoDatabase GetDatabase();
    }
}
