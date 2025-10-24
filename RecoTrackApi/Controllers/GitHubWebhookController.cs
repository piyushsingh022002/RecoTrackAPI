using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public GitHubWebhookController(IAutomatedPrReviewService reviewService, IConfiguration configuration)
        {
            _reviewService = reviewService;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("")]
        public async Task<IActionResult> HandleWebhook()
        {
            var eventType = Request.Headers["X-GitHub-Event"].FirstOrDefault();

            using var reader = new StreamReader(Request.Body);

            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrEmpty(body))
                return BadRequest("Empty body");

            JsonElement payload;

            try
            {
                payload = JsonSerializer.Deserialize<JsonElement>(body);
            }
            catch (JsonException)
            {
                return BadRequest("Invalid JSON");
            }

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
                ChangedFiles = new List<string>(),
                Diff = "",
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
                    var githubToken = _configuration["GitHub:Token"];
                    Console.WriteLine($"Using GitHub token: {githubToken}");
                    http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("token", githubToken);
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("RecoTrackApp/1.0");
                    http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3.diff");

                    var response = await http.GetAsync(diffUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        metadata.Diff = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"GitHub diff fetch failed: {response.StatusCode} - {errorBody}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception fetching diff: {ex.Message}");
                }
            }

            await _reviewService.AnalyzePullRequestAsync(metadata);
            return Ok("PR analyzed and comment posted.");
        }
    }
}
