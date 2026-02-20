using System.Net;
using System.Text.Json;
using Lumino.Api.Utils;
using Microsoft.AspNetCore.Hosting;
using System.Diagnostics;

namespace Lumino.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
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

            // Не шумимо у тестах на очікуваних 4xx (вони спеціально викликаються в інтеграційних тестах)
            if (!(_environment.IsEnvironment("Testing") && statusCode < 500))
            {
                if (statusCode >= 500)
                {
                    _logger.LogError(ex,
                        "Unhandled exception: {Type} | {Message} | Path: {Path} | TraceId: {TraceId}",
                        type,
                        message,
                        context.Request.Path.Value,
                        context.TraceIdentifier
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Handled exception: {Type} | {Message} | Path: {Path} | TraceId: {TraceId}",
                        type,
                        message,
                        context.Request.Path.Value,
                        context.TraceIdentifier
                    );
                }
            }

            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json; charset=utf-8";

            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            var payload = new ApiProblemDetails
            {
                Type = type,
                Title = ToTitle(statusCode),
                Status = statusCode,
                Detail = message,
                Instance = context.Request.Path.Value ?? "",
                TraceId = traceId
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
            if (ex is ForbiddenAccessException)
            {
                return ((int)HttpStatusCode.Forbidden, "forbidden", ex.Message);
            }

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

        private static string ToTitle(int statusCode)
        {
            if (statusCode == (int)HttpStatusCode.BadRequest) return "Bad Request";
            if (statusCode == (int)HttpStatusCode.Unauthorized) return "Unauthorized";
            if (statusCode == (int)HttpStatusCode.Forbidden) return "Forbidden";
            if (statusCode == (int)HttpStatusCode.NotFound) return "Not Found";
            return "Server Error";
        }

        private class ApiProblemDetails
        {
            public string Type { get; set; } = "";
            public string Title { get; set; } = "";
            public int Status { get; set; }
            public string Detail { get; set; } = "";
            public string Instance { get; set; } = "";
            public string TraceId { get; set; } = "";
        }
    }
}
