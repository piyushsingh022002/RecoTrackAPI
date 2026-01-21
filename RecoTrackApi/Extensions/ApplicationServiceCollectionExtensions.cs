using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Services;
using RecoTrack.Infrastructure.Services;
using RecoTrack.Infrastructure.ServicesV2;
using RecoTrackApi.Repositories;
using RecoTrackApi.Services;
using RecoTrackApi.Services.Interfaces;

namespace RecoTrackApi.Extensions
{
    public static class ApplicationServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Application services (business logic)
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IActivityService, ActivityService>();
            services.AddScoped<INoteService, NoteService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<ILogCleanerService, LogCleanerService>();
            services.AddScoped<IJobMetricsRepository, JobMetricsRepository>();
            services.AddHttpClient<IInternalHttpClient, InternalHttpClient>();
            services.AddSingleton<IServiceTokenGenerator, ServiceTokenGenerator>();
            services.AddScoped<IEmailService, EmailService>();

            return services;
        }
    }
}
