using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<PharmacyDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(PharmacyDbContext).Assembly.FullName);
    }));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.MapGet("/", () => Results.Ok(new { status = "Healthy", service = "Pharmacy Availability API" }));

app.Run();

// Make Program accessible for WebApplicationFactory in integration tests
public partial class Program { }
