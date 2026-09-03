using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Inventory;
using PharmacyAvailability.Domain.Pharmacies;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class PharmacyInventoryConfiguration : IEntityTypeConfiguration<PharmacyInventory>
{
    public void Configure(EntityTypeBuilder<PharmacyInventory> builder)
    {
        builder.ToTable("PharmacyInventories", t =>
        {
            t.HasCheckConstraint("CK_PharmacyInventories_AvailableQuantity", "[AvailableQuantity] >= 0");
            t.HasCheckConstraint("CK_PharmacyInventories_ReservedQuantity", "[ReservedQuantity] >= 0");
            t.HasCheckConstraint("CK_PharmacyInventories_Price", "[Price] >= 0.00");
            t.HasCheckConstraint("CK_PharmacyInventories_Source", "[Source] IN (1, 2, 3)");
        });

        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.PharmacyId)
            .IsRequired();

        builder.Property(pi => pi.MedicineId)
            .IsRequired();

        builder.Property(pi => pi.AvailableQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(pi => pi.ReservedQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        // Monetary value with explicit decimal(18,2) precision
        builder.Property(pi => pi.Price)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(pi => pi.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("EGP");

        builder.Property(pi => pi.Source)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(pi => pi.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(pi => pi.UpdatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(pi => pi.LastSuccessfulSyncAt)
            .HasColumnType("datetime2")
            .IsRequired(false);

        // Optimistic concurrency token mapped to SQL Server ROWVERSION / TIMESTAMP
        builder.Property(pi => pi.RowVersion)
            .IsRowVersion();

        // Foreign keys to parent entities with Restrict to protect historical records
        builder.HasOne<Pharmacy>()
            .WithMany()
            .HasForeignKey(pi => pi.PharmacyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Medicine>()
            .WithMany()
            .HasForeignKey(pi => pi.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Child audit collections with Restrict to prevent accidental cascades
        builder.HasMany(pi => pi.Transactions)
            .WithOne()
            .HasForeignKey(t => t.PharmacyInventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(pi => pi.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(pi => pi.PriceHistories)
            .WithOne()
            .HasForeignKey(ph => ph.PharmacyInventoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(pi => pi.PriceHistories)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Unique constraint: one inventory record per pharmacy/medicine pair
        builder.HasIndex(pi => new { pi.PharmacyId, pi.MedicineId })
            .IsUnique()
            .HasDatabaseName("IX_PharmacyInventories_PharmacyId_MedicineId");

        // Primary query path: Finding available pharmacies for a given medicine
        builder.HasIndex(pi => new { pi.MedicineId, pi.AvailableQuantity })
            .IncludeProperties(pi => new { pi.PharmacyId, pi.Price, pi.UpdatedAt, pi.Source })
            .HasDatabaseName("IX_PharmacyInventories_Medicine_Stock_Covering");

        // Secondary query path: Pharmacy dashboard loading its full inventory
        builder.HasIndex(pi => pi.PharmacyId)
            .HasDatabaseName("IX_PharmacyInventories_PharmacyId");
    }
}
