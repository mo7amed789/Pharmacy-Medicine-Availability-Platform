using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Inventory;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class InventoryTransactionConfiguration : IEntityTypeConfiguration<InventoryTransaction>
{
    public void Configure(EntityTypeBuilder<InventoryTransaction> builder)
    {
        builder.ToTable("InventoryTransactions", t =>
        {
            t.HasCheckConstraint("CK_InventoryTransactions_ResultingQuantity", "[ResultingQuantity] >= 0");
            t.HasCheckConstraint("CK_InventoryTransactions_Reason", "[Reason] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11)");
        });

        builder.HasKey(t => t.Id);

        builder.Property(t => t.PharmacyInventoryId)
            .IsRequired();

        builder.Property(t => t.DeltaQuantity)
            .IsRequired();

        builder.Property(t => t.ResultingQuantity)
            .IsRequired();

        builder.Property(t => t.Reason)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(t => t.ActorUserId)
            .IsRequired();

        builder.Property(t => t.Notes)
            .HasMaxLength(500);

        builder.Property(t => t.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Audit queries: chronological transaction history for an inventory item
        builder.HasIndex(t => new { t.PharmacyInventoryId, t.CreatedAt })
            .HasDatabaseName("IX_InventoryTransactions_Inventory_Created");
    }
}
