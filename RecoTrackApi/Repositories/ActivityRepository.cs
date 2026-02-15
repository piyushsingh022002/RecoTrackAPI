using MongoDB.Bson;
using MongoDB.Driver;
using RecoTrackApi.DTOs;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrack.Application.Models.Notes;

namespace RecoTrackApi.Repositories
{
    public class ActivityRepository : IActivityRepository
    {
        private readonly IMongoCollection<NoteActivityModel> _activities;
        private readonly IMongoCollection<Note> _notes;

        public ActivityRepository(IMongoDatabase database)
        {
            _activities = database.GetCollection<NoteActivityModel>("note_activity");
            _notes = database.GetCollection<Note>("Notes");
        }

        public async Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var filter = Builders<Note>.Filter.And(
                Builders<Note>.Filter.Eq(n => n.UserId, userId),
                Builders<Note>.Filter.Eq(n => n.DeletedAt, null),
                Builders<Note>.Filter.Gte(n => n.CreatedAt, startDate.Date),
                Builders<Note>.Filter.Lte(n => n.CreatedAt, endDate.Date.AddDays(1).AddTicks(-1))
            );

            var group = new BsonDocument
            {
                { "_id", new BsonDocument {
                    { "$dateToString", new BsonDocument {
                        { "format", "%Y-%m-%d" },
                        { "date", "$createdAt" }
                    }}
                }},
                { "noteCount", new BsonDocument { { "$sum", 1 } } }
            };

            var pipeline = new[]
            {
                new BsonDocument("$match", filter.ToBsonDocument()),
                new BsonDocument("$group", group),
                new BsonDocument("$sort", new BsonDocument("_id", 1))
            };

            var results = await _notes.Aggregate<BsonDocument>(pipeline).ToListAsync();

            var activity = results.Select(doc => new NoteActivityDto
            {
                Date = DateTime.Parse(doc["_id"].AsString),
                NoteCount = doc["noteCount"].AsInt32
            }).ToList();

            // Fill missing dates with 0
            var allDates = Enumerable.Range(0, (endDate.Date - startDate.Date).Days + 1)
                .Select(offset => startDate.Date.AddDays(offset));
            var dict = activity.ToDictionary(a => a.Date, a => a.NoteCount);
            var filled = allDates.Select(d => new NoteActivityDto { Date = d, NoteCount = dict.ContainsKey(d) ? dict[d] : 0 }).ToList();
            return filled;
        }

        public async Task RecordActivityAsync(string userId, Guid noteRefId, string eventType)
        {
            var activity = new NoteActivityModel
            {
                UserId = userId,
                NoteRefId = noteRefId,
                EventType = eventType,
                CreatedAt = DateTime.UtcNow
            };
            await _activities.InsertOneAsync(activity);
        }
    }
}