using System.Text;

namespace RecoTrack.Infrastructure.ServicesV2
{
 public static class CommonEmailTemplate
 {
 public static string BuildHtml(string title, string contentHtml, string ctaText, string ctaUrl)
 {
 var safeTitle = string.IsNullOrWhiteSpace(title) ? string.Empty : System.Net.WebUtility.HtmlEncode(title);
 var safeCtaText = string.IsNullOrWhiteSpace(ctaText) ? string.Empty : System.Net.WebUtility.HtmlEncode(ctaText);
 var safeCtaUrl = string.IsNullOrWhiteSpace(ctaUrl) ? string.Empty : System.Net.WebUtility.HtmlEncode(ctaUrl);

 var sb = new StringBuilder();
 sb.Append("<div style=\"font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width:700px; margin:0 auto; padding:20px;\">");
 sb.Append("<div style=\"background:#ffffff;border-radius:8px;padding:24px;color:#333;\">");
 sb.Append($"<h2 style=\"margin:0012px0;color:#1a202c;\">{safeTitle}</h2>");
 sb.Append("<div style=\"margin-top:8px;\">");
 sb.Append(contentHtml ?? string.Empty);
 sb.Append("</div>");
 sb.Append("<div style=\"text-align:center;margin-top:18px;\">");
 sb.Append($"<a href=\"{safeCtaUrl}\" style=\"background:#667eea;color:white;padding:10px18px;border-radius:20px;text-decoration:none;display:inline-block;\">{safeCtaText}</a>");
 sb.Append("</div>");
 sb.Append("</div>");
 sb.Append("<p style=\"font-size:12px;color:#888;text-align:center;margin-top:8px;\">©2026 RecoTrack. All rights reserved.</p>");
 sb.Append("</div>");

 return sb.ToString();
 }
 }
}