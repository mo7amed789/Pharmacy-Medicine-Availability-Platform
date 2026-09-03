using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Integration;
using PharmacyAvailability.Domain.Pharmacies;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class PharmacyProductMappingConfiguration : IEntityTypeConfiguration<PharmacyProductMapping>
{
    public void Configure(EntityTypeBuilder<PharmacyProductMapping> builder)
    {
        builder.ToTable("PharmacyProductMappings", t =>
        {
            t.HasCheckConstraint("CK_PharmacyProductMappings_Status", "[Status] IN (1, 2, 3, 4)");
        });

        builder.HasKey(m => m.Id);

        builder.Property(m => m.PharmacyId)
            .IsRequired();

        builder.Property(m => m.MedicineId)
            .IsRequired();

        builder.Property(m => m.ExternalProductId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.ExternalBarcode)
            .HasMaxLength(100);

        builder.Property(m => m.ExternalName)
            .HasMaxLength(250);

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(m => m.LastSyncAt)
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.HasOne<Pharmacy>()
            .WithMany()
            .HasForeignKey(m => m.PharmacyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Medicine>()
            .WithMany()
            .HasForeignKey(m => m.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // A pharmacy can only have one mapping per external product ID
        builder.HasIndex(m => new { m.PharmacyId, m.ExternalProductId })
            .IsUnique()
            .HasDatabaseName("IX_PharmacyProductMappings_Pharmacy_ExternalId");

        builder.HasIndex(m => m.ExternalBarcode)
            .HasDatabaseName("IX_PharmacyProductMappings_ExternalBarcode");
    }
}
