namespace PharmacyAvailability.Api.Middleware;

public class CorrelationIdMiddleware
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";
    public const string CorrelationIdItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate _next, ILogger<CorrelationIdMiddleware> logger)
    {
        this._next = _next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check for incoming correlation ID from client/gateway
        string correlationId;
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var incomingId) &&
            !string.IsNullOrWhiteSpace(incomingId))
        {
            correlationId = incomingId.ToString();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("D");
        }

        // Store in HttpContext.Items for downstream middleware and controllers
        context.Items[CorrelationIdItemKey] = correlationId;

        // Echo correlation ID back in response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = correlationId;
            return Task.CompletedTask;
        });

        // Attach correlation ID to log scope for all downstream logging
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
