using Serilog.Context;

namespace RecoTrackApi.Extensions
{
    public class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            const string correlationHeader = "X-Correlation-Id";

            //check if there is correlation id is already present in the Request Header or not
            if(!context.Request.Headers.TryGetValue(correlationHeader, out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            //Adding Optional Configurable Prefix(Advance)
            var prefix = context.RequestServices.GetService<IConfiguration>()
                ?.GetValue<string>("CorrelationIdPrefix") ?? "";
            var correlationIdValue = prefix + correlationId.ToString();

            context.Items[correlationHeader] = correlationId.ToString();

            // Add to response header BEFORE response starts
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[correlationHeader] = correlationId.ToString();
                return Task.CompletedTask;
            });

            //pass to the next middleware pipeline pushing correlation id to serilog context
            using(LogContext.PushProperty("CorrelationId", correlationId))
            {
                await _next(context);
            }

        }
    }
}
