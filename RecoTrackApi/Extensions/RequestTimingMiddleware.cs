using System.Diagnostics;

namespace RecoTrackApi.Extensions
{
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _logger;

        public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            _logger.LogInformation("Start {Path}", context.Request.Path);
            await _next(context); // forward
            sw.Stop();
            _logger.LogInformation("Elapsed {ms}ms for {Path}", sw.ElapsedMilliseconds, context.Request.Path);
        }
    }

    public static class RequestTimingExtensions
    {
        public static IApplicationBuilder UseRequestTiming(this IApplicationBuilder app) =>
            app.UseMiddleware<RequestTimingMiddleware>();
    }

}
