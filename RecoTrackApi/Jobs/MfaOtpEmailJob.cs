using Microsoft.Extensions.Logging;
using RecoTrack.Infrastructure.ServicesV2;
using System;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrackApi.Jobs
{
 public class MfaOtpEmailJob
 {
 private readonly ILogger<MfaOtpEmailJob> _logger;
 private readonly EmailService _emailService;

 public MfaOtpEmailJob(ILogger<MfaOtpEmailJob> logger, EmailService emailService)
 {
 _logger = logger;
 _emailService = emailService;
 }

 [Hangfire.AutomaticRetry(Attempts =3)]
 public async Task SendMfaOtpEmailAsync(string toEmail, string username, string otp)
 {
 if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(otp))
 {
 _logger.LogWarning("MfaOtpEmailJob invoked with invalid arguments");
 return;
 }

 try
 {
 var safeName = string.IsNullOrWhiteSpace(username) ? toEmail.Split('@')[0] : username;
 var subject = "Your RecoTrack MFA Code";

 var sb = new StringBuilder();
 sb.Append($"<p style=\"font-size:14px;color:#555;\">Hi {System.Net.WebUtility.HtmlEncode(safeName)},</p>");
 sb.Append("<p style=\"font-size:14px;color:#555;line-height:1.6;\">Use the following one-time code to complete your sign-in to RecoTrack. This code expires in10 minutes.</p>");
 sb.Append($"<div style=\"background:#f7fafc;padding:12px;border-radius:6px;margin:12px0;display:inline-block;\"><strong style=\"font-size:18px;letter-spacing:1px;\">{System.Net.WebUtility.HtmlEncode(otp)}</strong></div>");
 sb.Append("<p style=\"font-size:12px;color:#888;\">If you did not request this sign-in, please secure your account immediately.</p>");

 var html = RecoTrack.Infrastructure.ServicesV2.CommonEmailTemplate.BuildHtml(subject, sb.ToString(), "Return to RecoTrack", "https://recotrackpiyushsingh.vercel.app/");

 await _emailService.SendCustomEmailAsync(toEmail, safeName, subject, html).ConfigureAwait(false);

 _logger.LogInformation("MfaOtpEmailJob: sent MFA OTP to {Email}", toEmail);
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "MfaOtpEmailJob failed for {Email}", toEmail);
 throw;
 }
 }
 }
}
