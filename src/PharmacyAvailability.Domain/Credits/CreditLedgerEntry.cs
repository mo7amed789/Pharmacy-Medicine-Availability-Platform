using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;

namespace PharmacyAvailability.Domain.Credits;

public enum CreditEntryType
{
    TopUp = 1,
    QueryCharge = 2
}

/// <summary>Append-only record of every balance change. Never updated or deleted (a database trigger enforces this).</summary>
public class CreditLedgerEntry : BaseEntity
{
    public Guid AccountId { get; private set; }
    public CreditEntryType Type { get; private set; }
    /// <summary>Positive for top ups, negative for charges.</summary>
    public long Points { get; private set; }
    public long BalanceAfter { get; private set; }
    /// <summary>Operator-entered transfer number. Unique when present, so one transfer cannot be credited twice.</summary>
    public string? TransferReference { get; private set; }
    public Guid? PerformedByUserId { get; private set; }
    public string? Note { get; private set; }

    protected CreditLedgerEntry() { }

    public CreditLedgerEntry(Guid accountId, CreditEntryType type, long points, long balanceAfter,
        string? transferReference, Guid? performedByUserId, string? note)
    {
        if (points == 0) throw new DomainValidationException("Points cannot be zero.");
        if (type == CreditEntryType.TopUp && points < 0) throw new DomainValidationException("A top up must be positive.");
        if (type == CreditEntryType.QueryCharge && points > 0) throw new DomainValidationException("A charge must be negative.");
        AccountId = accountId;
        Type = type;
        Points = points;
        BalanceAfter = balanceAfter;
        TransferReference = transferReference;
        PerformedByUserId = performedByUserId;
        Note = note;
    }
}
