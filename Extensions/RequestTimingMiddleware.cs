namespace StudentRoutineTrackerApi.Extensions
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
			_logger.LogInformation("Start {path}", context.Request.Path);
			await _next(context);
			sw.Stop();
			_logger.LogInformation("Elapsed {ms}ms for {Path}", sw.ElapsedMilliSeconds, context.Request.Path);

		}
	}

	public static class RequestTimingExtensions
	{
		public static IApplicationBuilder UserRequestTiming(this IApplicationBuilder app) =>
			app.UseMiddleware<RequestTimingMiddleware>();
	}
}

