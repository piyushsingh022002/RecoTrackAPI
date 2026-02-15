using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace RecoTrackApi.Repositories
{
 public class NoteActivityModel
 {
 [BsonId]
 [BsonRepresentation(BsonType.ObjectId)]
 public string? Id { get; set; }

 [BsonElement("userId")]
 public string UserId { get; set; } = string.Empty;

 // Stored as string in Mongo
 [BsonElement("noteRefId")]
 [BsonRepresentation(BsonType.String)]
 public Guid NoteRefId { get; set; }

 [BsonElement("eventType")]
 public string EventType { get; set; } = string.Empty;

 [BsonElement("createdAt")]
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
 }
}
