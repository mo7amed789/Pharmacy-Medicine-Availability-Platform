using PharmacyAvailability.Contracts.Catalog;
using PharmacyAvailability.Contracts.Reservations;
using PharmacyAvailability.Domain.Identity;

namespace PharmacyAvailability.Application.Reservations;

public interface IReservationService
{
    Task<ReservationDto> CreateReservationAsync(
        CreateReservationRequest request,
        Guid customerId,
        CancellationToken ct = default);

    Task<ReservationDto> CancelReservationAsync(
        Guid reservationId,
        Guid customerId,
        CancellationToken ct = default);

    Task<ReservationDto> ConfirmReservationAsync(
        Guid reservationId,
        Guid staffUserId,
        Guid? staffPharmacyId,
        bool isPlatformAdmin,
        CancellationToken ct = default);

    Task<PagedResult<ReservationDto>> GetReservationsAsync(
        GetReservationsQuery query,
        Guid userId,
        UserRole role,
        Guid? userPharmacyId,
        CancellationToken ct = default);

    Task<ReservationDto?> GetReservationByIdAsync(
        Guid reservationId,
        Guid userId,
        UserRole role,
        Guid? userPharmacyId,
        CancellationToken ct = default);

    Task<int> ExpireOverdueReservationsAsync(
        CancellationToken ct = default);
}
