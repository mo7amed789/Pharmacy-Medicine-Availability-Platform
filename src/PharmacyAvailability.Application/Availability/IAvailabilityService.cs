using PharmacyAvailability.Contracts.Availability;

namespace PharmacyAvailability.Application.Availability;

public interface IAvailabilityService
{
    Task<AvailabilitySearchResult> SearchAvailabilityAsync(
        AvailabilitySearchRequest request,
        CancellationToken ct = default);
}
