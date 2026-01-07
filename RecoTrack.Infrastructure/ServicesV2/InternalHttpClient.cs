using RecoTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RecoTrack.Infrastructure.ServicesV2
{
    public class InternalHttpClient : IInternalHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceTokenGenerator _serviceTokenGenerator;

        public InternalHttpClient(HttpClient httpClient, IServiceTokenGenerator serviceTokenGenerator)
        {
            _httpClient = httpClient;
            _serviceTokenGenerator = serviceTokenGenerator;
        }

        public async Task<TResponse> PostAsync<TRequest, TResponse>(
            string url,
            TRequest body,
            string userJwt,
            string? serviceJwt = null,
            CancellationToken cancellationToken = default)
        {
            var resolvedServiceJwt = serviceJwt ?? _serviceTokenGenerator.GenerateToken();

            // Serialize body
            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userJwt);
            _httpClient.DefaultRequestHeaders.Add("X-Service-Authorization", resolvedServiceJwt);
            _httpClient.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());

            // Send POST request
            var response = await _httpClient.PostAsync(url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Deserialize response
            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var deserialized = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, cancellationToken: cancellationToken);
            return deserialized ?? throw new InvalidOperationException("Received an empty response from the HTTP call.");
        }

        public async Task<TResponse> GetAsync<TResponse>(
            string url,
            string userJwt,
            CancellationToken cancellationToken = default)
        {
            string serviceJwt = _serviceTokenGenerator.GenerateToken();

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userJwt);
            _httpClient.DefaultRequestHeaders.Add("X-Service-Authorization", serviceJwt);
            _httpClient.DefaultRequestHeaders.Add("X-Request-ID", Guid.NewGuid().ToString());

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var deserialized = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, cancellationToken: cancellationToken);
            return deserialized ?? throw new InvalidOperationException("Received an empty response from the HTTP call.");
        }
    }
}
