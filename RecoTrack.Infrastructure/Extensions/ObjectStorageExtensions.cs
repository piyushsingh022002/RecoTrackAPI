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
                var s3Config = new AmazonS3Config();

                // Determine effective values by checking bound settings first, then configuration, then common environment variables.
                string? effectiveEndpoint = !string.IsNullOrWhiteSpace(settings.EndpointUrl)
                    ? settings.EndpointUrl
                    : configuration["ObjectStorage:EndpointUrl"]
                    ?? Environment.GetEnvironmentVariable("OBJECT_STORAGE_ENDPOINT")
                    ?? Environment.GetEnvironmentVariable("S3_ENDPOINT")
                    ?? Environment.GetEnvironmentVariable("S3_ENDPOINT_URL");

                string? effectiveRegion = !string.IsNullOrWhiteSpace(settings.Region)
                    ? settings.Region
                    : configuration["ObjectStorage:Region"]
                    ?? Environment.GetEnvironmentVariable("OBJECT_STORAGE_REGION")
                    ?? Environment.GetEnvironmentVariable("AWS_REGION")
                    ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");

                if (!string.IsNullOrWhiteSpace(effectiveEndpoint))
                {
                    s3Config.ServiceURL = effectiveEndpoint;
                    s3Config.ForcePathStyle = true;
                }
                else if (!string.IsNullOrWhiteSpace(effectiveRegion))
                {
                    try
                    {
                        s3Config.RegionEndpoint = RegionEndpoint.GetBySystemName(effectiveRegion);
                        s3Config.ForcePathStyle = settings.UsePathStyle;
                    }
                    catch
                    {
                        // If region value is invalid, fall back to default client creation below instead of throwing.
                        return new AmazonS3Client();
                    }
                }
                else
                {
                    // No explicit endpoint or region resolved. Create default client which will rely on the SDK's default discovery (env vars, shared config, IMDS).
                    return new AmazonS3Client();
                }

                return new AmazonS3Client(s3Config);
            });

            services.AddScoped<IObjectStorageService, S3ObjectStorageService>();
            return services;
        }
    }
}
