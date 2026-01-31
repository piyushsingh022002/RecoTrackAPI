using RecoTrack.Application.Models;

namespace RecoTrack.Application.Interfaces
{
    public interface IObjectStorageService
    {
        Task<ObjectStorageUploadResult> GenerateUploadUrlAsync(
            string objectKey,
            string contentType,
            TimeSpan? expiry = null,
            CancellationToken cancellationToken = default);

        string GetPublicUrl(string objectKey);
    }
}
