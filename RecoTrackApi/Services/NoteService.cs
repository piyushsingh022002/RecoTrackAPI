using RecoTrackApi.DTOs;
using RecoTrack.Application.Models.Notes;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services.Interfaces;
using MongoDB.Driver;
using RecoTrackApi.Repositories;
using System.Linq;

namespace RecoTrackApi.Services
{
    public class NoteService : INoteService
    {
        private readonly INoteRepository _noteRepository;
        private readonly IActivityRepository _activityRepository;
        private readonly IUserRepository _userRepository;

        public NoteService(INoteRepository noteRepository, IActivityRepository activityRepository, IUserRepository userRepository)
        {
            _noteRepository = noteRepository;
            _activityRepository = activityRepository;
            _userRepository = userRepository;
        }

        public async Task<List<Note>> GetNotesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            return await _noteRepository.GetNotesByUserIdAsync(userId);
        }

        // New overload that supports filter + search in single optimized query
        public async Task<List<Note>> GetNotesAsync(string userId, string? filter, string? search)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            // If no filter and no search provided, preserve normal behaviour and use the simple repository call
            if (string.IsNullOrWhiteSpace(filter) && string.IsNullOrWhiteSpace(search))
            {
                return await _noteRepository.GetNotesByUserIdAsync(userId);
            }

            // Validate filter values
            if (!string.IsNullOrWhiteSpace(filter))
            {
                var f = filter.Trim().ToLowerInvariant();
                if (f != "important" && f != "pinned" && f != "favourite" && f != "favorite")
                    throw new ArgumentException("Invalid filter value.", nameof(filter));
            }

