using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RecoTrackApi.Models
{
    public class PasswordResetEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("otp")]
        public string Otp { get; set; } = string.Empty;

        [BsonElement("active")]
        public int Active { get; set; } = 1;

        [BsonElement("createdAtUtc")]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [BsonElement("expiresAtUtc")]
        public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMinutes(15);

        [BsonElement("successCode")]
        public string SuccessCode { get; set; } = string.Empty;

        [BsonElement("successCodeGeneratedAtUtc")]
        public DateTime? SuccessCodeGeneratedAtUtc { get; set; }
    }

    public class PasswordOtpResult
    {
        public string Message { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }

    public class PasswordOtpVerificationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string SuccessCode { get; set; } = string.Empty;
    }
}
