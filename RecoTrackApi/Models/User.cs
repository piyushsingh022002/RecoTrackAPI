using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RecoTrackApi.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        // Default value set in constructor
        public DateTime RegisteredAt { get; set; }

        public User()
        {
            RegisteredAt = DateTime.UtcNow; // or DateTime.Now depending on your preference
        }
    }
}
