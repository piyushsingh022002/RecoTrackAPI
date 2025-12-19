using Hangfire;
using RecoTrackApi.Configurations;
using RecoTrackApi.CustomMiddlewares;
using RecoTrackApi.Hubs;
using Serilog;

namespace RecoTrackApi.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds all API middlewares (correlation, headers, exception handling, logging, timing, CORS)
        /// </summary>
        public static IApplicationBuilder UseApiMiddlewares(this IApplicationBuilder app)
        {
            // Order is important
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<HeaderValidationMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();

            app.UseSerilogRequestLogging();
            app.UseRequestTiming();
            app.UseCors("FrontendPolicy");

            return app;
        }

        /// <summary>
        /// Adds authentication and authorization middlewares
        /// </summary>
        public static IApplicationBuilder UseSecurity(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }

        /// <summary>
        /// Adds observability-related middlewares (Hangfire Dashboard, etc.)
        /// </summary>
        public static IApplicationBuilder UseObservability(this IApplicationBuilder app)
        {
            var dashboardOptions = new Hangfire.DashboardOptions
            {
                Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
            };
            app.UseHangfireDashboard("/hangfire", dashboardOptions);

            return app;
        }

        /// <summary>
        /// Maps all endpoints: controllers, SignalR hubs, health checks, and root
        /// </summary>
        public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapControllers();
            endpoints.MapHub<NotificationHub>("/notificationHub");
            endpoints.MapHealthChecks("/health");
            endpoints.MapGet("/", () => Results.Ok("RecoTrack API is running, Credits - PIYUSH SINGH!"));

            return endpoints;
        }
    }
}
