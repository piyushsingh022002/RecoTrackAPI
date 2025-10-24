using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Models
{
    public class PrMetadata
    {

        public int PrNumber { get; set; }      
        public string Repo { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BranchName { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public List<string> ChangedFiles { get; set; } = new();

        public string Diff { get; set; } = string.Empty;

        public string CoverageSummary { get; set; } = string.Empty;
        public string WorkflowRunUrl { get; set; } = string.Empty;

    }
}
