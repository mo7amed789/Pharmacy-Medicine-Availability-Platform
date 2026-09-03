using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Reservations;
using Xunit;

namespace PharmacyAvailability.UnitTests.Reservations;

public class ReservationDomainTests
{
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _pharmacyId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _medicineId = Guid.NewGuid();

    [Fact]
    public void CreateReservation_WithValidData_InitializesAsActive()
    {
        var expiresAt = DateTime.UtcNow.AddHours(2);
        var reservation = new Reservation(
            _customerId,
            _pharmacyId,
            _branchId,
            _medicineId,
            3,
            expiresAt);

        Assert.NotEqual(Guid.Empty, reservation.Id);
        Assert.Equal(_customerId, reservation.CustomerId);
        Assert.Equal(_pharmacyId, reservation.PharmacyId);
        Assert.Equal(_branchId, reservation.BranchId);
        Assert.Equal(_medicineId, reservation.MedicineId);
        Assert.Equal(3, reservation.Quantity);
        Assert.Equal(ReservationStatus.Active, reservation.Status);
        Assert.Equal(expiresAt, reservation.ExpiresAt);
        Assert.Null(reservation.ConfirmedAt);
        Assert.Null(reservation.ReleasedAt);
        Assert.Null(reservation.CancelledAt);
    }

    [Fact]
    public void CreateReservation_EmptyCustomerId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Reservation(Guid.Empty, _pharmacyId, _branchId, _medicineId, 1, DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public void CreateReservation_NonPositiveQuantity_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Reservation(_customerId, _pharmacyId, _branchId, _medicineId, 0, DateTime.UtcNow.AddHours(1)));

        Assert.Throws<DomainValidationException>(() =>
            new Reservation(_customerId, _pharmacyId, _branchId, _medicineId, -2, DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public void CreateReservation_PastExpirationTime_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new Reservation(_customerId, _pharmacyId, _branchId, _medicineId, 1, DateTime.UtcNow.AddMinutes(-10)));
    }

    [Fact]
    public void Confirm_FromActiveStatus_SucceedsAndSetsTimestamp()
    {
        var reservation = new Reservation(_customerId, _pharmacyId, _branchId, _medicineId, 2, DateTime.UtcNow.AddHours(2));

        reservation.Confirm();

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
        Assert.NotNull(reservation.ConfirmedAt);
    }

    [Fact]
    public void Cancel_FromActiveStatus_SucceedsAndSetsTimestamp()
    {
        var reservation = new Reservation(_customerId, _pharmacyId, _branchId, _medicineId, 2, DateTime.UtcNow.AddHours(2));

        reservation.Cancel();

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.NotNull(reservation.CancelledAt);
    }

    [Fact]
    public void Expire_FromActiveStatus_SucceedsAndSetsTimestamp()
    {
        var reservation = new Reservation(_customerId, _pharmacyId, _branchId, _medicineId, 2, DateTime.UtcNow.AddHours(2));

        reservation.Expire();

        Assert.Equal(ReservationStatus.Expired, reservation.Status);
        Assert.NotNull(reservation.ReleasedAt);
    }

    [Fact]
    public void Release_FromActiveStatus_SucceedsAndSetsTimestamp()
    {
        var reservation = new Reservation(_customerId, _pharmacyId, _branchId, _medicineId, 2, DateTime.UtcNow.AddHours(2));

        reservation.Release();

        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.NotNull(reservation.ReleasedAt);
    }

    [Fact]
    public void InvalidTransition_FromConfirmed_ThrowsResourceConflictException()
    {
        var reservation = new Reservation(_customerId, _pharmacyId, _branchId, _medicineId, 2, DateTime.UtcNow.AddHours(2));
        reservation.Confirm();

        Assert.Throws<ResourceConflictException>(() => reservation.Cancel());
        Assert.Throws<ResourceConflictException>(() => reservation.Confirm());
        Assert.Throws<ResourceConflictException>(() => reservation.Expire());
        Assert.Throws<ResourceConflictException>(() => reservation.Release());
    }

    [Fact]
    public void InvalidTransition_FromCancelled_ThrowsResourceConflictException()
    {
        var reservation = new Reservation(_customerId, _pharmacyId, _branchId, _medicineId, 2, DateTime.UtcNow.AddHours(2));
        reservation.Cancel();

        Assert.Throws<ResourceConflictException>(() => reservation.Confirm());
        Assert.Throws<ResourceConflictException>(() => reservation.Cancel());
        Assert.Throws<ResourceConflictException>(() => reservation.Expire());
        Assert.Throws<ResourceConflictException>(() => reservation.Release());
    }

    [Fact]
    public void InvalidTransition_FromExpired_ThrowsResourceConflictException()
    {
        var reservation = new Reservation(_customerId, _pharmacyId, _branchId, _medicineId, 2, DateTime.UtcNow.AddHours(2));
        reservation.Expire();

        Assert.Throws<ResourceConflictException>(() => reservation.Confirm());
        Assert.Throws<ResourceConflictException>(() => reservation.Cancel());
        Assert.Throws<ResourceConflictException>(() => reservation.Expire());
        Assert.Throws<ResourceConflictException>(() => reservation.Release());
    }
}
