using MongoDB.Driver;
using StudentRoutineTrackerApi.Models;
using StudentRoutineTrackerApi.Repositories.Interfaces;
using YourApp.Models;

namespace StudentRoutineTrackerApi.Repositories
{
    public class NoteRepository : INoteRepository
    {
        private readonly IMongoCollection<Note> _notes;

        public NoteRepository(IMongoDatabase database)
        {
            _notes = database.GetCollection<Note>("Notes");
        }

        public async Task<List<Note>> GetNotesByUserIdAsync(string userId) =>
            await _notes.Find(n => n.UserId == userId).SortByDescending(n => n.CreatedAt).ToListAsync();

        public async Task<Note?> GetNoteByIdAsync(string id, string userId) =>
            await _notes.Find(n => n.Id == id && n.UserId == userId).FirstOrDefaultAsync();

        public async Task CreateNoteAsync(Note note) =>
            await _notes.InsertOneAsync(note);

        public async Task<bool> UpdateNoteAsync(Note note)
        {
            note.UpdatedAt = DateTime.UtcNow;
            var result = await _notes.ReplaceOneAsync(n => n.Id == note.Id && n.UserId == note.UserId, note);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteNoteAsync(string id, string userId)
        {
            var result = await _notes.DeleteOneAsync(n => n.Id == id && n.UserId == userId);
            return result.DeletedCount > 0;
        }
    }
}
