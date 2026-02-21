using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace RecoTrackApi.Models
{
 public class SupportRequestEntry
 {
 [BsonId]
 [BsonRepresentation(BsonType.ObjectId)]
 public string? Id { get; set; }

 [BsonElement("userId")]
 public string? UserId { get; set; }

 [BsonElement("userEmail")]
 public string? UserEmail { get; set; }

 [BsonElement("category")]
 public string Category { get; set; } = string.Empty;

 [BsonElement("subject")]
 public string Subject { get; set; } = string.Empty;

 [BsonElement("issueType")]
 public string IssueType { get; set; } = string.Empty;

 [BsonElement("message")]
 public string Message { get; set; } = string.Empty;

 [BsonElement("createdAtUtc")]
 public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
 }
}
