using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace RecoTrackApi.Models
{
 public class RefreshTokenEntry
 {
 [BsonId]
 [BsonRepresentation(BsonType.ObjectId)]
 public string Id { get; set; } = string.Empty;

 [BsonElement("userId")]
 public string UserId { get; set; } = string.Empty;

 [BsonElement("tokenHash")]
 public string TokenHash { get; set; } = string.Empty;

 [BsonElement("createdAtUtc")]
 public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

 [BsonElement("expiresAtUtc")]
 public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(7);

 [BsonElement("revoked")]
 public bool Revoked { get; set; } = false;

 [BsonElement("revokedAtUtc")]
 public DateTime? RevokedAtUtc { get; set; }

 [BsonElement("replacedByTokenHash")]
 public string? ReplacedByTokenHash { get; set; }
 }
}