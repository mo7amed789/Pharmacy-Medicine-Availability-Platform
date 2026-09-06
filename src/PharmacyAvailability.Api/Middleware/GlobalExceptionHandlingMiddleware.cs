using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Api.Middleware;

public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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
        var correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
            ?? Guid.NewGuid().ToString("D");

        var problemDetails = CreateProblemDetails(context, exception, correlationId);

        // Internal structured logging with full exception details and stack trace
        if (problemDetails.Status >= 500)
        {
            _logger.LogError(
                exception,
                "Unhandled server error occurred on {Method} {Path}. CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);
        }
        else
        {
            _logger.LogWarning(
                "Client error ({StatusCode}) on {Method} {Path}: {Message}. CorrelationId: {CorrelationId}",
                problemDetails.Status,
                context.Request.Method,
                context.Request.Path,
                exception.Message,
                correlationId);
        }

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("The response has already started, cannot write ProblemDetails for CorrelationId: {CorrelationId}", correlationId);
            return;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        var json = JsonSerializer.Serialize(problemDetails, problemDetails.GetType(), JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception, string correlationId)
    {
        ProblemDetails problem;

        switch (exception)
        {
            case KeyNotFoundException notFoundEx:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource Not Found",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Detail = notFoundEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case UnauthorizedAccessException unauthEx:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Detail = unauthEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case InsufficientStockException stockEx:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Insufficient Stock",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Detail = stockEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case ResourceConflictException confEx:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Resource Conflict",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Detail = confEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case InvalidPriceException priceEx:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid Price",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = priceEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case DomainValidationException valEx:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Error",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = valEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case DomainException domEx:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Domain Rule Violation",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = domEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case DbUpdateConcurrencyException:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Concurrency Conflict",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Detail = "A concurrency conflict occurred. The resource was modified by another operation. Please refresh and retry.",
                    Instance = context.Request.Path
                };
                break;

            case DbUpdateException:
                // Mask internal SQL details to prevent sensitive database exposure
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Data Integrity Conflict",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Detail = "The operation could not be completed because a database constraint or uniqueness rule was violated.",
                    Instance = context.Request.Path
                };
                break;

            case BadHttpRequestException badHttpEx:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid Request",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = badHttpEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case FluentValidation.ValidationException fvEx:
                var validationProblem = new HttpValidationProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = "Please refer to the errors property for additional details.",
                    Instance = context.Request.Path
                };
                foreach (var failure in fvEx.Errors)
                {
                    if (validationProblem.Errors.TryGetValue(failure.PropertyName, out var existingErrors))
                    {
                        validationProblem.Errors[failure.PropertyName] = existingErrors.Concat(new[] { failure.ErrorMessage }).ToArray();
                    }
                    else
                    {
                        validationProblem.Errors[failure.PropertyName] = new[] { failure.ErrorMessage };
                    }
                }
                problem = validationProblem;
                break;

            case ForbiddenAccessException forbEx:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    Detail = forbEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case ArgumentException argEx:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Invalid Argument",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = argEx.Message,
                    Instance = context.Request.Path
                };
                break;

            case OperationCanceledException:
                problem = new ProblemDetails
                {
                    Status = 499,
                    Title = "Request Cancelled",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Detail = "The operation was cancelled by the client.",
                    Instance = context.Request.Path
                };
                break;

            case TimeoutException:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status504GatewayTimeout,
                    Title = "Request Timeout",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.5",
                    Detail = "The requested operation timed out while communicating with downstream services.",
                    Instance = context.Request.Path
                };
                break;

            case NotImplementedException:
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status501NotImplemented,
                    Title = "Not Implemented",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.2",
                    Detail = "The requested feature or endpoint is not yet implemented.",
                    Instance = context.Request.Path
                };
                break;

            default:
                // Server 500: Never expose raw exception message or stack trace in production
                var detail = _env.IsDevelopment()
                    ? $"An unexpected error occurred: {exception.Message}"
                    : "An unexpected error occurred. Please contact support with the provided Correlation ID.";

                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Detail = detail,
                    Instance = context.Request.Path
                };
                break;
        }

        // Always attach Correlation ID for tracing
        problem.Extensions["correlationId"] = correlationId;

        return problem;
    }
}
