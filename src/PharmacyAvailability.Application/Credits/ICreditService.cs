using PharmacyAvailability.Contracts.Availability;
using PharmacyAvailability.Contracts.Credits;

namespace PharmacyAvailability.Application.Credits;

/// <summary>Result of a metered availability search, for the controller to expose.</summary>
public record MeteredSearchOutcome(AvailabilitySearchResult Result, int PointsCharged);

public interface ICreditService
{
    Task<MyCreditDto> GetMyAccountAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Runs the search through the metering rules: checks access and cap first, logs the query, charges only a successful one.</summary>
    Task<MeteredSearchOutcome> SearchMeteredAsync(
        Guid? userId,
        Func<CancellationToken, Task<AvailabilitySearchResult>> search,
        CancellationToken ct = default);

    Task<AdminCreditAccountDto> LookupAccountAsync(string publicCode, CancellationToken ct = default);
    Task<AdminCreditAccountDto> TopUpAsync(Guid adminUserId, CreditTopUpRequest request, CancellationToken ct = default);
    Task<UsageSummaryDto> GetUsageSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);
}
