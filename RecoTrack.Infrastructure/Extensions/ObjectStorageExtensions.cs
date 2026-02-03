using Amazon;
using Amazon.Runtime.Internal.Util;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            // Support Render env var names
            services.PostConfigure<ObjectStorageSettings>(settings =>
            {
                if (string.IsNullOrWhiteSpace(settings.BucketName))
                {
                    var bucket = Environment.GetEnvironmentVariable("OBJECT_STORAGE_BUCKET");
                    if (!string.IsNullOrWhiteSpace(bucket))
                        settings.BucketName = bucket;
                }

                if (string.IsNullOrWhiteSpace(settings.Region))
                {
                    var region = Environment.GetEnvironmentVariable("OBJECT_STORAGE_REGION");
                    if (!string.IsNullOrWhiteSpace(region))
                        settings.Region = region;
                }

                if (string.IsNullOrWhiteSpace(settings.EndpointUrl))
                {
                    var endpoint = Environment.GetEnvironmentVariable("OBJECT_STORAGE_ENDPOINT");
                    if (!string.IsNullOrWhiteSpace(endpoint))
                        settings.EndpointUrl = endpoint;
                }

            });
            

            services.AddSingleton<IAmazonS3>(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<ObjectStorageSettings>>().Value;
                var s3Config = new AmazonS3Config();

                var logger = sp.GetRequiredService<ILoggerFactory>()
                   .CreateLogger("ObjectStorage");

                logger.LogInformation(
                    "ObjectStorage config resolved - Bucket={Bucket}, Region={Region}, EndpointSet={EndpointSet}",
                    settings.BucketName,
                    settings.Region,
                    !string.IsNullOrWhiteSpace(settings.EndpointUrl)
                );

                // Use already resolved settings (post-configured)
                if (!string.IsNullOrWhiteSpace(settings.EndpointUrl))
                {
                    s3Config.ServiceURL = settings.EndpointUrl;
                    s3Config.ForcePathStyle = true;
                }
                else if (!string.IsNullOrWhiteSpace(settings.Region))
                {
                    try
                    {
                        s3Config.RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region);
                        s3Config.ForcePathStyle = settings.UsePathStyle;
                    }
                    catch
                    {
                        return new AmazonS3Client();
                    }
                }
                else
                {
                    return new AmazonS3Client();
                }

                return new AmazonS3Client(s3Config);
            });

            services.AddScoped<IObjectStorageService, S3ObjectStorageService>();
            return services;
        }
    }
}
