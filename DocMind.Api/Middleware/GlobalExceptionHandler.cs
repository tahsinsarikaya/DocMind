using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DocMind.Api.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (statusCode, title) = MapException(exception);

            _logger.LogError(exception, "Request failed with {StatusCode}: {Title}", statusCode, title);

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            };

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }

        private static (int StatusCode, string Title) MapException(Exception exception) => exception switch
        {
            ArgumentException => (StatusCodes.Status400BadRequest, "Invalid request"),

            // Ollama/Qdrant'a bağlanılamazsa (servisler ayakta değilse) HttpRequestException fırlar —
            // kullanıcıya çirkin bir stack trace yerine anlamlı bir "servis kullanılamıyor" mesajı.
            HttpRequestException => (StatusCodes.Status503ServiceUnavailable, "A dependent service (Ollama or Qdrant) is unavailable"),

            TaskCanceledException or OperationCanceledException =>
                (StatusCodes.Status504GatewayTimeout, "The request to a dependent service (likely the LLM) timed out"),

            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };
    }
}
