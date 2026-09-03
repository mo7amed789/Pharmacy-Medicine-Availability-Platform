using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Integration;
using PharmacyAvailability.Domain.Pharmacies;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class InventorySyncLogConfiguration : IEntityTypeConfiguration<InventorySyncLog>
{
    public void Configure(EntityTypeBuilder<InventorySyncLog> builder)
    {
        builder.ToTable("InventorySyncLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.PharmacyId)
            .IsRequired();

        builder.Property(l => l.SyncBatchId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.StartedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(l => l.CompletedAt)
            .HasColumnType("datetime2")
            .IsRequired(false);

        builder.Property(l => l.Success)
            .IsRequired();

        builder.Property(l => l.ErrorMessage)
            .HasMaxLength(1000);

        builder.Property(l => l.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne<Pharmacy>()
            .WithMany()
            .HasForeignKey(l => l.PharmacyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.PharmacyId, l.StartedAt })
            .HasDatabaseName("IX_InventorySyncLogs_Pharmacy_Started");
    }
}
