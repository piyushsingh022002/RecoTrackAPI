using Amazon;
using Amazon.Runtime;
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
            services.Configure<ObjectStorageSettings>(configuration.GetSection("ObjectStorageSettings"));

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

                //Avoid new AmazonS3Client() without Region or ServiceURL.
                if (string.IsNullOrWhiteSpace(settings.EndpointUrl) && string.IsNullOrWhiteSpace(settings.Region))
                {
                    throw new InvalidOperationException(
                        "ObjectStorage configuration invalid. Either EndpointUrl or Region must be provided.");
                }

                var config = new AmazonS3Config
                {
                    ServiceURL = settings.EndpointUrl,
                    ForcePathStyle = true
                };

                var credentials = new BasicAWSCredentials(
                    settings.AccessKey,
                    settings.SecretKey
                    );

                //var s3Config = new AmazonS3Config();

                //if (!string.IsNullOrWhiteSpace(settings.EndpointUrl))
                //{
                //    s3Config.ServiceURL = settings.EndpointUrl;
                //    s3Config.ForcePathStyle = true;
                //}
                //else
                //{
                //    s3Config.RegionEndpoint = RegionEndpoint.GetBySystemName(settings.Region);
                //    s3Config.ForcePathStyle = settings.UsePathStyle;
                //}

                return new AmazonS3Client(credentials, config);

            });

            services.AddScoped<IObjectStorageService, S3ObjectStorageService>();
            return services;
        }
    }
}
