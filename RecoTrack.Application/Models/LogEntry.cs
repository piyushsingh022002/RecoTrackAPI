// Shared/Models/LogEntry.cs
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RecoTrack.Application.Models
{
    public class LogEntry
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Exception { get; set; } = string.Empty;
        public string SourceContext { get; set; } = string.Empty;
        public Dictionary<string, string> Properties { get; set; } = new();
    }

}
