using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Integration;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Pharmacies;
namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class InventoryPublisherBindingConfiguration : IEntityTypeConfiguration<InventoryPublisherBinding>
{
    public void Configure(EntityTypeBuilder<InventoryPublisherBinding> b)
    {
        b.ToTable("InventoryPublisherBindings");
        b.HasKey(x => new { x.BranchId, x.ExternalProductId });
        b.Property(x => x.ExternalProductId).HasMaxLength(100);
        b.Property(x => x.IdentityFingerprint).HasMaxLength(64).IsRequired();
        b.HasIndex(x => new { x.BranchId, x.MedicineId }).IsUnique();
        b.HasOne<PharmacyBranch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Medicine>().WithMany().HasForeignKey(x => x.MedicineId).OnDelete(DeleteBehavior.Restrict);
    }
}
public class InventorySnapshotReceiptConfiguration : IEntityTypeConfiguration<InventorySnapshotReceipt>
{
    public void Configure(EntityTypeBuilder<InventorySnapshotReceipt> b)
    {
        b.ToTable("InventorySnapshotReceipts");
        b.HasKey(x => new { x.BranchId, x.InstallationId, x.SnapshotId });
        b.HasIndex(x => new { x.BranchId, x.Sequence }).IsUnique();
        b.Property(x => x.PayloadHash).HasMaxLength(64).IsRequired();
        b.HasOne<PharmacyBranch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
