using RecoTrack.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Interfaces
{
    public interface IAutomatedPrReviewService
    {
        Task<PrReviewResult> AnalyzePullRequestAsync(PrMetadata metadata);
    }
}
