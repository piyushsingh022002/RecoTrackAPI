using Microsoft.Extensions.Configuration;
using RecoTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RecoTrack.Infrastructure.Services
{
    public class GitHubClientService : IGitHubClientService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public GitHubClientService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("RecoTrackAI/1.0"); // must be non-empty & versioned
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            // ✅ GitHub requires "token" not "Bearer"
            var token = _config["GitHub:Token"];
            if (string.IsNullOrWhiteSpace(token))
                throw new Exception("GitHub token not found in configuration.");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("token", token);
        }

        public async Task PostPrCommentAsync(string repoFullName, int issueNumber, string body)
        {
            var url = $"https://api.github.com/repos/{repoFullName}/issues/{issueNumber}/comments";
            var payload = new { body };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"⚠️ Failed to post comment: {response.StatusCode} - {responseContent}");
            }

            Console.WriteLine($"✅ Successfully posted comment to PR #{issueNumber}: {responseContent}");
        }
    }
}
