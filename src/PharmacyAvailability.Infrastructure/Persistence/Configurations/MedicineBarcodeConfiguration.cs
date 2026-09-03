using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Catalog;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class MedicineBarcodeConfiguration : IEntityTypeConfiguration<MedicineBarcode>
{
    public void Configure(EntityTypeBuilder<MedicineBarcode> builder)
    {
        builder.ToTable("MedicineBarcodes");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.MedicineId)
            .IsRequired();

        builder.Property(b => b.Barcode)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(b => b.Barcode)
            .IsUnique()
            .HasDatabaseName("IX_MedicineBarcodes_Barcode");

        builder.Property(b => b.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();
    }
}
