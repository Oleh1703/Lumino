using System.Net;
using System.Text.Json;

namespace Lumino.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
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

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            var (statusCode, type, message) = MapException(ex);

            _logger.LogError(ex,
                "Unhandled exception: {Type} | {Message} | Path: {Path} | TraceId: {TraceId}",
                type,
                message,
                context.Request.Path.Value,
                context.TraceIdentifier
            );

            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json; charset=utf-8";

            var payload = new ApiErrorResponse
            {
                StatusCode = statusCode,
                Type = type,
                Message = message,
                TraceId = context.TraceIdentifier,
                Path = context.Request.Path.Value ?? "",
                TimestampUtc = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            await context.Response.WriteAsync(json);
        }

        private static (int statusCode, string type, string message) MapException(Exception ex)
        {
            if (ex is UnauthorizedAccessException)
            {
                return ((int)HttpStatusCode.Unauthorized, "unauthorized", ex.Message);
            }

            if (ex is KeyNotFoundException)
            {
                return ((int)HttpStatusCode.NotFound, "not_found", ex.Message);
            }

            if (ex is ArgumentException || ex is ArgumentNullException)
            {
                return ((int)HttpStatusCode.BadRequest, "bad_request", ex.Message);
            }

            // Неочікувані помилки
            return ((int)HttpStatusCode.InternalServerError, "server_error", "Unexpected server error.");
        }

        private class ApiErrorResponse
        {
            public int StatusCode { get; set; }
            public string Type { get; set; } = "";
            public string Message { get; set; } = "";
            public string TraceId { get; set; } = "";
            public string Path { get; set; } = "";
            public DateTime TimestampUtc { get; set; }
        }
    }
}
