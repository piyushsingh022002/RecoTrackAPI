using Microsoft.AspNetCore.SignalR;
using RecoTrackApi.Services;

namespace RecoTrackApi.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApi(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddControllers();

            services.AddSignalR();
            services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();

            services.AddCors(options =>
            {
                options.AddPolicy("FrontendPolicy", policy =>
                {
                    policy.WithOrigins()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });

            services.AddSwaggerDocumentation();

            return services;
        }
    }
}
