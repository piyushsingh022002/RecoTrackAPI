using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RecoTrack.Application.Models.Notes
{
    public enum NotePermission
    {
         VIEW =0,
         EDIT =1
    }

     public class NoteShare
     {
         [BsonId]
         [BsonRepresentation(BsonType.ObjectId)]
         public string? Id { get; set; }

         [BsonElement("noteId")]
         [BsonRepresentation(BsonType.ObjectId)]
         public string NoteId { get; set; } = string.Empty;

         [BsonElement("sharedWithUserId")]
         [BsonRepresentation(BsonType.ObjectId)]
         public string SharedWithUserId { get; set; } = string.Empty;

         [BsonElement("sharedByUserId")]
         [BsonRepresentation(BsonType.ObjectId)]
         public string SharedByUserId { get; set; } = string.Empty;

         [BsonElement("permission")]
         public NotePermission Permission { get; set; } = NotePermission.VIEW;

         [BsonElement("createdAt")]
         public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

         [BsonElement("updatedAt")]
         public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
     }
}
