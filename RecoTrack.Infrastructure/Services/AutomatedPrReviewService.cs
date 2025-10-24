using Microsoft.Extensions.Configuration;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RecoTrack.Infrastructure.Services
{
    public class AutomatedPrReviewService : IAutomatedPrReviewService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IGitHubClientService _githubClient;

        public AutomatedPrReviewService(HttpClient httpClient, IConfiguration configuration, IGitHubClientService githubClient)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _githubClient = githubClient;
        }

        public async Task<PrReviewResult> AnalyzePullRequestAsync(PrMetadata metadata)
        {
            var apiUrl = _configuration["OpenRouter:ApiUrl"];
            var apiKey = _configuration["OpenRouter:ApiKey"];
            var githubToken = _configuration["GitHub:Token"];

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OpenRouter API key is missing.");
            if (string.IsNullOrEmpty(githubToken))
                throw new InvalidOperationException("GitHub token is missing.");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://openrouter.ai");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "RecoTrack PR Review");

            // --- 🧠 Generate review prompt
            //var prompt = new StringBuilder();
            //prompt.AppendLine("You are an expert senior developer performing a code review.");
            //prompt.AppendLine("Analyze this pull request and write a concise professional summary and any actionable suggestions.");
            //prompt.AppendLine();
            //prompt.AppendLine($"**Title:** {metadata.Title}");
            //prompt.AppendLine($"**Description:** {metadata.Description}");
            //prompt.AppendLine($"**Branch:** {metadata.BranchName}");
            //prompt.AppendLine($"**Author:** {metadata.Author}");
            //prompt.AppendLine();
            //prompt.AppendLine("### Diff / Changed Code:");
            //prompt.AppendLine(metadata.Diff?.Length > 2000 ? metadata.Diff[..2000] + "..." : metadata.Diff);

            var prompt = new StringBuilder();
            prompt.AppendLine("You are an expert senior C#/.NET code reviewer. Produce a concise, actionable review in Markdown.");
            prompt.AppendLine("FORMAT: ");
            prompt.AppendLine("1) A one-line bolded summary (first sentence) followed by a short paragraph.");
            prompt.AppendLine("2) A section titled '### Code Suggestions' with up to 3 concrete suggestions. Each suggestion should include a short explanation and a C# code snippet in a ```csharp``` fenced code block.");
            prompt.AppendLine("3) A section titled '### Coverage Summary' that includes the provided coverage analysis text if available.");
            prompt.AppendLine("4) Keep each code snippet minimal and runnable (just the relevant method / snippet).");
            prompt.AppendLine("5) Be concise: prefer bullet points for suggestions, maximum 3 suggestions.");
            prompt.AppendLine();
            prompt.AppendLine($"PR Title: {metadata.Title}");
            prompt.AppendLine($"Author: {metadata.Author}");
            prompt.AppendLine($"Branch: {metadata.BranchName}");
            prompt.AppendLine();
            prompt.AppendLine("PR Description:");
            prompt.AppendLine(metadata.Description);
            prompt.AppendLine();
            prompt.AppendLine("Diff / code context (trimmed):");
            prompt.AppendLine(metadata.Diff?.Length > 4000 ? metadata.Diff.Substring(0, 4000) + "..." : metadata.Diff);
            prompt.AppendLine();
            if (!string.IsNullOrWhiteSpace(metadata.CoverageSummary))
            {
                prompt.AppendLine("Coverage Summary:");
                prompt.AppendLine(metadata.CoverageSummary.Length > 4000 ? metadata.CoverageSummary.Substring(0, 4000) : metadata.CoverageSummary);
                prompt.AppendLine();
            }
            prompt.AppendLine("Now produce the review in Markdown following the FORMAT above.");
            // --- 🤖 Call OpenRouter API

            var payload = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are an AI PR reviewer. Be concise, clear, and objective." },
                    new { role = "user", content = prompt.ToString() }
                }
            };

            var json = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(apiUrl, json);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(result);
            var summary = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // --- 🧾 Review Result
            var reviewResult = new PrReviewResult
            {
                Approved = true,
                Suggestions = new List<string>(),
                Summary = summary ?? "No summary generated.",
                ReviewedBy = "AI Reviewer",
                Timestamp = DateTime.UtcNow
            };

            // --- 🪶 Comment Markdown
            var comment = new StringBuilder();
            comment.AppendLine("## 🤖 Automated Pull Request Review");
            comment.AppendLine();
            comment.AppendLine($"**PR:** #{metadata.PrNumber}");
            comment.AppendLine($"**Title:** {metadata.Title}");
            comment.AppendLine($"**Author:** {metadata.Author}");
            comment.AppendLine();
            comment.AppendLine("### 📝 Summary");
            comment.AppendLine("> " + (summary?.Replace("\n", "\n> ") ?? "No summary generated."));
            comment.AppendLine();
            comment.AppendLine("---");
            comment.AppendLine("_Generated by RecoTrack AI PR Reviewer_");

            try
            {
                await _githubClient.PostPrCommentAsync(metadata.Repo, metadata.PrNumber, comment.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Failed to post PR comment: {ex.Message}");
            }

            return reviewResult;
        }
    }
}
