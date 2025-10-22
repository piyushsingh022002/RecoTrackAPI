using Microsoft.AspNetCore.Mvc;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;
using System.Text.Json;

namespace RecoTrackApi.Controllers
{
    [ApiController]
    [Route("api/github/webhook")]
    public class GitHubWebhookController : ControllerBase
    {
        private readonly IAutomatedPrReviewService _reviewService;

        public GitHubWebhookController(IAutomatedPrReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook([FromBody] JsonElement payload)
        {
            var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();
            if (eventType != "pull_request")
                return Ok("Ignored: Not a pull_request event");

            var action = payload.GetProperty("action").GetString();
            if (action != "opened" && action != "synchronize")
                return Ok($"Ignored: PR action {action}");

            var pr = payload.GetProperty("pull_request");

            var metadata = new PrMetadata
            {
                Title = pr.GetProperty("title").GetString() ?? string.Empty,
                Description = pr.GetProperty("body").GetString() ?? string.Empty,
                BranchName = pr.GetProperty("head").GetProperty("ref").GetString() ?? string.Empty,
                Author = pr.GetProperty("user").GetProperty("login").GetString() ?? string.Empty,
                ChangedFiles = new List<string>(), // optionally fetched later
                Diff = "", // optionally fetched later
                Repo = payload.GetProperty("repository").GetProperty("full_name").GetString() ?? string.Empty,
                PrNumber = pr.GetProperty("number").GetInt32()
            };

            // Optionally fetch diff from PR
            if (pr.TryGetProperty("diff_url", out var diffUrlProp))
            {
                var diffUrl = diffUrlProp.GetString();
                try
                {
                    var http = new HttpClient();
                    metadata.Diff = await http.GetStringAsync(diffUrl);
                }
                catch
                {
                    // Ignore fetch error
                }
            }

            await _reviewService.AnalyzePullRequestAsync(metadata);
            return Ok("PR analyzed and comment posted.");
        }
    }
}
