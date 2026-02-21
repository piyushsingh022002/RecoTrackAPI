using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RecoTrack.Application.Models.Notes;
using RecoTrack.Shared.Settings;

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
    /// Generic email service that currently exposes welcome, google-welcome, import-note and otp functionality.
    /// Uses Brevo transactional API via injected HttpClient (typed client). Designed to be extensible for
    /// additional email types in the future without changing callers.
    /// </summary>
    public partial class EmailService
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

        // Small helper that encapsulates request creation, sending and common logging
        // Added optional sender override parameters so callers can change 'From' on a per-email basis.
        private async Task SendEmailInternalAsync(string toEmail, string toName, string subject, string htmlContent, object? @params = null, string? senderEmailOverride = null, string? senderNameOverride = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("Attempted to send email with empty recipient");
                return;
            }

            if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _logger.LogError("Brevo API key is not configured");
                throw new InvalidOperationException("Brevo API key not configured");
            }

            var senderEmail = string.IsNullOrWhiteSpace(senderEmailOverride) ? _settings.SenderEmail : senderEmailOverride;
            var senderName = string.IsNullOrWhiteSpace(senderNameOverride) ? _settings.SenderName : senderNameOverride;

            var requestObj = new BrevoEmailRequest
            {
                sender = new BrevoSender { email = senderEmail, name = senderName },
                to = new List<BrevoRecipient> { new BrevoRecipient { email = toEmail, name = toName } },
                subject = subject,
                htmlContent = htmlContent,
                @params = @params
            };

            var json = JsonSerializer.Serialize(requestObj, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            _logger.LogDebug("Brevo request payload for email to {Email}: {Payload}", toEmail, json);

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
                _logger.LogWarning("SendEmailInternalAsync cancelled for {Email}", toEmail);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to call Brevo API for email to {Email}", toEmail);
                throw;
            }

            string respBody = string.Empty;
            try
            {
                respBody = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read Brevo response body for {Email}", toEmail);
            }

            resp.Headers.TryGetValues("sib-request-id", out var sibValues);
            var sibRequestId = sibValues != null ? string.Join(',', sibValues) : string.Empty;

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Brevo API returned non-success status {Status} for {Email}. SibRequestId: {ReqId}. Body: {Body}", resp.StatusCode, toEmail, sibRequestId, respBody);
                throw new InvalidOperationException($"Brevo API error: {resp.StatusCode}");
            }

            _logger.LogInformation("Brevo API responded {Status} for {Email}. SibRequestId: {ReqId}. Body: {Body}", resp.StatusCode, toEmail, sibRequestId, respBody);
        }

        /// <summary>
        /// Public helper for sending custom HTML emails. This wraps internal Brevo request handling so callers
        /// (controllers/jobs) may send arbitrary templates without duplicating Brevo-specific logic.
        /// Added optional fromName/fromEmail so callers (like portfolio controller) can change sender name.
        /// </summary>
        public Task SendCustomEmailAsync(string toEmail, string toName, string subject, string htmlContent, string? fromEmail = null, string? fromName = null, CancellationToken cancellationToken = default)
        {
            return SendEmailInternalAsync(toEmail, toName, subject, htmlContent, null, fromEmail, fromName, cancellationToken);
        }

        /// <summary>
        /// Send registration welcome email to the supplied recipient.
        /// </summary>
        public async Task SendWelcomeEmailAsync(string toEmail, string username, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("SendWelcomeEmailAsync called with empty toEmail");
                return;
            }

            var safeName = string.IsNullOrWhiteSpace(username) ? toEmail.Split('@')[0] : username;
            var subject = WelcomeEmailTemplate.Subject;
            var html = WelcomeEmailTemplate.BuildHtml(safeName);

            await SendEmailInternalAsync(toEmail, safeName, subject, html, new { username = safeName }, null, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Send Google-specific welcome email including auto-generated password.
        /// </summary>
        public async Task SendGoogleWelcomeEmailAsync(string toEmail, string username, string userPassword, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("SendGoogleWelcomeEmailAsync called with empty toEmail");
                return;
            }

            var safeName = string.IsNullOrWhiteSpace(username) ? toEmail.Split('@')[0] : username;
            var subject = GoogleWelcomeEmailTemplate.Subject;
            var html = GoogleWelcomeEmailTemplate.BuildHtml(safeName, userPassword);

            await SendEmailInternalAsync(toEmail, safeName, subject, html, new { username = safeName, userPassword }, null, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Send note import email. If externalEmail is provided it will be used as recipient, otherwise userEmail.
        /// </summary>
        public async Task SendImportNoteEmailAsync(CreateNoteDto noteDto, string userEmail, string? externalEmail = null, string? userName = null, CancellationToken cancellationToken = default)
        {
            if (noteDto == null)
            {
                _logger.LogWarning("SendImportNoteEmailAsync called with null noteDto");
                return;
            }

            var recipient = string.IsNullOrWhiteSpace(externalEmail) ? userEmail : externalEmail!;
            var recipientName = string.IsNullOrWhiteSpace(userName) ? recipient.Split('@')[0] : userName;
            var subject = string.IsNullOrWhiteSpace(noteDto.Title) ? "Your Imported Note from RecoTrack" : $"Imported Note: {noteDto.Title}";

            var sb = new StringBuilder();
            sb.Append($"<p style=\"font-size:14px;color:#555;\">Hi {System.Net.WebUtility.HtmlEncode(recipientName)},</p>");
            sb.Append("<p style=\"font-size:14px;color:#555;line-height:1.6;\">The following note was exported from RecoTrack:</p>");
            sb.Append("<div style=\"background:#f7fafc;padding:12px;border-radius:6px;margin:12px0;\">");

            if (!string.IsNullOrWhiteSpace(noteDto.Title))
                sb.Append($"<h4 style=\"margin:6px0;color:#1a202c;\">{System.Net.WebUtility.HtmlEncode(noteDto.Title)}</h4>");

            if (!string.IsNullOrWhiteSpace(noteDto.Content))
                sb.Append($"<div style=\"font-size:14px;color:#374151;\">{System.Net.WebUtility.HtmlEncode(noteDto.Content).Replace("\n", "<br />")}</div>");

            if (noteDto.Tags != null && noteDto.Tags.Count > 0)
                sb.Append($"<p style=\"font-size:13px;color:#374151;margin-top:8px;\"><strong>Tags:</strong> {System.Net.WebUtility.HtmlEncode(string.Join(", ", noteDto.Tags))}</p>");

            sb.Append("</div>");

            var html = CommonEmailTemplate.BuildHtml("Your Imported Note", sb.ToString(), "View your notes in RecoTrack", "https://recotrackpiyushsingh.vercel.app/notes");

            await SendEmailInternalAsync(recipient, recipientName, subject, html, new { noteTitle = noteDto.Title }, null, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Send OTP email for password reset.
        /// </summary>
        public async Task SendOtpEmailAsync(string toEmail, string username, string otpCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("SendOtpEmailAsync called with empty toEmail");
                return;
            }

            var safeName = string.IsNullOrWhiteSpace(username) ? toEmail.Split('@')[0] : username;
            var subject = "Your RecoTrack OTP Code";

            var sb = new StringBuilder();
            sb.Append($"<p style=\"font-size:14px;color:#555;\">Hi {System.Net.WebUtility.HtmlEncode(safeName)},</p>");
            sb.Append("<p style=\"font-size:14px;color:#555;line-height:1.6;\">Use the following one-time password (OTP) to reset your RecoTrack password. This code will expire in10 minutes.</p>");
            sb.Append($"<div style=\"background:#f7fafc;padding:12px;border-radius:6px;margin:12px0;display:inline-block;\"><strong style=\"font-size:18px;letter-spacing:1px;\">{System.Net.WebUtility.HtmlEncode(otpCode)}</strong></div>");
            sb.Append("<p style=\"font-size:12px;color:#888;\">If you did not request this, please ignore this email or contact support.</p>");

            var bodyHtml = sb.ToString();
            var html = CommonEmailTemplate.BuildHtml("Password Reset OTP", bodyHtml, "Reset Password", "https://recotrackpiyushsingh.vercel.app/forgot-password");

            await SendEmailInternalAsync(toEmail, safeName, subject, html, new { otp = otpCode }, null, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Send notification email after a successful password change.
        /// </summary>
        public async Task SendPasswordChangedEmailAsync(string toEmail, string username, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("SendPasswordChangedEmailAsync called with empty toEmail");
                return;
            }

            var safeName = string.IsNullOrWhiteSpace(username) ? toEmail.Split('@')[0] : username;
            var subject = "Your RecoTrack password was changed";

            var sb = new StringBuilder();
            sb.Append($"<p style=\"font-size:14px;color:#555;\">Hi {System.Net.WebUtility.HtmlEncode(safeName)},</p>");
            sb.Append("<p style=\"font-size:14px;color:#555;line-height:1.6;\">Your RecoTrack account password was updated successfully. If you made this change, no further action is needed.</p>");
            // Use configured sender email as admin contact
            var adminContactEmail = _settings?.SenderEmail ?? "support@recotrack.example";
            sb.Append($"<p style=\"font-size:14px;color:#555;line-height:1.6;\">If you did not make this change, please contact the administrator immediately at <a href=\"mailto:{System.Net.WebUtility.HtmlEncode(adminContactEmail)}\">{System.Net.WebUtility.HtmlEncode(adminContactEmail)}</a>.</p>");
            sb.Append("<p style=\"font-size:12px;color:#888;\">If you need help, reply to this email and we will assist you.</p>");

            var bodyHtml = sb.ToString();
            var html = CommonEmailTemplate.BuildHtml("Password Changed", bodyHtml, "Visit RecoTrack", "https://recotrackpiyushsingh.vercel.app/");

            await SendEmailInternalAsync(toEmail, safeName, subject, html, null, null, null, cancellationToken).ConfigureAwait(false);
        }
    }
}
