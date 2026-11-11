using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Identity;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
{
    public void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        builder.ToTable("VerificationCodes");

        builder.HasKey(vc => vc.Id);

        builder.Property(vc => vc.UserId)
            .IsRequired();

        builder.Property(vc => vc.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(vc => vc.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(vc => vc.ExpiresAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(vc => vc.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(vc => vc.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(vc => vc.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(vc => new { vc.UserId, vc.Type, vc.IsUsed, vc.CreatedAt })
            .HasDatabaseName("IX_VerificationCodes_UserId_Type_IsUsed");
    }
}
