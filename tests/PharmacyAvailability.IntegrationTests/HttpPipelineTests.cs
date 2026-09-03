using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace PharmacyAvailability.IntegrationTests;

public class HttpPipelineTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HttpPipelineTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_Returns200AndHealthyStatus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.Equal("Healthy", doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("Pharmacy Availability API", doc.RootElement.GetProperty("service").GetString());
    }

    [Fact]
    public async Task CorrelationId_WhenSuppliedByClient_IsEchoedInResponse()
    {
        // Arrange
        var client = _factory.CreateClient();
        var customId = $"client-corr-{Guid.NewGuid():N}";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("X-Correlation-ID", customId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.Equal(customId, values.First());
    }

    [Fact]
    public async Task CorrelationId_WhenOmittedByClient_IsGeneratedAndReturned()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        var generatedId = values.First();
        Assert.False(string.IsNullOrWhiteSpace(generatedId));
        Assert.True(Guid.TryParse(generatedId, out _));
    }

    [Fact]
    public async Task SecurityHeaders_ArePresentInAllResponses()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var contentTypeOptions));
        Assert.Equal("nosniff", contentTypeOptions.First());

        Assert.True(response.Headers.TryGetValues("X-Frame-Options", out var frameOptions));
        Assert.Equal("DENY", frameOptions.First());

        Assert.True(response.Headers.TryGetValues("X-XSS-Protection", out var xssProtection));
        Assert.Equal("0", xssProtection.First());

        Assert.True(response.Headers.TryGetValues("Referrer-Policy", out var referrerPolicy));
        Assert.Equal("strict-origin-when-cross-origin", referrerPolicy.First());
    }

    [Fact]
    public async Task ProblemDetails_DomainValidationException_Returns400WithProblemDetails()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/domain-validation");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal("Validation Error", root.GetProperty("title").GetString());
        Assert.Equal("Medicine name is required.", root.GetProperty("detail").GetString());
        Assert.True(root.TryGetProperty("correlationId", out var corrId));
        Assert.False(string.IsNullOrWhiteSpace(corrId.GetString()));
    }

    [Fact]
    public async Task ProblemDetails_InsufficientStockException_Returns409Conflict()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/insufficient-stock");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal(409, root.GetProperty("status").GetInt32());
        Assert.Equal("Insufficient Stock", root.GetProperty("title").GetString());
        Assert.Contains("Insufficient stock for inventory", root.GetProperty("detail").GetString());
        Assert.True(root.TryGetProperty("correlationId", out _));
    }

    [Fact]
    public async Task ProblemDetails_InvalidPriceException_Returns400BadRequest()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/invalid-price");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal(400, root.GetProperty("status").GetInt32());
        Assert.Equal("Invalid Price", root.GetProperty("title").GetString());
        Assert.Contains("Price cannot be negative", root.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ProblemDetails_ConcurrencyConflict_Returns409Conflict()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/concurrency-conflict");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal(409, root.GetProperty("status").GetInt32());
        Assert.Equal("Concurrency Conflict", root.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ProblemDetails_UnhandledError_InProduction_HidesInternalSecrets()
    {
        // Arrange: Create a client running with Production environment and secure production key
        var prodFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Jwt:Secret", "ProductionGradeSecretKeyWithMoreThan64CharactersForEnterpriseSecurity1234567890!");
        });
        var client = prodFactory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/test/unhandled-error");

        // Assert
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();

        // Must NOT leak simulated credentials, SQL connection strings, or stack trace
        Assert.DoesNotContain("secret123", content);
        Assert.DoesNotContain("password", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("10.0.0.1", content);
        Assert.DoesNotContain("at PharmacyAvailability", content);

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        Assert.Equal(500, root.GetProperty("status").GetInt32());
        Assert.Equal("Internal Server Error", root.GetProperty("title").GetString());
        Assert.Equal("An unexpected error occurred. Please contact support with the provided Correlation ID.",
            root.GetProperty("detail").GetString());
        Assert.True(root.TryGetProperty("correlationId", out var corrId));
        Assert.False(string.IsNullOrWhiteSpace(corrId.GetString()));
    }

    [Fact]
    public async Task CORS_AllowedOrigin_ReturnsCorsHeaders()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "http://localhost:5173");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var originValues));
        Assert.Equal("http://localhost:5173", originValues.First());
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out var credValues));
        Assert.Equal("true", credValues.First());
    }

    [Fact]
    public async Task SwaggerEndpoint_InDevelopment_IsAccessible()
    {
        // Arrange: Default factory runs in Development
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        Assert.True(doc.RootElement.TryGetProperty("openapi", out _) || doc.RootElement.TryGetProperty("swagger", out _));
    }
}
