using MongoDB.Bson;
using MongoDB.Driver;
using RecoTrack.Application.Models.Notes;
using RecoTrackApi.DTOs;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories.Interfaces;

namespace RecoTrackApi.Repositories
{
    public class NoteRepository : INoteRepository
    {
        private readonly IMongoCollection<Note> _notes;

        public NoteRepository(IMongoDatabase database)
        {
            _notes = database.GetCollection<Note>("Notes");
            EnsureIndexes();
        }

        private void EnsureIndexes()
        {
            var activeIndex = new CreateIndexModel<Note>(
                Builders<Note>.IndexKeys
                    .Ascending(n => n.UserId)
                    .Ascending(n => n.DeletedAt)
                    .Descending(n => n.CreatedAt));

            var deletedIndex = new CreateIndexModel<Note>(
                Builders<Note>.IndexKeys
                    .Ascending(n => n.UserId)
                    .Descending(n => n.DeletedAt)
                    .Descending(n => n.CreatedAt));

            _notes.Indexes.CreateMany(new[] { activeIndex, deletedIndex });
        }

        public async Task<List<Note>> GetNotesByUserIdAsync(string userId)
        {
            //do not check userId here, repository should trust service
            var filter = Builders<Note>.Filter.And(
                Builders<Note>.Filter.Eq(n => n.UserId, userId),
                Builders<Note>.Filter.Eq(n => n.DeletedAt, null));

            return await _notes
                .Find(filter)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Note>> GetDeletedNotesByUserIdAsync(string userId)
        {
            var filter = Builders<Note>.Filter.And(
             Builders<Note>.Filter.Eq(n => n.UserId, userId),
             Builders<Note>.Filter.Ne(n => n.DeletedAt, null));

            return await _notes
                .Find(filter)
                .SortByDescending(n => n.DeletedAt)
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Note?> GetNoteByIdAsync(string id, string userId)
        {
            var filter = Builders<Note>.Filter.And(
        Builders<Note>.Filter.Eq(n => n.Id, id),
        Builders<Note>.Filter.Eq(n => n.UserId, userId),
        Builders<Note>.Filter.Eq(n => n.DeletedAt, null)
    );

            return await _notes
                .Find(filter)
                .FirstOrDefaultAsync();
        }

        public async Task CreateNoteAsync(Note note)
        {
            await _notes.InsertOneAsync(note);
        }

        public async Task<bool> UpdateNoteAsync(Note note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            var result = await _notes.ReplaceOneAsync(n => n.Id == note.Id && n.UserId == note.UserId && n.DeletedAt == null,note);

            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteNoteAsync(string id, string userId)
        {
            var update = Builders<Note>.Update
                .Set(n => n.DeletedAt, DateTime.UtcNow)
                .Set(n => n.UpdatedAt, DateTime.UtcNow);

            var result = await _notes.UpdateOneAsync(
                n => n.Id == id && n.UserId == userId && n.DeletedAt == null,
                update
            );

            return result.ModifiedCount > 0;
        }

        public async Task<bool> RestoreNoteAsync(string id, string userId)
        {
            var update = Builders<Note>.Update
                .Set(n => n.DeletedAt, null)
                .Set(n => n.UpdatedAt, DateTime.UtcNow);

            var result = await _notes.UpdateOneAsync(
                n => n.Id == id && n.UserId == userId && n.DeletedAt != null,
                update
            );

            return result.ModifiedCount > 0;
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
            return await _notes.Find(n => n.UserId == userId && n.DeletedAt == null && n.CreatedAt >= start && n.CreatedAt <= end)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetNoteStreakAsync(string userId)
        {
            var notes = await _notes.Find(n => n.UserId == userId && n.DeletedAt == null)
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

        public async Task<List<Note>> GetAllFavouriteNotesByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            var filter = Builders<Note>.Filter.And(
                Builders<Note>.Filter.Eq(n => n.UserId, userId),
                Builders<Note>.Filter.Eq(n => n.DeletedAt, null),
                Builders<Note>.Filter.AnyEq(n => n.Labels, "Favourite")
            );

            return await _notes
                .Find(filter)
                .SortByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Note>> GetAllImportantNotesByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            var filter = Builders<Note>.Filter.And(
                Builders<Note>.Filter.Eq(n => n.UserId, userId),
                Builders<Note>.Filter.Eq(n => n.DeletedAt, null),
                Builders<Note>.Filter.AnyEq(n => n.Labels, "Important")
            );

            return await _notes
                .Find(filter)
                .SortByDescending(n => n.UpdatedAt)
                .ToListAsync();
        }
    }
}
