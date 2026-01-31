using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RecoTrackApi.Models
{
    public class SecurityQuestionEntry
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("question")]
        public string Question { get; set; } = string.Empty;

        [BsonElement("answerHash")]
        public string AnswerHash { get; set; } = string.Empty;

        [BsonElement("createdAtUtc")]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAtUtc")]
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
