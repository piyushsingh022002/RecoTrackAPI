using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Models.Notes
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

        // Tags (user-defined)
        [BsonElement("tags")]
        public List<string> Tags { get; set; } = new();

        // System labels (favorite, important, pinned)
        [BsonElement("labels")]
        public List<string> Labels { get; set; } = new();

        [BsonElement("mediaUrls")]
        public List<string> MediaUrls { get; set; } = new();

        // Active | Archived | Deleted
        [BsonElement("status")]
        public string Status { get; set; } = "Active";

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Soft delete support
        [BsonElement("deletedAt")]
        public DateTime? DeletedAt { get; set; }

        // Future-ready fields
        [BsonElement("pinnedAt")]
        public DateTime? PinnedAt { get; set; }

        [BsonElement("reminderAt")]
        public DateTime? ReminderAt { get; set; }

        [BsonElement("isLocked")]
        public bool IsLocked { get; set; } = false;
    }
}
