using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Services;
using RecoTrack.Data.Repositories;
using RecoTrack.Infrastructure.Extensions;
using RecoTrack.Infrastructure.Services;
using RecoTrack.Infrastructure.Services.GoogleAuthService;
using RecoTrack.Infrastructure.ServicesV2;
using RecoTrack.Shared.Settings;
using RecoTrackApi.Repositories;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;

namespace RecoTrackApi.Extensions
{
    public static class InfrastructureServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Mongo
            services.AddSingleton<IMongoDbService, MongoDbService>();
            services.AddScoped<IActivityRepository, ActivityRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<ISecurityQuestionRepository, SecurityQuestionRepository>();
            services.AddScoped<IPasswordResetRepository, PasswordResetRepository>();
            services.AddScoped<INoteRepository, NoteRepository>();
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddSingleton<RecoTrackApi.Repositories.Interfaces.ILogRepository, RecoTrackApi.Repositories.LogRepository>();
            services.AddScoped<RecoTrack.Application.Interfaces.ILogRepository, RecoTrack.Infrastructure.Services.LogRepository>();
            services.AddScoped<IEmailAuditRepository, EmailAuditRepository>();
            services.AddScoped<GoogleAuthService>();

            // Object storage
            services.AddObjectStorageServices(configuration);

            // HttpClients
            services.AddHttpClient<IAutomatedPrReviewService, AutomatedPrReviewService>();
            services.AddHttpClient<IGitHubClientService, GitHubClientService>();

            // Register Brevo settings
            services.Configure<BrevoSettings>(configuration.GetSection("BrevoSettings"));

            // Register Brevo HttpClient
            services.AddHttpClient("brevo", client =>
            {
                client.BaseAddress = new System.Uri("https://api.brevo.com/");
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            });

            // Register concrete EmailService to be used for welcome emails
            services.AddScoped<EmailService>();

            services.AddInfrastructureServices(configuration);

            // Hangfire
            var hangfireOptions = new MongoStorageOptions
            {
                Prefix = "hangfire.",
                MigrationOptions = new MongoMigrationOptions
                {
                    MigrationStrategy = new MigrateMongoMigrationStrategy(),
                    BackupStrategy = new CollectionMongoBackupStrategy()
                }
            };

            services.AddHangfire((provider, config) =>
            {
                var mongoClient = provider.GetRequiredService<IMongoClient>();
                var mongoSettings = configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
                if (mongoSettings == null)
                    throw new InvalidOperationException("MongoDB settings are not configured properly");

                config.UseMongoStorage(
                    mongoClient,
                    mongoSettings.DatabaseName,
                    hangfireOptions);
            });
            services.AddHangfireServer();

            return services;
        }
    }
}
