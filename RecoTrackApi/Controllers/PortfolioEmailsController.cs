using System;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RecoTrack.Infrastructure.ServicesV2;
using RecoTrack.Shared.Settings;
using Microsoft.Extensions.Options;

namespace RecoTrackApi.Controllers
{
 [ApiController]
 [Route("api/[controller]")]
 public class PortfolioEmailsController : ControllerBase
 {
 private readonly EmailService _emailService;
 private readonly BrevoSettings _brevoSettings;
 private readonly ILogger<PortfolioEmailsController> _logger;

 public PortfolioEmailsController(EmailService emailService, IOptions<BrevoSettings> brevoOptions, ILogger<PortfolioEmailsController> logger)
 {
 _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
 _brevoSettings = brevoOptions?.Value ?? throw new ArgumentNullException(nameof(brevoOptions));
 _logger = logger ?? throw new ArgumentNullException(nameof(logger));
 }

 public class PortfolioEmailRequest
 {
 public string Type { get; set; } = string.Empty; // CONNECT | GET_IN_TOUCH | QUERRY
 public JsonElement Data { get; set; }
 }

 // Models for different types
 public class ConnectModel
 {
 public string SenderName { get; set; } = string.Empty;
 public string SenderEmail { get; set; } = string.Empty;
 public string Message { get; set; } = string.Empty;
 }

 public class GetInTouchModel
 {
 public string FullName { get; set; } = string.Empty;
 public string Email { get; set; } = string.Empty;
 public string Subject { get; set; } = string.Empty;
 public string Message { get; set; } = string.Empty;
 }

 public class QuerryModel
 {
 public string FullName { get; set; } = string.Empty;
 public string Designation { get; set; } = string.Empty;
 public string EmailAddress { get; set; } = string.Empty;
 public string PhoneNumber { get; set; } = string.Empty;
 public string Querry { get; set; } = string.Empty;
 }

 [HttpPost]
 [Consumes(MediaTypeNames.Application.Json)]
 [Produces(MediaTypeNames.Application.Json)]
 [Route("send")]
 public async Task<IActionResult> Send([FromBody] PortfolioEmailRequest request)
 {
 if (request == null)
 return BadRequest(new { error = "Request body is required." });

 if (string.IsNullOrWhiteSpace(request.Type))
 return BadRequest(new { error = "Type is required." });

 try
 {
 // Decide template and build email bodies
 string subjectToOwner = string.Empty;
 string ownerBodyHtml = string.Empty;

 string senderEmail = string.Empty;
 string senderName = string.Empty;

 switch (request.Type.ToUpperInvariant())
 {
 case "CONNECT":
 {
 var model = JsonSerializer.Deserialize<ConnectModel>(request.Data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
 if (model == null)
 return BadRequest(new { error = "Invalid data for CONNECT" });

 senderEmail = model.SenderEmail;
 senderName = model.SenderName;
 subjectToOwner = $"Portfolio: Connect request from {model.SenderName}";

 var sb = new StringBuilder();
 sb.Append($"<p><strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(model.SenderName)}</p>");
 sb.Append($"<p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(model.SenderEmail)}</p>");
 sb.Append($"<p><strong>Message:</strong><br/>{System.Net.WebUtility.HtmlEncode(model.Message).Replace("\n", "<br/>")}</p>");

 ownerBodyHtml = sb.ToString();
 break;
 }
 case "GET_IN_TOUCH":
 {
 var model = JsonSerializer.Deserialize<GetInTouchModel>(request.Data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
 if (model == null)
 return BadRequest(new { error = "Invalid data for GET_IN_TOUCH" });

 senderEmail = model.Email;
 senderName = model.FullName;
 subjectToOwner = $"Portfolio: Get in touch - {model.Subject} from {model.FullName}";

 var sb = new StringBuilder();
 sb.Append($"<p><strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(model.FullName)}</p>");
 sb.Append($"<p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(model.Email)}</p>");
 sb.Append($"<p><strong>Subject:</strong> {System.Net.WebUtility.HtmlEncode(model.Subject)}</p>");
 sb.Append($"<p><strong>Message:</strong><br/>{System.Net.WebUtility.HtmlEncode(model.Message).Replace("\n", "<br/>")}</p>");

 ownerBodyHtml = sb.ToString();
 break;
 }
 case "QUERRY":
 {
 var model = JsonSerializer.Deserialize<QuerryModel>(request.Data.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
 if (model == null)
 return BadRequest(new { error = "Invalid data for QUERRY" });

 senderEmail = model.EmailAddress;
 senderName = model.FullName;
 subjectToOwner = $"Portfolio: Querry from {model.FullName} - {model.Designation}";

 var sb = new StringBuilder();
 sb.Append($"<p><strong>Name:</strong> {System.Net.WebUtility.HtmlEncode(model.FullName)}</p>");
 sb.Append($"<p><strong>Designation:</strong> {System.Net.WebUtility.HtmlEncode(model.Designation)}</p>");
 sb.Append($"<p><strong>Email:</strong> {System.Net.WebUtility.HtmlEncode(model.EmailAddress)}</p>");
 sb.Append($"<p><strong>Phone:</strong> {System.Net.WebUtility.HtmlEncode(model.PhoneNumber)}</p>");
 sb.Append($"<p><strong>Querry:</strong><br/>{System.Net.WebUtility.HtmlEncode(model.Querry).Replace("\n", "<br/>")}</p>");

 ownerBodyHtml = sb.ToString();
 break;
 }
 default:
 return BadRequest(new { error = "Unknown Type" });
 }

 // Send email to owner
 var ownerSubject = subjectToOwner;
 var ownerHtml = CommonEmailTemplate.BuildHtml(ownerSubject, ownerBodyHtml, "Reply to sender", $"mailto:{System.Net.WebUtility.HtmlEncode(senderEmail)}");

 var ownerEmail = _brevoSettings.SenderEmail; // workspace owner
 // Use friendly sender name for portfolio emails
 await _emailService.SendCustomEmailAsync(ownerEmail, _brevoSettings.SenderName, ownerSubject, ownerHtml, null, "Piyush's Portfolio");

 // Send acknowledge email to sender if sender email provided
 if (!string.IsNullOrWhiteSpace(senderEmail))
 {
 var ackSubject = "Thanks for contacting me";
 var ackBodySb = new StringBuilder();
 ackBodySb.Append($"<p>Hi {System.Net.WebUtility.HtmlEncode(senderName)},</p>");
 // Minimal acknowledgement message - do not echo payload
 ackBodySb.Append("<p>Thanks for reaching out via my portfolio. I will reach you out Very soon.</p>");
 ackBodySb.Append("<p>Best,<br/>Piyush</p>");

 var ackHtml = CommonEmailTemplate.BuildHtml(ackSubject, ackBodySb.ToString(), "Visit portfolio", "https://piyushsingh.com");

 try
 {
 await _emailService.SendCustomEmailAsync(senderEmail, senderName, ackSubject, ackHtml, null, "Piyush's Portfolio");
 }
 catch (Exception ex)
 {
 // Don't fail the entire request if acknowledgement fails, just log it
 _logger.LogWarning(ex, "Failed to send acknowledgement email to {Email}", senderEmail);
 }
 }

 return Ok(new { status = "sent" });
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "PortfolioEmailsController.Send failed for type {Type}", request.Type);
 return StatusCode(500, new { error = "Failed to send email" });
 }
 }
 }
}
