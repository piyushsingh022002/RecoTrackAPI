namespace RecoTrack.Infrastructure.ServicesV2
{
 public static class WelcomeEmailTemplate
 {
 public static string Subject => "Welcome to RecoTrack — Your Productivity, Reimagined";

 public static string HtmlTemplate => @"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width:700px; margin:0 auto; background: linear-gradient(135deg, #667eea0%, #764ba2100%); padding:40px20px; border-radius:12px;"">
 <!-- Header -->
 <div style=""text-align:center; color:#ffffff; margin-bottom:28px;"">
 <h1 style=""margin:0; font-size:28px; font-weight:700; letter-spacing:0.2px;"">Welcome to RecoTrack</h1>
 <p style=""margin:8px000; font-size:14px; opacity:0.95;"">Organize. Collaborate. Achieve.</p>
 </div>

 <!-- Card -->
 <div style=""background:#ffffff; border-radius:8px; padding:32px; color:#333333; box-shadow:04px18px rgba(0,0,0,0.08);"">
 <p style=""font-size:16px; margin:0014px0; color:#1a202c; font-weight:600;"">Hello {name},</p>

 <p style=""font-size:14px; line-height:1.6; margin:0018px0; color:#4b5563;"">
 Thank you for joining RecoTrack. We built RecoTrack to help teams and professionals capture ideas, manage work, and collaborate with clarity — all from a single, secure workspace.
 </p>

 <!-- Highlights -->
 <div style=""background:#f7fafc; border-left:4px solid #667eea; padding:18px; border-radius:6px; margin:18px0;"">
 <h3 style=""margin:008px0; color:#2b6cb0; font-size:15px; font-weight:700;"">What RecoTrack helps you do</h3>
 <ul style=""margin:0; padding-left:18px; color:#4b5563; font-size:13px; line-height:1.8;"">
 <li><strong>Capture & organize:</strong> Create rich notes with tags, attachments, and links.</li>
 <li><strong>Collaborate securely:</strong> Share workspaces, assign access, and work in real time.</li>
 <li><strong>Export & share:</strong> Download notes as PDF or Word, or send via email instantly.</li>
 <li><strong>Track progress:</strong> Visual streaks and analytics to keep momentum.</li>
 </ul>
 </div>

 <!-- Premium -->
 <div style=""background: linear-gradient(135deg, #f6d3650%, #fda085100%); padding:16px; border-radius:6px; margin:16px0; color:#0f172a;"">
 <h4 style=""margin:006px0; font-size:14px; font-weight:700;"">Enterprise-grade collaboration</h4>
 <p style=""margin:0; font-size:13px; line-height:1.5;"">Scale with shared workspaces, role-based access, and secure integrations. Ideal for teams that require reliability and compliance.</p>
 </div>

 <p style=""font-size:14px; line-height:1.6; margin:16px022px0; color:#4b5563;"">Ready to get started? Create your first note or invite a teammate — we built RecoTrack to make your workflow clearer and faster.</p>

 <!-- CTA -->
 <div style=""text-align:center; margin:22px0;"">
 <a href=""https://recotrackpiyushsingh.vercel.app/login"" style=""background: linear-gradient(135deg, #667eea0%, #764ba2100%); color:#ffffff; padding:12px30px; text-decoration:none; border-radius:28px; font-weight:700; font-size:14px; display:inline-block;"">Get started with RecoTrack</a>
 </div>

 <hr style=""border:none; border-top:1px solid #e6eef8; margin:26px0;"">

 <!-- Support -->
 <div style=""background:#f0f6ff; padding:16px; border-radius:6px; margin:10px0; color:#334155;"">
 <h5 style=""margin:008px0; color:#2b6cb0; font-size:13px; font-weight:700;"">Need help or have questions?</h5>
 <p style=""margin:0; font-size:13px; color:#374151;""><strong>Support:</strong> <a href=""mailto:workspace.piyush01@gmail.com"" style=""color:#2b6cb0; text-decoration:none;"">workspace.piyush01@gmail.com</a></p>
 <p style=""margin:6px000; font-size:13px; color:#374151;""><strong>Phone:</strong> +918382818030</p>

 <div style=""margin-top:12px; display:flex; gap:10px; justify-content:center;"">
 <a href=""https://github.com/piyushsingh022002"" style=""display:inline-block; background:#111827; color:#ffffff; padding:8px14px; border-radius:6px; text-decoration:none; font-size:12px; font-weight:700;"">GitHub</a>
 <a href=""https://www.linkedin.com/in/piyushsingh02"" style=""display:inline-block; background:#0a66c2; color:#ffffff; padding:8px14px; border-radius:6px; text-decoration:none; font-size:12px; font-weight:700;"">LinkedIn</a>
 </div>
 </div>

 <!-- Footer -->
 <p style=""font-size:12px; text-align:center; margin:18px000; color:#94a3b8; line-height:1.5;"">
 You received this email because you created an account with RecoTrack.<br>
 ©2026 RecoTrack. All rights reserved. If you did not request this, please contact support.
 </p>
 </div>
</div>";
 }
}
