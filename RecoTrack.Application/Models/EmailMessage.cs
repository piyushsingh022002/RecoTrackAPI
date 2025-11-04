using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Models
{
    public class EmailMessage
    {
        public string To { get; set; } = default!;
        public string? From { get; set; }
        public string Subject { get; set; } = default!;
        public string Body { get; set; } = default!;
        public bool IsBodyHtml { get; set; } = false;
    }

}
