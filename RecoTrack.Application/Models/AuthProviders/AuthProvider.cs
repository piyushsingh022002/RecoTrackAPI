using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Models.AuthProviders
{
    public class AuthProvider
    {
        [BsonElement("provider")]
        public string Provider { get; set; } = string.Empty; // "google"

        [BsonElement("providerUserId")]
        public string ProviderUserId { get; set; } = string.Empty; // Google "sub"

        [BsonElement("email")]
        public string? Email { get; set; }

        [BsonElement("profilePicture")]
        public string? ProfilePicture { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
