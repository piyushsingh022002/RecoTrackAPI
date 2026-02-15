using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Models.Notes
{
    public class CreateNoteDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }

        // User-defined tags
        public List<string> Tags { get; set; } = new();

        // System labels: "important", "favorite", "pinned"
        public List<string> Labels { get; set; } = new();

        public List<string> MediaUrls { get; set; } = new();

        public DateTime? ReminderAt { get; set; }

        // New fields for frontend workflow
        // SaveOption: "SAVE" | "JUST_DOWNLOAD"
        public string? SaveOption { get; set; }

        // EventType: "DOWNLOAD" | "IMPORT_EMAIL"
        public string? EventType { get; set; }

        // Optional external email to send the imported note to. If not provided, user's email from JWT will be used.
        public string? ExternalEmail { get; set; }
    }
}
