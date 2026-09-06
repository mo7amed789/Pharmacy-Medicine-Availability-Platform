using PharmacyAvailability.Contracts.Operations;
using Xunit;

namespace PharmacyAvailability.UnitTests.Operations;

public class PharmacyOperationsDtoTests
{
    [Fact]
    public void StaffReservationQueueItemDto_CalculatesTotalDueCorrectly()
    {
        var item = new StaffReservationQueueItemDto(
            ReservationId: Guid.NewGuid(),
            CustomerId: Guid.NewGuid(),
            CustomerName: "Alice Smith",
            CustomerPhone: "+201001234567",
            CustomerEmail: "alice@test.com",
            MedicineId: Guid.NewGuid(),
            MedicineBrandName: "Panadol Extra",
            MedicineGenericName: "Paracetamol",
            DosageForm: "Tablet",
            Strength: "500mg",
            Quantity: 4,
            UnitPrice: 25.50m,
            TotalAmountDue: 4 * 25.50m,
            Status: "Active",
            CreatedAt: DateTime.UtcNow,
            ExpiresAt: DateTime.UtcNow.AddMinutes(25),
            RemainingMinutes: 25,
            IsExpiringSoon: true);

        Assert.Equal(102.00m, item.TotalAmountDue);
        Assert.Equal(25, item.RemainingMinutes);
        Assert.True(item.IsExpiringSoon);
    }

    [Theory]
    [InlineData(15, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    [InlineData(60, false)]
    [InlineData(0, false)]
    public void StaffReservationQueueItemDto_ExpiringSoonThresholdEvaluation(int remainingMinutes, bool expectedIsExpiringSoon)
    {
        var isExpiringSoon = remainingMinutes <= 30 && remainingMinutes > 0;
        Assert.Equal(expectedIsExpiringSoon, isExpiringSoon);
    }

    [Theory]
    [InlineData("Active", 10, true, 10)]
    [InlineData("Active", 30, true, 30)]
    [InlineData("Active", 31, false, 31)]
    [InlineData("Active", -5, false, 0)]
    [InlineData("Confirmed", 10, false, 0)]
    [InlineData("Cancelled", 10, false, 0)]
    [InlineData("Expired", 10, false, 0)]
    [InlineData("Released", 10, false, 0)]
    public void StaffReservationQueueItemDto_StrictUrgencyAndRemainingMinutesRules(
        string status, int minutesOffset, bool expectedExpiringSoon, int expectedRemainingMinutes)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(minutesOffset);

        var isActive = status.Equals("Active", StringComparison.OrdinalIgnoreCase);
        var remainingMinutes = (isActive && expiresAt > now)
            ? Math.Max(0, (int)Math.Ceiling((expiresAt - now).TotalMinutes))
            : 0;

        var isExpiringSoon = isActive &&
                             expiresAt > now &&
                             expiresAt <= now.AddMinutes(30);

        Assert.Equal(expectedExpiringSoon, isExpiringSoon);
        Assert.Equal(expectedRemainingMinutes, remainingMinutes);
    }

    [Fact]
    public void LowStockAlertDto_CalculatesTotalPhysicalStockCorrectly()
    {
        var dto = new LowStockAlertDto(
            InventoryId: Guid.NewGuid(),
            MedicineId: Guid.NewGuid(),
            MedicineBrandName: "Augmentin",
            MedicineGenericName: "Amoxicillin",
            CategoryName: "Antibiotics",
            ManufacturerName: "GSK",
            AvailableQuantity: 3,
            ReservedQuantity: 4,
            TotalPhysicalStock: 3 + 4,
            Price: 85.00m,
            Threshold: 5,
            LastUpdated: DateTime.UtcNow);

        Assert.Equal(7, dto.TotalPhysicalStock);
        Assert.Equal(3, dto.AvailableQuantity);
        Assert.Equal(4, dto.ReservedQuantity);
        Assert.True(dto.AvailableQuantity <= dto.Threshold);
    }
}
