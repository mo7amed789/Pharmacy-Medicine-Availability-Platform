using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Catalog;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class MedicineAliasConfiguration : IEntityTypeConfiguration<MedicineAlias>
{
    public void Configure(EntityTypeBuilder<MedicineAlias> builder)
    {
        builder.ToTable("MedicineAliases");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.MedicineId)
            .IsRequired();

        builder.Property(a => a.Alias)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(a => a.LanguageCode)
            .HasMaxLength(10);

        builder.Property(a => a.CreatedAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasIndex(a => new { a.MedicineId, a.Alias })
            .IsUnique()
            .HasDatabaseName("IX_MedicineAliases_MedicineId_Alias");

        builder.HasIndex(a => a.Alias)
            .HasDatabaseName("IX_MedicineAliases_Alias");
    }
}
