using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Catalog;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class MedicineConfiguration : IEntityTypeConfiguration<Medicine>
{
    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.ToTable("Medicines");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.BrandName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(m => m.GenericName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(m => m.ActiveIngredient)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(m => m.Strength)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.DosageForm)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.CategoryId)
            .IsRequired();

        builder.Property(m => m.ManufacturerId)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(1000);

        builder.Property(m => m.RequiresPrescription)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        // Foreign Key Relationships
        builder.HasOne(m => m.Category)
            .WithMany()
            .HasForeignKey(m => m.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Manufacturer)
            .WithMany()
            .HasForeignKey(m => m.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Child entities
        builder.HasMany(m => m.Barcodes)
            .WithOne()
            .HasForeignKey(b => b.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Barcodes)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(m => m.Aliases)
            .WithOne()
            .HasForeignKey(a => a.MedicineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Aliases)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes for search
        builder.HasIndex(m => m.BrandName)
            .HasDatabaseName("IX_Medicines_BrandName");

        builder.HasIndex(m => m.GenericName)
            .HasDatabaseName("IX_Medicines_GenericName");

        builder.HasIndex(m => m.ActiveIngredient)
            .HasDatabaseName("IX_Medicines_ActiveIngredient");

        builder.HasIndex(m => new { m.IsActive, m.BrandName })
            .HasDatabaseName("IX_Medicines_IsActive_BrandName");
    }
}
