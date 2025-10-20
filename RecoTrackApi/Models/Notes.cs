using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace YourApp.Models
{
    public class Note
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("userId")]
        public string? UserId { get; set; }

        [BsonElement("title")]
        public string? Title { get; set; }

        [BsonElement("content")]
        public string? Content { get; set; }

        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new();

        [BsonElement("mediaUrls")]
        public List<string> MediaUrls { get; set; } = new();

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
