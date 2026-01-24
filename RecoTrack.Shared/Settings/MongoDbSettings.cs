namespace RecoTrack.Shared.Settings
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string LogsCollectionName { get; set; } = "logs";

        public string EmailAuditCollectionName { get; set; } = "email_audit";
        public string PasswordResetCollectionName { get; set; } = "password_resets";
    }
}