            return await _noteRepository.GetNotesByUserIdAsync(userId, filter, search);
        }

        public async Task<List<Note>> GetDeletedNotesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

            return await _noteRepository.GetDeletedNotesByUserIdAsync(userId);
        }

        public async Task<Note?> GetNoteByIdAsync(string id, string userId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID is required.", nameof(id));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            // Authorization: allow owner or shared
            var authorized = await _noteRepository.IsUserAuthorizedForNoteAsync(id, userId);
            if (!authorized)
                return null;

            // If authorized and owner, repository method will return note; if shared, repository GetNoteByIdAsync will return null for shared user, so fetch directly
            var note = await _noteRepository.GetNoteByIdAsync(id, userId);
            if (note != null)
                return note;

            // If not owner, attempt to fetch note by id without owner constraint for shared access
            // Note: avoid returning sensitive fields
            var sharedList = await _noteRepository.GetNotesSharedWithUserAsync(userId);
            var tuple = sharedList.FirstOrDefault(t => t.share.NoteId == id);
            if (tuple.note != null)
            {
                return tuple.note;
            }

            return null;
        }

        public async Task CreateNoteAsync(Note note)
        {
            if (note == null)
                throw new ArgumentNullException(nameof(note));

            if (string.IsNullOrWhiteSpace(note.UserId))
                throw new ArgumentException("User ID is required.", nameof(note.UserId));

            if (string.IsNullOrWhiteSpace(note.Title))
                throw new ArgumentException("Title is required.", nameof(note.Title));

            note.DeletedAt = null;
            await _noteRepository.CreateNoteAsync(note);
        }

        // New helper to create or just record an activity
        public async Task<Guid> CreateOrRecordAsync(Note note, string saveOption, string? eventType, string userId)
        {
            // Ensure note has a NoteRefId (it's init-only in model so already set)
            var noteRef = note.NoteRefId;

            if (string.Equals(saveOption, "SAVE", StringComparison.OrdinalIgnoreCase))
            {
                await CreateNoteAsync(note);
            }
            else
            {
                // JUST_DOWNLOAD: do not persist note, but we still record activity below
            }

            // Record activity via the dedicated activity repository
            if (_activityRepository != null)
            {
                var evt = string.IsNullOrWhiteSpace(eventType) ? "DOWNLOAD" : eventType;
                await _activityRepository.RecordActivityAsync(userId, noteRef, evt);
            }

            return noteRef;
        }

        public async Task<bool> UpdateNoteAsync(string noteId, UpdateNoteDto updateDto, string userId)
        {
            return await UpdateNoteAsync(noteId, updateDto, userId, null);
        }

        public async Task<bool> UpdateNoteAsync(string noteId, UpdateNoteDto updateDto, string userId, IReadOnlyCollection<string>? presentFields)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            var existingNote = await _noteRepository.GetNoteByIdAsync(noteId, userId);
            if (existingNote == null)
                return false;

            bool updateTitle = presentFields == null || presentFields.Contains("title", StringComparer.OrdinalIgnoreCase);
            bool updateContent = presentFields == null || presentFields.Contains("content", StringComparer.OrdinalIgnoreCase);
            bool updateTags = presentFields == null || presentFields.Contains("tags", StringComparer.OrdinalIgnoreCase);
            bool updateLabels = presentFields == null || presentFields.Contains("labels", StringComparer.OrdinalIgnoreCase)
                || presentFields.Contains("labelsToAdd", StringComparer.OrdinalIgnoreCase)
                || presentFields.Contains("labelsToRemove", StringComparer.OrdinalIgnoreCase);
            bool updateMedia = presentFields == null || presentFields.Contains("mediaUrls", StringComparer.OrdinalIgnoreCase);
            bool updateStatus = presentFields == null || presentFields.Contains("status", StringComparer.OrdinalIgnoreCase);
            bool updateIsLocked = presentFields == null || presentFields.Contains("isLocked", StringComparer.OrdinalIgnoreCase);
            bool updateReminder = presentFields == null || presentFields.Contains("reminderAt", StringComparer.OrdinalIgnoreCase);

            if (updateTitle && updateDto.Title != null) existingNote.Title = updateDto.Title.Trim();
            if (updateContent && updateDto.Content != null) existingNote.Content = updateDto.Content.Trim();
            if (updateTags && updateDto.Tags != null) existingNote.Tags = updateDto.Tags;

            if (updateLabels)
            {
                // Labels handling: full replace, or add/remove (tolerant)
                if (updateDto.Labels != null)
                {
                    var normalized = new List<string>();
                    foreach (var l in updateDto.Labels.Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
                    {
                        if (!LabelTypeExtensions.TryNormalize(l, out var n))
                        {
                            // skip invalid
                            continue;
                        }
                        normalized.Add(n);
                    }
                    existingNote.Labels = normalized.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                }
                else
                {
                    if (updateDto.LabelsToAdd != null && updateDto.LabelsToAdd.Count > 0)
                    {
                        var set = new HashSet<string>(existingNote.Labels ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                        foreach (var l in updateDto.LabelsToAdd.Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
                        {
                            if (!LabelTypeExtensions.TryNormalize(l, out var n))
                                continue;
                            set.Add(n);
                        }
                        existingNote.Labels = set.ToList();
                    }

                    if (updateDto.LabelsToRemove != null && updateDto.LabelsToRemove.Count > 0)
                    {
                        var removeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var l in updateDto.LabelsToRemove.Select(x => x?.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
                        {
                            if (!LabelTypeExtensions.TryNormalize(l, out var n))
                                continue;
                            removeSet.Add(n);
                        }
                        existingNote.Labels = (existingNote.Labels ?? new List<string>()).Where(l => !removeSet.Contains(l)).ToList();
                    }
                }

                // Ensure pinnedAt reflects presence of the Pinned label
                var labelsList = existingNote.Labels ?? new List<string>();
                var hasPinned = labelsList.Any(l => string.Equals(l, LabelType.Pinned.ToString(), StringComparison.OrdinalIgnoreCase));
                if (hasPinned)
                {
                    if (!existingNote.PinnedAt.HasValue)
                        existingNote.PinnedAt = DateTime.UtcNow;
                }
                else
                {
                    if (existingNote.PinnedAt.HasValue)
                        existingNote.PinnedAt = null;
                }
            }

            if (updateMedia && updateDto.MediaUrls != null) existingNote.MediaUrls = updateDto.MediaUrls;
            if (updateStatus && updateDto.Status != null) existingNote.Status = updateDto.Status;
            if (updateIsLocked && updateDto.IsLocked.HasValue) existingNote.IsLocked = updateDto.IsLocked.Value;
            if (updateReminder && updateDto.ReminderAt.HasValue) existingNote.ReminderAt = updateDto.ReminderAt;

            existingNote.UpdatedAt = DateTime.UtcNow;
            return await _noteRepository.UpdateNoteAsync(existingNote);
        }

        public async Task<bool> DeleteNoteAsync(string id, string userId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            return await _noteRepository.DeleteNoteAsync(id, userId);
        }

        public async Task<bool> RestoreNoteAsync(string id, string userId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Note ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            return await _noteRepository.RestoreNoteAsync(id, userId);
        }

        public async Task<List<NoteActivityDto>> GetNoteActivityAsync(string userId, DateTime startDate, DateTime endDate)
        {
            return await _activityRepository.GetNoteActivityAsync(userId, startDate, endDate);
        }

        public async Task<List<Note>> GetNotesByDateAsync(string userId, DateTime date)
        {
            return await _noteRepository.GetNotesByDateAsync(userId, date);
        }

        public async Task<int> GetNoteStreakAsync(string userId)
        {
            return await _noteRepository.GetNoteStreakAsync(userId);
        }

        public async Task<List<Note>> GetAllFavouriteNotesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            return await _noteRepository.GetAllFavouriteNotesByUserIdAsync(userId);
        }

        public async Task<List<Note>> GetAllImportantNotesAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            return await _noteRepository.GetAllImportantNotesByUserIdAsync(userId);
        }

        // New: fetch notes shared with current user
        public async Task<List<SharedNoteDto>> GetNotesSharedWithMeAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User ID is required.", nameof(userId));

            var shared = await _noteRepository.GetNotesSharedWithUserAsync(userId);
            var dtos = shared.Select(tuple => new SharedNoteDto
            {
                NoteId = tuple.share.NoteId,
                Title = tuple.note.Title,
                Content = tuple.note.Content,
                OwnerId = tuple.note.UserId,
                Permission = tuple.share.Permission.ToString(),
                SharedAt = tuple.share.CreatedAt
            }).ToList();

            return dtos;
        }

        // New: authorization helper
        public async Task<bool> IsUserAuthorizedForNoteAsync(string noteId, string userId)
        {
            if (string.IsNullOrWhiteSpace(noteId) || string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("Note ID and User ID are required.");

            return await _noteRepository.IsUserAuthorizedForNoteAsync(noteId, userId);
        }

        public async Task<bool> ShareNoteAsync(string noteId, string sharedByUserId, string sharedWithUserId, string permission)
        {
            if (string.IsNullOrWhiteSpace(noteId) || string.IsNullOrWhiteSpace(sharedByUserId) || string.IsNullOrWhiteSpace(sharedWithUserId))
                throw new ArgumentException("NoteId, sharedByUserId and sharedWithUserId are required.");

            // Only owner can share
            var isOwner = await _noteRepository.IsUserAuthorizedForNoteAsync(noteId, sharedByUserId);
            if (!isOwner)
                return false;

            // Create share via repository in a safe manner (repository handles unique constraint)
            return await _noteRepository.CreateNoteShareAsync(noteId, sharedWithUserId, sharedByUserId, permission);
        }

        // New overload: share by emails (comma separated). Service resolves emails -> user ids
        public async Task<bool> ShareNoteByEmailsAsync(string noteId, string sharedByUserId, string sharedWithEmails, string permission)
        {
            if (string.IsNullOrWhiteSpace(noteId) || string.IsNullOrWhiteSpace(sharedByUserId) || string.IsNullOrWhiteSpace(sharedWithEmails))
                throw new ArgumentException("NoteId, sharedByUserId and sharedWithEmails are required.");

            // Only owner can share
            var isOwner = await _noteRepository.IsUserAuthorizedForNoteAsync(noteId, sharedByUserId);
            if (!isOwner)
                return false;

            // Split emails, resolve each email to userId
            var emails = sharedWithEmails.Split(',').Select(e => e.Trim()).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (!emails.Any()) return false;

            var anyShared = false;
            foreach (var email in emails)
            {
                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    // Skip unknown email addresses; could alternatively return failure or send invitation
                    continue;
                }

                var created = await _noteRepository.CreateNoteShareAsync(noteId, user.Id, sharedByUserId, permission);
                anyShared = anyShared || created;
            }

            return anyShared;
        }

        public async Task<bool> UnshareNoteAsync(string noteId, string sharedByUserId, string sharedWithUserId)
        {
            if (string.IsNullOrWhiteSpace(noteId) || string.IsNullOrWhiteSpace(sharedByUserId) || string.IsNullOrWhiteSpace(sharedWithUserId))
                throw new ArgumentException("NoteId, sharedByUserId and sharedWithUserId are required.");

            // Only owner can unshare
            var isOwner = await _noteRepository.IsUserAuthorizedForNoteAsync(noteId, sharedByUserId);
            if (!isOwner)
                return false;

            return await _noteRepository.RemoveNoteShareAsync(noteId, sharedWithUserId);
        }
    }
}
