using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace RecoTrack.Application.Models
{
    public class EmailAuditRecord
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string UserId { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string ToEmail { get; set; } = default!;
        public DateTime SentAtUtc { get; set; }
    }
}
