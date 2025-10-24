using Microsoft.AspNetCore.Mvc;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;

namespace RecoTrackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutomatedPrReviewController : ControllerBase
    {
        private readonly IAutomatedPrReviewService _reviewService;

        public AutomatedPrReviewController(IAutomatedPrReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] PrReviewRequest request)
        {
            // If caller provided a DiffUrl but not the raw Diff, fetch it
            if (string.IsNullOrWhiteSpace(request.Diff) && !string.IsNullOrWhiteSpace(request.DiffUrl))
            {
                try
                {
                    var http = new HttpClient();
                    request.Diff = await http.GetStringAsync(request.DiffUrl);
                }
                catch
                {
                    // ignore fetch failure — service can still analyze metadata and changed files
                }
            }

            var metadata = new PrMetadata
            {
                Title = request.Title,
                Description = request.Description,
                BranchName = request.Branch,            
                Author = request.Author,
                ChangedFiles = request.ChangedFiles ?? new List<string>(),
                Diff = request.Diff ?? string.Empty,
                Repo = request.Repo ?? string.Empty,
                PrNumber = request.PrNumber,
                CoverageSummary = request.CoverageSummary ?? string.Empty,
                WorkflowRunUrl = request.WorkflowRunUrl ?? string.Empty,
                HtmlReportUrl = request.HtmlReportUrl ?? string.Empty
            }; 

            var result = await _reviewService.AnalyzePullRequestAsync(metadata);
            return Ok(result);
        }
    }
}
