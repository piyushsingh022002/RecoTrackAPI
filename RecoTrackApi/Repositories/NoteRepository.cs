using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using RecoTrack.Application.Models.Notes;
using RecoTrackApi.DTOs;
using RecoTrackApi.Models;
using RecoTrackApi.Repositories.Interfaces;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace RecoTrackApi.Repositories
{
    public class NoteRepository : INoteRepository
    {
        private readonly IMongoCollection<Note> _notes;
        private readonly IMongoCollection<NoteShare> _noteShares;

        public NoteRepository(IMongoDatabase database)
        {
            _notes = database.GetCollection<Note>("Notes");
            _noteShares = database.GetCollection<NoteShare>("NoteShares");
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

            // Indexes for NoteShare to optimize lookups by note and recipient
            var shareIndex1 = new CreateIndexModel<NoteShare>(Builders<NoteShare>.IndexKeys.Ascending(s => s.NoteId));
            var shareIndex2 = new CreateIndexModel<NoteShare>(Builders<NoteShare>.IndexKeys.Ascending(s => s.SharedWithUserId));
            var uniqueShare = new CreateIndexModel<NoteShare>(Builders<NoteShare>.IndexKeys
                .Ascending(s => s.NoteId)
                .Ascending(s => s.SharedWithUserId), new CreateIndexOptions { Unique = true });

            _noteShares.Indexes.CreateMany(new[] { shareIndex1, shareIndex2, uniqueShare });
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

        // Overload supporting optional filter and search in single optimized query
        public async Task<List<Note>> GetNotesByUserIdAsync(string userId, string? filter, string? search)
        {
            // Build dynamic filter
            var filters = new List<FilterDefinition<Note>>();
            filters.Add(Builders<Note>.Filter.Eq(n => n.UserId, userId));
            filters.Add(Builders<Note>.Filter.Eq(n => n.DeletedAt, null));

            if (!string.IsNullOrWhiteSpace(filter))
            {
                // Normalize incoming filter values
                var f = filter.Trim().ToLowerInvariant();
                if (f == "important")
                {
                    // Labels contain "Important" (note repository stores labels as title-case)
                    filters.Add(Builders<Note>.Filter.AnyEq(n => n.Labels, "Important"));
                }
                else if (f == "pinned")
                {
                    filters.Add(Builders<Note>.Filter.Ne(n => n.PinnedAt, null));
                }
                else if (f == "favourite" || f == "favorite")
                {
                    filters.Add(Builders<Note>.Filter.AnyEq(n => n.Labels, "Favourite"));
                }
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                // Case-insensitive partial match on Title or Content
                // Use regex with options for case-insensitive
                var regex = new BsonRegularExpression($"{RegexEscape(s)}", "i");
                var titleFilter = Builders<Note>.Filter.Regex(n => n.Title, regex);
                var contentFilter = Builders<Note>.Filter.Regex(n => n.Content, regex);
                filters.Add(Builders<Note>.Filter.Or(titleFilter, contentFilter));
            }

            var finalFilter = Builders<Note>.Filter.And(filters);

            return await _notes
                .Find(finalFilter)
                .SortByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        private static string RegexEscape(string input)
        {
            // Minimal escape for regex special chars
            return Regex.Escape(input);
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
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            // Ensure Id is set so caller can read it after insert (string-representation of ObjectId)
            if (string.IsNullOrWhiteSpace(note.Id))
            {
                note.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            }

            // Ensure timestamps
            if (note.CreatedAt == default) note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;
            note.DeletedAt ??= null;

            await _notes.InsertOneAsync(note);
        }

        public async Task<bool> UpdateNoteAsync(Note note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            var result = await _notes.ReplaceOneAsync(n => n.Id == note.Id && n.UserId == note.UserId && n.DeletedAt == null,note);

            return result.ModifiedCount >0;
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

            return result.ModifiedCount >0;
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

            return result.ModifiedCount >0;
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
 if (notes.Count ==0) return 0;
 var streak =0;
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

        // New: get notes shared with a user
        public async Task<List<(Note note, NoteShare share)>> GetNotesSharedWithUserAsync(string userId)
        {
            // Single-join like query using aggregation to join NoteShares with Notes
            var match = Builders<NoteShare>.Filter.Eq(s => s.SharedWithUserId, userId);
            var lookup = new BsonDocument
            {
                { "$lookup", new BsonDocument {
                    { "from", "Notes" },
                    { "localField", "noteId" },
                    { "foreignField", "_id" },
                    { "as", "note" }
                }}
            };

            var pipeline = new[]
            {
                new BsonDocument("$match", match.ToBsonDocument()),
                lookup,
                new BsonDocument("$unwind", new BsonDocument("path","$note")),
                new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$eq", new BsonArray {"$note.deletedAt", BsonNull.Value})))
            };

            var aggregate = _noteShares.Aggregate<BsonDocument>(pipeline);
            var results = await aggregate.ToListAsync();

            var output = new List<(Note, NoteShare)>();
            foreach (var doc in results)
            {
                var bdoc = doc.AsBsonDocument;

                // Build NoteShare from aggregation result (avoid serializer issues due to extra 'note' field)
                var share = new NoteShare
                {
                    Id = bdoc.GetValue("_id").ToString(),
                    NoteId = bdoc.GetValue("noteId").AsString,
                    SharedWithUserId = bdoc.GetValue("sharedWithUserId").AsString,
                    SharedByUserId = bdoc.GetValue("sharedByUserId").AsString,
                    Permission = (NotePermission)bdoc.GetValue("permission").AsInt32,
                    CreatedAt = bdoc.GetValue("createdAt").ToUniversalTime(),
                    UpdatedAt = bdoc.GetValue("updatedAt").ToUniversalTime()
                };

                var noteDoc = bdoc["note"].AsBsonDocument;
                var note = BsonSerializer.Deserialize<Note>(noteDoc);
                output.Add((note, share));
            }

            return output;
        }

        // New: authorization check for a note - owner or shared with
        public async Task<bool> IsUserAuthorizedForNoteAsync(string noteId, string userId)
        {
            if (string.IsNullOrWhiteSpace(noteId) || string.IsNullOrWhiteSpace(userId))
                return false;

            // Check ownership first
            var ownerFilter = Builders<Note>.Filter.And(
                Builders<Note>.Filter.Eq(n => n.Id, noteId),
                Builders<Note>.Filter.Eq(n => n.UserId, userId),
                Builders<Note>.Filter.Eq(n => n.DeletedAt, null)
            );

            var ownerExists = await _notes.Find(ownerFilter).AnyAsync();
            if (ownerExists) return true;

            // Check note share
            var shareFilter = Builders<NoteShare>.Filter.And(
                Builders<NoteShare>.Filter.Eq(s => s.NoteId, noteId),
                Builders<NoteShare>.Filter.Eq(s => s.SharedWithUserId, userId)
            );

            return await _noteShares.Find(shareFilter).AnyAsync();
        }

        public async Task<bool> CreateNoteShareAsync(string noteId, string sharedWithUserId, string sharedByUserId, string permission)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(noteId) || string.IsNullOrWhiteSpace(sharedWithUserId) || string.IsNullOrWhiteSpace(sharedByUserId))
                throw new ArgumentException("noteId, sharedWithUserId and sharedByUserId are required.");

            // Ensure note exists and not deleted
            var note = await _notes.Find(n => n.Id == noteId && n.DeletedAt == null).FirstOrDefaultAsync();
            if (note == null) return false;

            // Create share document. Use upsert logic to avoid duplicates or conflicts.
            var share = new NoteShare
            {
                NoteId = noteId,
                SharedWithUserId = sharedWithUserId,
                SharedByUserId = sharedByUserId,
                Permission = permission.Equals("EDIT", StringComparison.OrdinalIgnoreCase) ? NotePermission.EDIT : NotePermission.VIEW,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                var filter = Builders<NoteShare>.Filter.And(
                    Builders<NoteShare>.Filter.Eq(s => s.NoteId, noteId),
                    Builders<NoteShare>.Filter.Eq(s => s.SharedWithUserId, sharedWithUserId)
                );

                var update = Builders<NoteShare>.Update
                    .SetOnInsert(s => s.NoteId, share.NoteId)
                    .SetOnInsert(s => s.SharedWithUserId, share.SharedWithUserId)
                    .SetOnInsert(s => s.SharedByUserId, share.SharedByUserId)
                    .Set(s => s.Permission, share.Permission)
                    .Set(s => s.UpdatedAt, share.UpdatedAt)
                    .SetOnInsert(s => s.CreatedAt, share.CreatedAt);

                var options = new UpdateOptions { IsUpsert = true };
                var result = await _noteShares.UpdateOneAsync(filter, update, options);

                // If upserted or modified, return true
                return result.IsAcknowledged && (result.UpsertedId != null || result.ModifiedCount >0);
            }
            catch (MongoWriteException mwx) when (mwx.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                // Duplicate due to race; consider it success (already exists)
                return true;
            }
        }

        public async Task<bool> RemoveNoteShareAsync(string noteId, string sharedWithUserId)
        {
            if (string.IsNullOrWhiteSpace(noteId) || string.IsNullOrWhiteSpace(sharedWithUserId))
                throw new ArgumentException("noteId and sharedWithUserId are required.");

            var result = await _noteShares.DeleteOneAsync(s => s.NoteId == noteId && s.SharedWithUserId == sharedWithUserId);
            return result.DeletedCount >0;
        }
    }
}
