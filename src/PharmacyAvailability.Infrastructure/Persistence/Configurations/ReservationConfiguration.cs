using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Reservations;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations", t =>
        {
            t.HasCheckConstraint("CK_Reservations_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_Reservations_Status", "[Status] IN (1, 2, 3, 4, 5)");
            t.HasCheckConstraint("CK_Reservations_ExpiresAt", "[ExpiresAt] > [CreatedAt]");
        });

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Quantity)
            .IsRequired();

        builder.Property(r => r.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(r => r.ExpiresAt)
            .IsRequired()
            .HasColumnType("datetime2");

        builder.Property(r => r.ConfirmedAt)
            .HasColumnType("datetime2");

        builder.Property(r => r.ReleasedAt)
            .HasColumnType("datetime2");

        builder.Property(r => r.CancelledAt)
            .HasColumnType("datetime2");

        builder.Property(r => r.RowVersion)
            .IsRowVersion();

        // Foreign keys with Restrict
        builder.HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Pharmacy)
            .WithMany()
            .HasForeignKey(r => r.PharmacyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Branch)
            .WithMany()
            .HasForeignKey(r => r.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Medicine)
            .WithMany()
            .HasForeignKey(r => r.MedicineId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(r => new { r.CustomerId, r.CreatedAt });
        builder.HasIndex(r => new { r.PharmacyId, r.BranchId, r.Status });
        builder.HasIndex(r => new { r.BranchId, r.MedicineId, r.Status });
        builder.HasIndex(r => new { r.Status, r.ExpiresAt });
    }
}
