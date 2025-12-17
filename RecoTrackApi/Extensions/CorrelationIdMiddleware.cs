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

            context.Items[correlationHeader] = correlationId.ToString();

            //pass to the next middleware pipeline
            await _next(context);
        }
    }
}
