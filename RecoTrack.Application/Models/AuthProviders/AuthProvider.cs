using MongoDB.Bson.Serialization.Attributes;
using System;

namespace RecoTrack.Application.Models.AuthProviders
{
    public class AuthProvider
    {
        [BsonElement("provider")]
        public string Provider { get; set; } = string.Empty; // e.g. "google", "register"

        [BsonElement("providerUserId")]
        public string ProviderUserId { get; set; } = string.Empty; // provider-specific id
    }
}
