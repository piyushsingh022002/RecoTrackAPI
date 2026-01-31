using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecoTrack.Application.Interfaces;
using RecoTrack.Infrastructure.Services;
using RecoTrack.Shared.Settings;

namespace RecoTrack.Infrastructure.Extensions
{
    public static class ObjectStorageExtensions
    {
        public static IServiceCollection AddObjectStorageServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ObjectStorageSettings>(configuration.GetSection("ObjectStorage"));

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<ObjectStorageSettings>>().Value;
                var config = new AmazonS3Config();

                if (!string.IsNullOrWhiteSpace(settings.EndpointUrl))
                {
                    config.ServiceURL = settings.EndpointUrl;
                    config.ForcePathStyle = true;
                }
                else if (!string.IsNullOrWhiteSpace(settings.Region))
                {
                    config.RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region);
                    config.ForcePathStyle = settings.UsePathStyle;
                }
                else
                {
                    throw new InvalidOperationException("Object storage configuration requires either a Region or an Endpoint URL.");
                }

                return new AmazonS3Client(config);
            });

            services.AddScoped<IObjectStorageService, S3ObjectStorageService>();
            return services;
        }
    }
}
