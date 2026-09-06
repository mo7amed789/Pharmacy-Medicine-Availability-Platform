using System.IO;
using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using PharmacyAvailability.Api.Middleware;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.UnitTests.Middleware;

public class GlobalExceptionHandlingMiddlewareTests
{
    private readonly NullLogger<GlobalExceptionHandlingMiddleware> _logger = new();
    private readonly TestHostEnvironment _env = new() { EnvironmentName = Environments.Development };

    private class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private static (DefaultHttpContext context, MemoryStream bodyStream) CreateHttpContext(string? correlationId = null)
    {
        var context = new DefaultHttpContext();
        var stream = new MemoryStream();
        context.Response.Body = stream;
        if (!string.IsNullOrEmpty(correlationId))
        {
            context.Items[CorrelationIdMiddleware.CorrelationIdItemKey] = correlationId;
        }
        return (context, stream);
    }

    private static async Task<JsonElement> ReadResponseBodyAsync(MemoryStream stream)
    {
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    [Fact]
    public async Task InvokeAsync_FluentValidationException_Returns400WithErrorsMap()
    {
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required."),
            new("Password", "Password must be at least 8 characters.")
        };
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new FluentValidation.ValidationException(failures),
            _logger,
            _env);

        var (context, stream) = CreateHttpContext("corr-validation-123");
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);

        var body = await ReadResponseBodyAsync(stream);
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.Equal("One or more validation errors occurred.", body.GetProperty("title").GetString());
        Assert.Equal("corr-validation-123", body.GetProperty("correlationId").GetString());

        var errors = body.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Email", out var emailErrors));
        Assert.Contains("Email is required.", emailErrors[0].GetString());
        Assert.True(errors.TryGetProperty("Password", out var passErrors));
        Assert.Contains("Password must be at least 8 characters.", passErrors[0].GetString());
    }

    [Fact]
    public async Task InvokeAsync_ForbiddenAccessException_Returns403Forbidden()
    {
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new ForbiddenAccessException("You cannot access branch settings for other pharmacies."),
            _logger,
            _env);

        var (context, stream) = CreateHttpContext("corr-forbidden-403");
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(stream);
        Assert.Equal(403, body.GetProperty("status").GetInt32());
        Assert.Equal("Forbidden", body.GetProperty("title").GetString());
        Assert.Equal("You cannot access branch settings for other pharmacies.", body.GetProperty("detail").GetString());
        Assert.Equal("corr-forbidden-403", body.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_Returns404NotFound()
    {
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new KeyNotFoundException("Medicine with ID not found."),
            _logger,
            _env);

        var (context, stream) = CreateHttpContext("corr-404");
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(stream);
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("Resource Not Found", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ResourceConflictException_Returns409Conflict()
    {
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new ResourceConflictException("Hold has already expired."),
            _logger,
            _env);

        var (context, stream) = CreateHttpContext("corr-409");
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(stream);
        Assert.Equal(409, body.GetProperty("status").GetInt32());
        Assert.Equal("Resource Conflict", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task InvokeAsync_InsufficientStockException_Returns409Conflict()
    {
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new InsufficientStockException(Guid.NewGuid(), 2, 5),
            _logger,
            _env);

        var (context, stream) = CreateHttpContext();
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(stream);
        Assert.Equal(409, body.GetProperty("status").GetInt32());
        Assert.Equal("Insufficient Stock", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns400BadRequest()
    {
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new ArgumentException("Search radius must be positive."),
            _logger,
            _env);

        var (context, stream) = CreateHttpContext();
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(stream);
        Assert.Equal(400, body.GetProperty("status").GetInt32());
        Assert.Equal("Invalid Argument", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500WithSanitizedMessageInProduction()
    {
        var prodEnv = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw new NullReferenceException("Sensitive server pointer null"),
            _logger,
            prodEnv);

        var (context, stream) = CreateHttpContext("corr-500-prod");
        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var body = await ReadResponseBodyAsync(stream);
        Assert.Equal(500, body.GetProperty("status").GetInt32());
        Assert.Equal("Internal Server Error", body.GetProperty("title").GetString());
        Assert.DoesNotContain("Sensitive server pointer null", body.GetProperty("detail").GetString());
        Assert.Equal("corr-500-prod", body.GetProperty("correlationId").GetString());
    }
}
