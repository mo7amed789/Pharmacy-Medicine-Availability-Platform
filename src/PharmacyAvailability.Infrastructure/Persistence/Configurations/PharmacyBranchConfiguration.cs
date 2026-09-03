using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Pharmacies;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class PharmacyBranchConfiguration : IEntityTypeConfiguration<PharmacyBranch>
{
    public void Configure(EntityTypeBuilder<PharmacyBranch> builder)
    {
        builder.ToTable("PharmacyBranches", t =>
        {
            t.HasCheckConstraint("CK_PharmacyBranches_Latitude", "[Latitude] >= -90.0 AND [Latitude] <= 90.0");
            t.HasCheckConstraint("CK_PharmacyBranches_Longitude", "[Longitude] >= -180.0 AND [Longitude] <= 180.0");
        });

        builder.HasKey(b => b.Id);

        builder.Property(b => b.PharmacyId)
            .IsRequired();

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.BranchCode)
            .HasMaxLength(50);

        builder.Property(b => b.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(b => b.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.Latitude)
            .HasColumnType("decimal(9,6)")
            .IsRequired();

        builder.Property(b => b.Longitude)
            .HasColumnType("decimal(9,6)")
            .IsRequired();

        builder.Property(b => b.Phone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.OpeningHours)
            .HasMaxLength(200);

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        // Foreign Key Relationship
        builder.HasOne(b => b.Pharmacy)
            .WithMany(p => p.Branches)
            .HasForeignKey(b => b.PharmacyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for querying
        builder.HasIndex(b => b.PharmacyId)
            .HasDatabaseName("IX_PharmacyBranches_PharmacyId");

        builder.HasIndex(b => new { b.IsActive, b.Latitude, b.Longitude })
            .HasDatabaseName("IX_PharmacyBranches_IsActive_Coordinates");

        builder.HasIndex(b => b.City)
            .HasDatabaseName("IX_PharmacyBranches_City");
    }
}
