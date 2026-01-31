namespace RecoTrack.Shared.Settings
{
    public class ObjectStorageSettings
    {
        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string? EndpointUrl { get; set; }
        public string? PublicBaseUrl { get; set; }
        public bool UsePathStyle { get; set; }
        public int UploadUrlExpiryMinutes { get; set; } = 15;
    }
}
