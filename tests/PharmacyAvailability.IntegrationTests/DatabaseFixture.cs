using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.IntegrationTests;

public class DatabaseFixture : IDisposable
{
    public string ConnectionString { get; } =
        "Server=(localdb)\\MSSQLLocalDB;Database=PharmacyAvailabilityDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<PharmacyDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new PharmacyDbContext(options);
        // Ensure migration is applied
        context.Database.Migrate();
    }

    public PharmacyDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PharmacyDbContext>()
            .UseSqlServer(ConnectionString)
            .LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information)
            .EnableSensitiveDataLogging()
            .Options;

        return new PharmacyDbContext(options);
    }

    public void Dispose()
    {
        // Keep DB for inspections/further steps or cleanup test data
    }
}
