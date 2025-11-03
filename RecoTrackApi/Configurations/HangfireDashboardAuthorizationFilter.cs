using Hangfire.Dashboard;
using Microsoft.Extensions.Configuration;

namespace RecoTrackApi.Configurations
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly IConfiguration _configuration;
        public HangfireDashboardAuthorizationFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public bool Authorize(DashboardContext context)
        {
            // TODO: Implement your authorization logic here
            // For now, allow all users
            return true;
        }
    }
}
