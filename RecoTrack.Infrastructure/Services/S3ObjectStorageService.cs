using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;
using RecoTrack.Shared.Settings;

namespace RecoTrack.Infrastructure.Services
{
    public class S3ObjectStorageService : IObjectStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly ObjectStorageSettings _settings;
        private readonly TimeSpan _defaultExpiry;

        public S3ObjectStorageService(IAmazonS3 s3Client, IOptions<ObjectStorageSettings> options)
        {
            _s3Client = s3Client;
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));

            if (string.IsNullOrWhiteSpace(_settings.BucketName))
            {
                throw new InvalidOperationException("Object storage bucket is not configured.");
            }

            var minutes = Math.Max(1, _settings.UploadUrlExpiryMinutes);
            _defaultExpiry = TimeSpan.FromMinutes(minutes);
        }

        public Task<ObjectStorageUploadResult> GenerateUploadUrlAsync(
            string objectKey,
            string contentType,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default)
        {
            var expiresAt = DateTime.UtcNow.Add(expiry ?? _defaultExpiry);
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _settings.BucketName,
                Key = objectKey,
                Verb = HttpVerb.PUT,
                Expires = expiresAt,
                ContentType = contentType
            };

            var uploadUrl = _s3Client.GetPreSignedURL(request);
            var publicUrl = BuildPublicUrl(objectKey);

            var result = new ObjectStorageUploadResult(uploadUrl, publicUrl);
            return Task.FromResult(result);
        }

        public string GetPublicUrl(string objectKey) => BuildPublicUrl(objectKey);

        private string BuildPublicUrl(string objectKey)
        {
            var trimmedKey = objectKey.TrimStart('/');
            if (!string.IsNullOrWhiteSpace(_settings.PublicBaseUrl))
            {
                return $"{_settings.PublicBaseUrl.TrimEnd('/')}/{trimmedKey}";
            }

            if (!string.IsNullOrWhiteSpace(_settings.EndpointUrl))
            {
                return $"{_settings.EndpointUrl.TrimEnd('/')}/{_settings.BucketName}/{trimmedKey}";
            }

            if (!string.IsNullOrWhiteSpace(_settings.Region))
            {
                return $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{trimmedKey}";
            }

            return $"https://{_settings.BucketName}.s3.amazonaws.com/{trimmedKey}";
        }
    }
}
