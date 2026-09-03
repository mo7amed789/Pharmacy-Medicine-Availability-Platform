using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Inventory;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class PriceHistoryConfiguration : IEntityTypeConfiguration<PriceHistory>
{
    public void Configure(EntityTypeBuilder<PriceHistory> builder)
    {
        builder.ToTable("PriceHistories", t =>
        {
            t.HasCheckConstraint("CK_PriceHistories_OldPrice", "[OldPrice] >= 0.00");
            t.HasCheckConstraint("CK_PriceHistories_NewPrice", "[NewPrice] >= 0.00");
        });

        builder.HasKey(ph => ph.Id);

        builder.Property(ph => ph.PharmacyInventoryId)
            .IsRequired();

        builder.Property(ph => ph.OldPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(ph => ph.NewPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(ph => ph.ChangedByUserId)
            .IsRequired();

        builder.Property(ph => ph.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ph => ph.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Price audit query
        builder.HasIndex(ph => new { ph.PharmacyInventoryId, ph.CreatedAt })
            .HasDatabaseName("IX_PriceHistories_Inventory_Created");
    }
}
