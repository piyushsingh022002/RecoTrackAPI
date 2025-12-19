using RecoTrack.Application.Exceptions;
using RecoTrack.Shared.Contracts.Errors;
using System.Net;

namespace RecoTrackApi.CustomMiddlewares
{
    public sealed class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, errorCode, message) = MapException(exception);

            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "N/A";
            var clientId = context.Items["X-Client-Id"]?.ToString();
            var path = context.Request.Path.Value ?? "unknown";

            _logger.LogError(
                exception,
                "Unhandled exception | CorrelationId: {CorrelationId} | ClientId: {ClientId} | Path: {Path}",
                correlationId,
                clientId,
                path);

            var response = new ApiErrorResponse
            {
                Status = statusCode,
                ErrorCode = errorCode,
                Message = message,
                CorrelationId = correlationId,
                ClientId = clientId,
                Path = path
            };

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(response);
        }

        private static (int StatusCode, string ErrorCode, string Message) MapException(Exception exception)
        {
            return exception switch
            {
                BusinessException be => (
                    (int)HttpStatusCode.BadRequest,
                    be.ErrorCode,
                    be.Message
                ),

                AuthException ae => (
                    (int)HttpStatusCode.Unauthorized,
                    ae.ErrorCode,
                    ae.Message
                ),

                RecoTrack.Application.Exceptions.SystemException se => (
                    (int)HttpStatusCode.InternalServerError,
                    se.ErrorCode,
                    "An unexpected system error occurred."
                ),

                CriticalException ce => (
                    (int)HttpStatusCode.InternalServerError,
                    ce.ErrorCode,
                    "A critical error occurred. The team has been notified."
                ),

                _ => (
                    (int)HttpStatusCode.InternalServerError,
                    "GEN-500",
                    "An unexpected error occurred."
                )
            };
        }
    }
}
