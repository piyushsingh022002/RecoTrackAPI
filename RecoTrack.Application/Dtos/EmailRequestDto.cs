using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Dtos
{
    public record EmailRequestDto
    {
        // Validate minimal fields here; expand per your frontend payload
        public string UserId { get; init; } = default!;
        public string UserName { get; init; } = default!;
        public string ToEmail { get; init; } = default!;
        public string Subject { get; init; } = "Message from app";
        public string BodyMarkdown { get; init; } = string.Empty; // the editor content
                                                                  // Additional structured metadata from frontend (optional)
        public IDictionary<string, string>? Metadata { get; init; }
    }
}
