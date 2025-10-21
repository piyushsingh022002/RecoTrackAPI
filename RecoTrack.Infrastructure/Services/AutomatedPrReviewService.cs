using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;

namespace RecoTrack.Infrastructure.Services
{
    public class AutomatedPrReviewService : IAutomatedPrReviewService
    {
        public async Task<PrReviewResult> AnalyzePullRequestAsync(PrMetadata metadata)
        {
            // Simulated logic — later we’ll integrate AI or rule-based analysis
            await Task.Delay(200); // simulate processing time

            var result = new PrReviewResult
            {
                Approved = !metadata.ChangedFiles.Any(f => f.Contains("TODO") || f.Contains("temp")),
                Summary = $"Analyzed PR '{metadata.Title}' by {metadata.Author} on branch '{metadata.BranchName}'."
            };

            if (!result.Approved)
                result.Suggestions.Add("Avoid committing temporary or TODO files.");

            if (metadata.Description.Length < 20)
                result.Suggestions.Add("Provide a more detailed PR description.");

            return result;
        }
    }
}
