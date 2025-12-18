using System.Text.Json;
namespace RecoTrackApi.CustomMiddlewares
{
    public class HeaderValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private const string ClientIdHeader = "X-Client-Id";
        private const string CorrelationIdHeader = "X-Correlation-Id";

        //predefined allowed clientIds can be added here for validation
        private static readonly HashSet<string> AllowedClients = new HashSet<string>
        {
            "web-ui-v1.0",
            "postman-v1.0",
            "swagger-ui-v1.0"
        };

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

            //Validate header value against allowlist
            if (!AllowedClients.Contains(clientId.ToString()))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                var errorResponse = new
                {
                    status = 400,
                    error = $"ClientId '{clientId}' is not allowed",
                    correlationId = context.Items["CorrelationId"]
                };
                await context.Response.WriteAsJsonAsync(errorResponse);
                return; // short-circuit
            }

            //store ClientId for downstream usage
            context.Items[ClientIdHeader] = clientId.ToString();

            //write the client Id in the Response Header BEFORE response starts
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[ClientIdHeader] = clientId.ToString();
                return Task.CompletedTask;
            });

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
