using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Models
{
    public class PrMetadata
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public List<string> ChangedFiles { get; set; } = new();
    }
}
