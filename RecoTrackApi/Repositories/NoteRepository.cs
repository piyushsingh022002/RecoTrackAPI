using MongoDB.Bson;
using MongoDB.Driver;
using StudentRoutineTrackerApi.DTOs;
using StudentRoutineTrackerApi.Models;
using StudentRoutineTrackerApi.Repositories.Interfaces;
using YourApp.Models;

namespace StudentRoutineTrackerApi.Repositories
{
    public class NoteRepository : INoteRepository, IActivityRepository
    {
        private readonly IMongoCollection<Note> _notes;

        public NoteRepository(IMongoDatabase database)
        {
            _notes = database.GetCollection<Note>("Notes");
        }

        public async Task<List<Note>> GetNotesByUserIdAsync(string userId)
        {
            //Todo:: No need to verify the userId here if it's already verified in the controller
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _notes
                .Find(n => n.UserId == userId)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }
        public async Task<Note?> GetNoteByIdAsync(string id, string userId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID cannot be null or empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _notes
                .Find(n => n.Id == id && n.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task CreateNoteAsync(Note note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note), "Note cannot be null.");

            await _notes.InsertOneAsync(note);
        }


        public async Task<bool> UpdateNoteAsync(Note note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            if (string.IsNullOrWhiteSpace(note.Id) || string.IsNullOrWhiteSpace(note.UserId))
                throw new ArgumentException("Note ID and User ID must be provided.");

            note.UpdatedAt = DateTime.UtcNow;

            var result = await _notes.ReplaceOneAsync(
                n => n.Id == note.Id && n.UserId == note.UserId,
                note
            );

            return result.ModifiedCount > 0;
        }


        public async Task<bool> DeleteNoteAsync(string id, string userId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID cannot be null or empty.", nameof(id));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            var result = await _notes.DeleteOneAsync(n => n.Id == id && n.UserId == userId);
            return result.DeletedCount > 0;
        }


        public async Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var filter = Builders<Note>.Filter.And(
                Builders<Note>.Filter.Eq(n => n.UserId, userId),
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

            var allDates = Enumerable.Range(0, (endDate.Date - startDate.Date).Days + 1)
                .Select(offset => startDate.Date.AddDays(offset));
            var dict = activity.ToDictionary(a => a.Date, a => a.NoteCount);
            var filled = allDates.Select(d => new NoteActivityDto { Date = d, NoteCount = dict.ContainsKey(d) ? dict[d] : 0 }).ToList();
            return filled;
        }

        public async Task<List<Note>> GetNotesByDateAsync(string userId, DateTime date)
        {
            var start = date.Date;
            var end = date.Date.AddDays(1).AddTicks(-1);
            return await _notes.Find(n => n.UserId == userId && n.CreatedAt >= start && n.CreatedAt <= end)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetNoteStreakAsync(string userId)
        {
            // Get all note dates for user, sorted descending
            var notes = await _notes.Find(n => n.UserId == userId)
                .SortByDescending(n => n.CreatedAt)
                .Project(n => n.CreatedAt.Date)
                .ToListAsync();
            if (notes.Count == 0) return 0;
            var streak = 0;
            var today = DateTime.UtcNow.Date;
            var expected = today;
            var dates = notes.Distinct().ToList();
            foreach (var d in dates)
            {
                if (d == expected)
                {
                    streak++;
                    expected = expected.AddDays(-1);
                }
                else if (d < expected)
                {
                    break;
                }
            }
            return streak;
        }
    }
}
