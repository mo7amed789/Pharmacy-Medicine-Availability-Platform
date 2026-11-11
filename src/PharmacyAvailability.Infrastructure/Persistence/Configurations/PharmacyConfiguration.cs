using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Pharmacies;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class PharmacyConfiguration : IEntityTypeConfiguration<Pharmacy>
{
    public void Configure(EntityTypeBuilder<Pharmacy> builder)
    {
        builder.ToTable("Pharmacies", t =>
        {
            t.HasCheckConstraint("CK_Pharmacies_Latitude", "[Latitude] >= -90.0 AND [Latitude] <= 90.0");
            t.HasCheckConstraint("CK_Pharmacies_Longitude", "[Longitude] >= -180.0 AND [Longitude] <= 180.0");
            t.HasCheckConstraint("CK_Pharmacies_Status", "[Status] IN (1, 2, 3, 4)");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.LicenseNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.LicenseNumber)
            .IsUnique()
            .HasDatabaseName("IX_Pharmacies_LicenseNumber");

        builder.Property(p => p.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.City)
            .IsRequired()
            .HasMaxLength(100);

        // SQL Server float or decimal for coordinates. Decimal(9,6) offers sub-meter accuracy.
        builder.Property(p => p.Latitude)
            .HasColumnType("decimal(9,6)")
            .IsRequired();

        builder.Property(p => p.Longitude)
            .HasColumnType("decimal(9,6)")
            .IsRequired();

        builder.Property(p => p.Phone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Email)
            .HasMaxLength(256);

        builder.Property(p => p.OpeningHours)
            .HasMaxLength(200);

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        // Index for location-based queries filtering by active status
        builder.HasIndex(p => new { p.Status, p.Latitude, p.Longitude })
            .HasDatabaseName("IX_Pharmacies_Status_Coordinates");

        // Supports the public active-pharmacy list without scanning every tenant.
        builder.HasIndex(p => new { p.Status, p.City, p.Name })
            .HasDatabaseName("IX_Pharmacies_Status_City_Name");

        // Child branches navigation
        builder.HasMany(p => p.Branches)
            .WithOne(b => b.Pharmacy)
            .HasForeignKey(b => b.PharmacyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(p => p.Branches)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
