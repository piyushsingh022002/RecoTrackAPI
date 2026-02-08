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
    }
}
