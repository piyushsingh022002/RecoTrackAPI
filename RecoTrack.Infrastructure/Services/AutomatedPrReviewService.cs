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

        public AutomatedPrReviewService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<PrReviewResult> AnalyzePullRequestAsync(PrMetadata metadata)
        {
            var apiUrl = _configuration["OpenRouter:ApiUrl"];
            var apiKey = _configuration["OpenRouter:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
                throw new InvalidOperationException("OpenRouter API key is missing.");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);

            var prompt = $@"
Summarize this Pull Request in one concise, professional paragraph.
Title: {metadata.Title}
Description: {metadata.Description}
Branch: {metadata.BranchName}
Author: {metadata.Author}
Changed Files: {string.Join(", ", metadata.ChangedFiles)}
";

            var payload = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new { role = "system", content = "You are an AI PR reviewer providing clear, concise summaries." },
                    new { role = "user", content = prompt }
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

            // Basic logic for demo purposes
            var reviewResult = new PrReviewResult
            {
                Approved = !(metadata.ChangedFiles?.Any(f => f.Contains("TODO")) ?? false),
                Suggestions = new List<string>(),
                Summary = summary ?? "No summary generated.",
                ReviewedBy = "AI Reviewer",
                Timestamp = DateTime.UtcNow
            };

            if (metadata.ChangedFiles?.Any(f => f.Contains("TODO")) ?? false)
            {
                reviewResult.Suggestions.Add("Avoid committing temporary or TODO files.");
            }
            if (string.IsNullOrWhiteSpace(metadata.Description) || metadata.Description.Length < 15)
            {
                reviewResult.Suggestions.Add("Provide a more detailed PR description.");
            }

            return reviewResult;
        }
    }
}
