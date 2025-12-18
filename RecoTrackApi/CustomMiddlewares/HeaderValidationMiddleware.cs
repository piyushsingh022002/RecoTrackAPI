using System.Text.Json;
namespace RecoTrackApi.CustomMiddlewares
{
    public class HeaderValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private const string ClientIdHeader = "X-Client-Id";
        private const string CorrelationIdHeader = "X-Correlation-Id";

        public HeaderValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path;

            //allowed Path Lists
            if(path.StartsWithSegments("/swagger") || path.StartsWithSegments("/health") || path.StartsWithSegments("/hangfire"))
            {
                await _next(context);
                return;
            }

            //validate clientId Header 
            if( !context.Request.Headers.TryGetValue(ClientIdHeader, out var clientId) || string.IsNullOrWhiteSpace(clientId))
            {
                await WriteBadRequest(context);
                return;
            }

            //store ClientId for downstream usage
            context.Items[ClientIdHeader] = clientId.ToString();
            await _next(context);
        }

        private static async Task WriteBadRequest(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";

            var correlationId = context.Items.ContainsKey(CorrelationIdHeader)
                ? context.Items[CorrelationIdHeader]?.ToString()
                : null;

            var response = new
            {
                status = 400,
                error = "Missing required header: X-Client-Id",
                correlationId
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
