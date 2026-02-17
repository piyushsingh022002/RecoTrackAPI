namespace RecoTrack.Infrastructure.ServicesV2
{
 public static class GoogleWelcomeEmailTemplate
 {
 public static string Subject => "Welcome to RecoTrack - Your Smart Note Companion!";

 private static string HtmlTemplate => @"<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width:700px; margin:0 auto; background: linear-gradient(135deg, #667eea0%, #764ba2100%); padding:40px20px; border-radius:10px;"">
 <!-- Header -->
 <div style=""text-align: center; color: white; margin-bottom:30px;"">
 <h1 style=""margin:0; font-size:32px; font-weight:700;"">Welcome to RecoTrack!</h1>
 <p style=""margin:10px000; font-size:16px; opacity:0.95;"">Your ultimate note-taking companion</p>
 </div>

 <!-- Main Content -->
 <div style=""background: white; border-radius:8px; padding:40px; color: #333;"">
 <p style=""font-size:18px; margin:0020px0; color: #667eea; font-weight:600;"">Hi {username}! ??</p>
 <p style=""font-size:14px; line-height:1.6; margin:0025px0; color: #555;"">Thank you for joining RecoTrack! We're thrilled to have you as part of our growing community. Get ready to transform the way you create, manage, and collaborate on notes.</p>
 <p style=""font-size:14px; line-height:1.6; margin:0025px0; color: #555;"">You have been successfully registered with our system using Google Services.</p>
 <p style=""font-size:14px; line-height:1.6; margin:0025px0; color: #555;"">This is HERE your Auto Generated PAssword, Please Do not share it with anyone, You can change it later in your profile settings.</p>
 <p style=""font-size:14px; line-height:1.6; margin:0025px0; color: #555;""><strong>Auto Generated Password:</strong> {userPassword}</p>

 <!-- Features Section -->
 <div style=""background: #f8f9fa; border-left:4px solid #667eea; padding:20px; border-radius:5px; margin:25px0;"">
 <h3 style=""margin:0015px0; color: #667eea; font-size:16px;"">? What You Can Do With RecoTrack:</h3>
 <ul style=""margin:0; padding-left:20px; color: #555; font-size:13px; line-height:1.8;"">
 <li><strong>Smart Note Creation:</strong> Full CRUD operations with rich text editing</li>
 <li><strong>Instant Mail Export:</strong> Share your notes directly via email</li>
 <li><strong>Tags & Organization:</strong> Categorize and find notes effortlessly</li>
 <li><strong>Attachments & URLs:</strong> Embed files and links within your notes</li>
 <li><strong>Multiple Export Formats:</strong> Download as PDF or Word documents instantly</li>
 <li><strong>Streak Tracking:</strong> Maintain your productivity streaks with visual calendar</li>
 <li><strong>Customizable Settings:</strong> Personalize your experience completely</li>
 </ul>
 </div>

 <!-- Premium Feature -->
 <div style=""background: linear-gradient(135deg, #ffd89b0%, #19547b100%); padding:20px; border-radius:5px; margin:25px0; color: white;"">
 <h3 style=""margin:0010px0; font-size:16px; font-weight:700;"">?? Premium Feature - Global Coordination & Collaboration</h3>
 <p style=""margin:0; font-size:13px; line-height:1.6;"">Work seamlessly with your team across the globe. Real-time collaboration, shared workspaces, and unified productivity - all in one platform.</p>
 </div>

 <!-- Get Started -->
 <p style=""font-size:13px; line-height:1.6; margin:25px0; color: #555;"">Start creating your first note today and unlock the full potential of organized digital workspace. Your journey to enhanced productivity begins now!</p>

 <!-- CTA Button -->
 <div style=""text-align: center; margin:30px0;"">
 <a href=""https://recotrackpiyushsingh.vercel.app/login"" style=""background: linear-gradient(135deg, #667eea0%, #764ba2100%); color: white; padding:12px35px; text-decoration: none; border-radius:25px; font-weight:600; font-size:14px; display: inline-block;"">Start Using RecoTrack ?</a>
 </div>

 <!-- Divider -->
 <hr style=""border: none; border-top:1px solid #e0e0e0; margin:30px0;"" />

 <!-- Admin Contact Section -->
 <div style=""background: #f0f4ff; padding:20px; border-radius:5px; margin:20px0;"">
 <h4 style=""margin:0015px0; color: #667eea; font-size:14px; font-weight:700;"">Need Help? Connect With Us</h4>
 <p style=""font-size:12px; margin:8px0; color: #555;""><strong>?? Admin & Creator:</strong> Piyush Singh</p>
 <p style=""font-size:12px; margin:8px0; color: #555;""><strong>?? Work Email:</strong> <a href=""mailto:workspace.piyush01@gmail.com"" style=""color: #667eea; text-decoration: none;"">workspace.piyush01@gmail.com</a></p>
 <p style=""font-size:12px; margin:8px0; color: #555;""><strong>?? Contact:</strong> +918382818030</p>
 
 <div style=""margin-top:15px; display: flex; gap:12px; justify-content: center;"">
 <a href=""https://github.com/piyushsingh022002"" style=""display: inline-block; background: #333; color: white; padding:8px15px; border-radius:4px; text-decoration: none; font-size:11px; font-weight:600;"">GitHub</a>
 <a href=""https://www.linkedin.com/in/piyushsingh02"" style=""display: inline-block; background: #0077b5; color: white; padding:8px15px; border-radius:4px; text-decoration: none; font-size:11px; font-weight:600;"">LinkedIn</a>
 </div>
 </div>

 <!-- Footer -->
 <p style=""font-size:12px; text-align: center; margin:25px000; color: #999; line-height:1.6;"">This email was sent because you recently joined RecoTrack.<br />
 ©2026 RecoTrack. All rights reserved.
 </p>
 </div>
 </div>";

 public static string BuildHtml(string username, string userPassword)
 {
 var safeUser = string.IsNullOrWhiteSpace(username) ? "" : System.Net.WebUtility.HtmlEncode(username);
 var safePass = string.IsNullOrWhiteSpace(userPassword) ? "" : System.Net.WebUtility.HtmlEncode(userPassword);
 return HtmlTemplate.Replace("{username}", safeUser).Replace("{userPassword}", safePass);
 }
 }
}
