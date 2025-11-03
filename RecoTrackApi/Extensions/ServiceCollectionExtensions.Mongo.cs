using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RecoTrack.Application.Models;
using RecoTrackApi.Configurations;
using Serilog;

namespace RecoTrackApi.Extensions
{
    public static class MongoServiceCollectionExtensions
    {
        public static IServiceCollection AddMongo(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind MongoDbSettings from configuration
            services.Configure<MongoDbSettings>(configuration.GetSection(nameof(MongoDbSettings)));

            // Register MongoClient as Singleton (thread-safe)
            services.AddSingleton<IMongoClient>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                Log.Information("Connecting to MongoDB at: {ConnectionString}",
                    settings.ConnectionString.StartsWith("mongodb://localhost") ? "localhost" : "production");
                return new MongoClient(settings.ConnectionString);
            });

            // Register IMongoDatabase as Scoped
            services.AddScoped(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
                var client = sp.GetRequiredService<IMongoClient>();
                return client.GetDatabase(settings.DatabaseName);
            });

            // Optional: Register a specific collection (example for Logs)
            services.AddScoped(sp =>
            {
                var database = sp.GetRequiredService<IMongoDatabase>();
                return database.GetCollection<RecoTrack.Application.Models.LogEntry>("Logs");
            });

            return services;
        }
    }
}
