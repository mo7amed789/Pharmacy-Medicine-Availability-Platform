using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PharmacyAvailability.Domain.Credits;
using PharmacyAvailability.Domain.Identity;

namespace PharmacyAvailability.Infrastructure.Persistence.Configurations;

public class CreditAccountConfiguration : IEntityTypeConfiguration<CreditAccount>
{
    public void Configure(EntityTypeBuilder<CreditAccount> builder)
    {
        builder.ToTable("CreditAccounts", t =>
        {
            t.HasCheckConstraint("CK_CreditAccounts_Balance", "[Balance] >= 0");
            t.HasCheckConstraint("CK_CreditAccounts_Free", "[FreeQueriesRemaining] >= 0");
        });
        builder.HasKey(a => a.Id);
        builder.Property(a => a.PublicCode).IsRequired().HasMaxLength(CreditAccount.CodeLength);
        builder.HasIndex(a => a.PublicCode).IsUnique().HasDatabaseName("IX_CreditAccounts_PublicCode");
        builder.HasIndex(a => a.UserId).IsUnique().HasDatabaseName("IX_CreditAccounts_UserId");
        builder.Property(a => a.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CreditLedgerEntryConfiguration : IEntityTypeConfiguration<CreditLedgerEntry>
{
    public void Configure(EntityTypeBuilder<CreditLedgerEntry> builder)
    {
        builder.ToTable("CreditLedgerEntries");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).IsRequired().HasConversion<int>();
        builder.Property(e => e.TransferReference).HasMaxLength(64);
        builder.Property(e => e.Note).HasMaxLength(500);
        builder.Property(e => e.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.HasIndex(e => e.TransferReference).IsUnique()
            .HasFilter("[TransferReference] IS NOT NULL").HasDatabaseName("IX_CreditLedgerEntries_TransferReference");
        builder.HasIndex(e => new { e.AccountId, e.CreatedAt }).HasDatabaseName("IX_CreditLedgerEntries_Account_Created");
        builder.HasOne<CreditAccount>().WithMany().HasForeignKey(e => e.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class QueryUsageLogConfiguration : IEntityTypeConfiguration<QueryUsageLog>
{
    public void Configure(EntityTypeBuilder<QueryUsageLog> builder)
    {
        builder.ToTable("QueryUsageLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Outcome).IsRequired().HasConversion<int>();
        builder.Property(l => l.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.HasIndex(l => l.CreatedAt).HasDatabaseName("IX_QueryUsageLogs_CreatedAt");
        builder.HasIndex(l => new { l.UserId, l.CreatedAt }).HasDatabaseName("IX_QueryUsageLogs_User_Created");
    }
}
