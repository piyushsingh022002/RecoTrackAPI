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

            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("RecoTrackApp", "1.0"));
        }

        public async Task PostPrCommentAsync(string repo, int prNumber, string comment)
        {
            var apiUrl = _config["GitHub:ApiUrl"];
            var token = _config["GitHub:Token"];

            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("GitHub token not configured.");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var body = new
            {
                body = comment
            };

            var json = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var url = $"{apiUrl}/repos/{repo}/issues/{prNumber}/comments";

            var response = await _httpClient.PostAsync(url, json);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to post comment: {response.StatusCode} - {error}");
            }
        }
    }
}
