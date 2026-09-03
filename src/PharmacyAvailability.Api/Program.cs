using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PharmacyAvailability.Api.Authorization;
using PharmacyAvailability.Api.Middleware;
using PharmacyAvailability.Application.Auth;
using PharmacyAvailability.Application.Common.Interfaces;
using PharmacyAvailability.Application.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Infrastructure.Catalog;
using PharmacyAvailability.Infrastructure.Persistence;
using PharmacyAvailability.Infrastructure.Security;

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

// 3. Security & Cryptography Services
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICatalogService, CatalogService>();

// 4. JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException("Fatal Security Error: 'Jwt:Secret' configuration is missing or too short.");
}

if (!builder.Environment.IsDevelopment() && jwtSecret.Contains("Development_Only", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Fatal Security Error: A production-grade 'Jwt:Secret' must be provided via environment variables in non-development environments.");
}

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = key,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Strict expiration without 5-minute default clock skew
    };
});

// 5. Role-Based & Resource Authorization
builder.Services.AddScoped<IAuthorizationHandler, PharmacyOwnershipHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PlatformAdminOnly", policy =>
        policy.RequireRole(UserRole.PlatformAdmin.ToString()));

    options.AddPolicy("PharmacyStaffOrAdmin", policy =>
        policy.RequireRole(
            UserRole.PharmacyStaff.ToString(),
            UserRole.PharmacyAdmin.ToString(),
            UserRole.PlatformAdmin.ToString()));

    options.AddPolicy("CustomerOnly", policy =>
        policy.RequireRole(UserRole.Customer.ToString()));
});

// 6. Rate Limiting for Abuse Mitigation
var authRateLimit = builder.Configuration.GetValue<int>("RateLimiting:AuthEndpointLimit", 100);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("AuthRateLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

// 7. CORS Policy Configuration
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

// 8. Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 9. Middleware Pipeline (Strict Ordering)
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

// G. Rate Limiter
app.UseRateLimiter();

// H. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// I. Swagger UI (Development / Local testing only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pharmacy Availability API v1");
    });
}

// J. Endpoints
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
