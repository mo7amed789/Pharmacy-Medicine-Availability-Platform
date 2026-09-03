using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Api.Middleware;
using PharmacyAvailability.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// 1. Kestrel Server Hardening
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB max body size
});

// 2. Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<PharmacyDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(PharmacyDbContext).Assembly.FullName);
    }));

// 3. CORS Policy Configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 4. Controllers & OpenAPI / Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5. Middleware Pipeline (Strict Ordering)
// A. Correlation ID first so all subsequent logs and error handlers inherit it
app.UseMiddleware<CorrelationIdMiddleware>();

// B. Global Exception Handler to catch any downstream exception and return RFC 7807 ProblemDetails
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// C. Structured Request Logging with duration metrics
app.UseMiddleware<RequestLoggingMiddleware>();

// D. Security Headers (X-Content-Type-Options, X-Frame-Options, etc.)
app.UseMiddleware<SecurityHeadersMiddleware>();

// E. Production HTTPS & HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// F. CORS
app.UseCors("CorsPolicy");

// G. Swagger UI (Development / Local testing only - not exposed in production)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pharmacy Availability API v1");
    });
}

// H. Endpoints
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "Pharmacy Availability API",
    timestamp = DateTime.UtcNow
}));

app.Run();

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program { }
