namespace RecoTrack.Application.Models
{
    public sealed record ObjectStorageUploadResult(
        string UploadUrl,
        string PublicUrl);
}
