using System.Text.Json;
namespace RecoTrackApi.CustomMiddlewares
{
    public class HeaderValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private const string ClientIdHeader = "X-Client-Id";
        private const string CorrelationIdHeader = "X-Correlation-Id";

        //predefined allowed clientIds can be added here for validation
        private readonly HashSet<string> _allowedClients;

        public HeaderValidationMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;

            //detect environment and load allowed clients accordingly
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            IEnumerable<string> allowed;

            if (env == "Development")
            {
                allowed = configuration.GetSection("ClientSettings:AllowedClients").Get<string[]>() ?? Array.Empty<string>();
            }
            else
            {
                var envClients = Environment.GetEnvironmentVariable("ALLOWED_CLIENTS") ?? "";
                allowed = envClients.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }

            _allowedClients = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);

        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

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
            if (!_allowedClients.Contains(clientId.ToString()))
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
