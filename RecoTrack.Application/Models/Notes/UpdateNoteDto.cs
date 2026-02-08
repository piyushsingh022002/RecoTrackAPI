using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Models.Notes
{
    public class UpdateNoteDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }

        public List<string>? Tags { get; set; }

        // Full replacement of labels
        // Example: ["important", "favorite"]
        public List<string>? Labels { get; set; }

        public List<string>? MediaUrls { get; set; }

        // Active | Archived | Deleted
        public string? Status { get; set; }

        public bool? IsLocked { get; set; }

        public DateTime? ReminderAt { get; set; }
    }
}
