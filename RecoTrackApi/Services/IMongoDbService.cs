using MongoDB.Driver;

namespace RecoTrackApi.Services
{
    public interface IMongoDbService
    {
        IMongoDatabase GetDatabase();
    }
}
