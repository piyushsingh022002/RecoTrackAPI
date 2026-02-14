using RecoTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            string? userJwt = null,
            string? serviceJwt = null,
            CancellationToken cancellationToken = default)
        {
            //var resolvedServiceJwt = serviceJwt ?? _serviceTokenGenerator.GenerateToken();

            // Serialize body
            var json = JsonSerializer.Serialize(body);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Build the request message and set headers per-request (do not mutate shared DefaultRequestHeaders)
            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            // If caller provided a user JWT, use that in Authorization (user context). Otherwise use the service token

                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceJwt);


            requestMessage.Headers.Accept.Clear();
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            // Keep headers minimal: only Authorization and a request id
            requestMessage.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());

            // Send POST request
            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Deserialize response
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return default!;
            }

            var deserialized = JsonSerializer.Deserialize<TResponse>(responseContent);
            return deserialized ?? default!;
        }

        public async Task<TResponse> GetAsync<TResponse>(
            string url,
            string userJwt,
            CancellationToken cancellationToken = default)
        {
            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userJwt);
            requestMessage.Headers.Add("X-Request-ID", Guid.NewGuid().ToString());

            var response = await _httpClient.SendAsync(requestMessage, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var deserialized = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, cancellationToken: cancellationToken);
            return deserialized ?? throw new InvalidOperationException("Received an empty response from the HTTP call.");
        }
    }
}
