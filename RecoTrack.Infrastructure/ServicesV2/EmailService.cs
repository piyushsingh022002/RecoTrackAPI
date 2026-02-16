using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecoTrack.Shared.Settings;
using System.Collections.Generic;

namespace RecoTrack.Infrastructure.ServicesV2
{
    // Minimal Brevo request DTOs
    internal class BrevoEmailRequest
    {
        public BrevoSender sender { get; set; } = new BrevoSender();
        public List<BrevoRecipient> to { get; set; } = new List<BrevoRecipient>();
        public string subject { get; set; } = string.Empty;
        public string htmlContent { get; set; } = string.Empty;
        public object? @params { get; set; }
    }

    internal class BrevoSender
    {
        public string name { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
    }

    internal class BrevoRecipient
    {
        public string email { get; set; } = string.Empty;
        public string? name { get; set; }
    }

    /// <summary>
    /// Generic email service that currently exposes only the welcome-email functionality.
    /// Uses Brevo transactional API via injected HttpClient (typed client). Designed to be extensible for
    /// additional email types in the future without changing callers.
    /// </summary>
    public class EmailService
    {
        private readonly HttpClient _httpClient;
        private readonly BrevoSettings _settings;
        private readonly ILogger<EmailService> _logger;

        private const string BrevoEndpoint = "v3/smtp/email";
        private const string DefaultBrevoBase = "https://api.brevo.com/";

        public EmailService(HttpClient httpClient, IOptions<BrevoSettings> options, ILogger<EmailService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Send registration welcome email to the supplied recipient.
        /// This is async, logs structured events and throws on fatal errors.
        /// </summary>
        public async Task SendWelcomeEmailAsync(string toEmail, string username, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("SendWelcomeEmailAsync called with empty toEmail");
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogError("Brevo API key is not configured");
                throw new InvalidOperationException("Brevo API key not configured");
            }

            var safeName = string.IsNullOrWhiteSpace(username) ? toEmail.Split('@')[0] : username;

            // Use the external template file
            var subject = WelcomeEmailTemplate.Subject;
            var html = WelcomeEmailTemplate.HtmlTemplate.Replace("{name}", System.Net.WebUtility.HtmlEncode(safeName));

            var requestObj = new BrevoEmailRequest
            {
                sender = new BrevoSender { email = _settings.SenderEmail, name = _settings.SenderName },
                to = new List<BrevoRecipient> { new BrevoRecipient { email = toEmail, name = safeName } },
                subject = subject,
                htmlContent = html,
                @params = new { username = safeName }
            };

            var json = JsonSerializer.Serialize(requestObj, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Log request payload (do not log API key)
            _logger.LogDebug("Brevo request payload for welcome email to {Email}: {Payload}", toEmail, json);

            // Ensure absolute URI: combine BaseAddress and endpoint or fall back to default Brevo base
            Uri targetUri;
            if (_httpClient.BaseAddress != null)
            {
                targetUri = new Uri(_httpClient.BaseAddress, BrevoEndpoint);
            }
            else
            {
                targetUri = new Uri(new Uri(DefaultBrevoBase), BrevoEndpoint);
                _logger.LogWarning("HttpClient.BaseAddress was null when sending email; falling back to {Base}", DefaultBrevoBase);
            }

            using var httpReq = new HttpRequestMessage(HttpMethod.Post, targetUri);
            httpReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpReq.Headers.Add("api-key", _settings.ApiKey);
            httpReq.Content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage resp;
            try
            {
                resp = await _httpClient.SendAsync(httpReq, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("SendWelcomeEmailAsync cancelled for {Email}", toEmail);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Brevo API for welcome email to {Email}", toEmail);
                throw;
            }

            // Read and log response body even on success to help debug delivery issues
            string respBody = string.Empty;
            try
            {
                respBody = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read Brevo response body for {Email}", toEmail);
            }

            // Extract Sib request id header to correlate on Brevo dashboard
            resp.Headers.TryGetValues("sib-request-id", out var sibValues);
            var sibRequestId = sibValues != null ? string.Join(',', sibValues) : string.Empty;

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Brevo API returned non-success status {Status} for {Email}. SibRequestId: {ReqId}. Body: {Body}", resp.StatusCode, toEmail, sibRequestId, respBody);
                throw new InvalidOperationException($"Brevo API error: {resp.StatusCode}");
            }

            _logger.LogInformation("Brevo API responded {Status} for {Email}. SibRequestId: {ReqId}. Body: {Body}", resp.StatusCode, toEmail, sibRequestId, respBody);

            // Note:201 Created indicates Brevo accepted the request. If mail is not delivered:
            // - Check Brevo dashboard using sibRequestId
            // - Verify SenderEmail is validated in Brevo account
            // - Check suppression/blacklist for recipient
            // - Check spam folder or DMARC/SPF/DKIM configuration for sender domain
        }
    }
}
