using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Models
{
    public class PrReviewResult
    {
        public bool Approved { get; set; }
        public List<string> Suggestions { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public string ReviewedBy { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }
        public string HtmlReportUrl { get; set; } = string.Empty;

    }
}
