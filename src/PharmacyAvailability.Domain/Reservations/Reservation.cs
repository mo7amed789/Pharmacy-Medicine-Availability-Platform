using PharmacyAvailability.Domain.Common;
using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Catalog;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Domain.Pharmacies;

namespace PharmacyAvailability.Domain.Reservations;

public class Reservation : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public Guid PharmacyId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid MedicineId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? ExpiredAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    // Navigations
    public virtual User? Customer { get; private set; }
    public virtual Pharmacy? Pharmacy { get; private set; }
    public virtual PharmacyBranch? Branch { get; private set; }
    public virtual Medicine? Medicine { get; private set; }

    // EF Core Constructor
    private Reservation() { }

    public Reservation(
        Guid customerId,
        Guid pharmacyId,
        Guid branchId,
        Guid medicineId,
        int quantity,
        DateTime expiresAt)
    {
        if (customerId == Guid.Empty)
            throw new DomainValidationException("CustomerId cannot be empty.");
        if (pharmacyId == Guid.Empty)
            throw new DomainValidationException("PharmacyId cannot be empty.");
        if (branchId == Guid.Empty)
            throw new DomainValidationException("BranchId cannot be empty.");
        if (medicineId == Guid.Empty)
            throw new DomainValidationException("MedicineId cannot be empty.");
        if (quantity <= 0)
            throw new DomainValidationException("Reservation quantity must be greater than zero.");

        var now = DateTime.UtcNow;
        if (expiresAt <= now)
            throw new DomainValidationException("Expiration time must be in the future.");

        Id = Guid.NewGuid();
        CustomerId = customerId;
        PharmacyId = pharmacyId;
        BranchId = branchId;
        MedicineId = medicineId;
        Quantity = quantity;
        Status = ReservationStatus.Active;
        CreatedAt = now;
        ExpiresAt = expiresAt;
    }

    public void Confirm()
    {
        if (Status != ReservationStatus.Active)
            throw new ResourceConflictException($"Cannot confirm reservation '{Id}' with status '{Status}'. Only active reservations can be confirmed.");

        Status = ReservationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != ReservationStatus.Active)
            throw new ResourceConflictException($"Cannot cancel reservation '{Id}' with status '{Status}'. Only active reservations can be cancelled.");

        Status = ReservationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    public void Expire()
    {
        if (Status != ReservationStatus.Active)
            throw new ResourceConflictException($"Cannot expire reservation '{Id}' with status '{Status}'. Only active reservations can be expired.");

        Status = ReservationStatus.Expired;
        ExpiredAt = DateTime.UtcNow;
    }

    public void Release()
    {
        if (Status != ReservationStatus.Active)
            throw new ResourceConflictException($"Cannot release reservation '{Id}' with status '{Status}'. Only active reservations can be released.");

        Status = ReservationStatus.Released;
        ReleasedAt = DateTime.UtcNow;
    }
}
