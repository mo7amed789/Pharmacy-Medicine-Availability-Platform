using System.Diagnostics;

namespace PharmacyAvailability.Api.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();

            var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString() ?? "N/A";
            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (statusCode >= 500)
            {
                _logger.LogError(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                    method, path, statusCode, elapsedMs, correlationId);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                    method, path, statusCode, elapsedMs, correlationId);
            }
            else
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms. CorrelationId: {CorrelationId}",
                    method, path, statusCode, elapsedMs, correlationId);
            }
        }
    }
}
