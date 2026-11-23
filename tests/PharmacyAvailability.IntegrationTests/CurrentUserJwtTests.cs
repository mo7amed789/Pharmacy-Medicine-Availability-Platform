using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Infrastructure.Persistence;
using PharmacyAvailability.Infrastructure.Security;

namespace PharmacyAvailability.IntegrationTests;

public class CurrentUserJwtTests
{
    [Fact]
    public async Task PreviouslyValidJwtRejectedImmediatelyAfterDeactivation()
    {
        var connection = Environment.GetEnvironmentVariable("INVENTORY_TEST_SQLSERVER")
            ?? "Server=(localdb)\\InventoryReceiverTests;Database=InventoryReceiverSafety;Integrated Security=true;TrustServerCertificate=true";
        const string secret = "SyntheticOnly_CurrentUser_TestKey_123456789012345678901234567890";
        var isolated = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection) { InitialCatalog = "CurrentUserJwt_" + Guid.NewGuid().ToString("N") };
        connection = isolated.ConnectionString;
        var options = new DbContextOptionsBuilder<PharmacyDbContext>().UseSqlServer(connection).Options;
        await using var db = new PharmacyDbContext(options);
        await db.Database.MigrateAsync();
        var user = new User($"jwt-{Guid.NewGuid()}@example.invalid", "fake-hash", "Synthetic", UserRole.Customer, "000");
        db.Add(user); await db.SaveChangesAsync();
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => {
                b.UseEnvironment("Testing");
                b.UseSetting("ConnectionStrings:DefaultConnection", connection);
                b.UseSetting("Jwt:Secret", secret);
                b.UseSetting("Jwt:Issuer", "CurrentUserTest");
                b.UseSetting("Jwt:Audience", "CurrentUserTest");
            });
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
            var service = new JwtTokenService(Options.Create(new JwtOptions { Secret=secret, Issuer="CurrentUserTest", Audience="CurrentUserTest", AccessTokenLifetimeMinutes=15 }));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", service.GenerateAccessToken(user).Token);
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
            user.Deactivate(); await db.SaveChangesAsync();
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        }
        finally { await db.Database.EnsureDeletedAsync(); }
    }
}
