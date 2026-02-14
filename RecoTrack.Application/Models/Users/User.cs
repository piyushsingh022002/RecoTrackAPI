using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RecoTrack.Application.Models.AuthProviders;
using System;
using System.Collections.Generic;

namespace RecoTrack.Application.Models.Users
{
    public enum UserStatus
    {
        Active =0,
        Suspended =1,
        Deleted =2
    }

    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("username")]
        public string Username { get; set; } = string.Empty;

        [BsonElement("fullName")]
        public string FullName { get; set; } = string.Empty;

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [BsonElement("dob")]
        public DateTime Dob { get; set; }

        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [BsonElement("profile")]
        public UserProfile Profile { get; set; } = new();

        [BsonElement("isOAuthUser")]
        public bool IsOAuthUser { get; set; } = false;

        [BsonElement("authProviders")]
        public List<AuthProvider> AuthProviders { get; set; } = new();

        // New status fields
        [BsonElement("status")]
        public UserStatus Status { get; set; } = UserStatus.Active;

        [BsonElement("suspendedAt")]
        public DateTime? SuspendedAt { get; set; }

        [BsonElement("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        [BsonElement("premium")]
        public PremiumFeature Premium { get; set; } = new();


        public User()
        {
            var now = DateTime.UtcNow;
            CreatedAt = now;
            UpdatedAt = now;
            Dob = now;
        }
    }

    public class UserProfile
    {
        [BsonElement("avatarUrl")]
        public string? AvatarUrl { get; set; }
    }

    public class PremiumFeature
    {
        [BsonElement("premiumAt")]
        public DateTime? PremiumAt { get; set; }

        [BsonElement("validTil")]
        public DateTime? ValidTil { get; set; }

        [BsonElement("expiresAt")]
        public DateTime? ExpiresAt { get; set; }

        [BsonElement("isRenewed")]
        public bool IsRenewed { get; set; }

        [BsonElement("cancelled")]
        public bool Cancelled { get; set; }
    }
}
