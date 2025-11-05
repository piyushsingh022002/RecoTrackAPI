using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Text;

namespace RecoTrackApi.Configurations
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private const string DashboardUser = "workspace.piyush01@gmail.com";
        private const string DashboardPass = "HangfireProduction12345";

        public HangfireDashboardAuthorizationFilter() { }

        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();
            string authHeader = httpContext.Request.Headers["Authorization"];
            if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Basic ".Length).Trim();
                var credentialString = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var credentials = credentialString.Split(':', 2);
                if (credentials.Length == 2)
                {
                    var username = credentials[0];
                    var password = credentials[1];
                    if (username == DashboardUser && password == DashboardPass)
                    {
                        return true;
                    }
                }
            }
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
            return false;
        }
    }
}
