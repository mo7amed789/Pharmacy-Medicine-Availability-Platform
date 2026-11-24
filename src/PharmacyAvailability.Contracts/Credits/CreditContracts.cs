namespace PharmacyAvailability.Contracts.Credits;

public record MyCreditDto(
    string PublicCode,
    long Balance,
    int FreeQueriesRemaining,
    bool ChargingEnabled);

public record CreditTopUpRequest(
    string PublicCode,
    long Points,
    string TransferReference,
    string? Note);

public record CreditLedgerEntryDto(
    Guid Id,
    string Type,
    long Points,
    long BalanceAfter,
    string? TransferReference,
    Guid? PerformedByUserId,
    string? Note,
    DateTime CreatedAt);

public record AdminCreditAccountDto(
    string PublicCode,
    Guid UserId,
    string FullName,
    string PhoneNumber,
    bool IsPhoneVerified,
    long Balance,
    int FreeQueriesRemaining,
    IReadOnlyList<CreditLedgerEntryDto> RecentEntries);

public record UsageSummaryDto(
    DateTime FromUtc,
    DateTime ToUtc,
    int TotalQueries,
    int SuccessQueries,
    int UnknownQueries,
    int NoResultQueries,
    int AnonymousQueries,
    long PointsCharged,
    int FreeQueriesUsed,
    int DistinctUsers);
