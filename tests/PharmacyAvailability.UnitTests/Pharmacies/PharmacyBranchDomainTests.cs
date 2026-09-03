using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Pharmacies;
using Xunit;

namespace PharmacyAvailability.UnitTests.Pharmacies;

public class PharmacyBranchDomainTests
{
    private readonly Coordinates _validCoordinates = new(30.0444, 31.2357);

    [Fact]
    public void CreateBranch_WithValidData_InitializesActiveBranch()
    {
        var pharmacyId = Guid.NewGuid();
        var branch = new PharmacyBranch(
            pharmacyId,
            "Dokki Branch",
            "123 Dokki St",
            "Giza",
            _validCoordinates,
            "+201011112222",
            "24/7",
            "BR-01");

        Assert.Equal(pharmacyId, branch.PharmacyId);
        Assert.Equal("Dokki Branch", branch.Name);
        Assert.Equal("123 Dokki St", branch.Address);
        Assert.Equal("Giza", branch.City);
        Assert.Equal(_validCoordinates.Latitude, branch.Latitude);
        Assert.Equal(_validCoordinates.Longitude, branch.Longitude);
        Assert.Equal("+201011112222", branch.Phone);
        Assert.Equal("24/7", branch.OpeningHours);
        Assert.Equal("BR-01", branch.BranchCode);
        Assert.True(branch.IsActive);
    }

    [Fact]
    public void CreateBranch_EmptyPharmacyId_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() =>
            new PharmacyBranch(Guid.Empty, "Branch", "Address", "City", _validCoordinates, "+2010"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateBranch_EmptyName_ThrowsDomainValidationException(string name)
    {
        Assert.Throws<DomainValidationException>(() =>
            new PharmacyBranch(Guid.NewGuid(), name, "Address", "City", _validCoordinates, "+2010"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateBranch_EmptyAddress_ThrowsDomainValidationException(string address)
    {
        Assert.Throws<DomainValidationException>(() =>
            new PharmacyBranch(Guid.NewGuid(), "Branch", address, "City", _validCoordinates, "+2010"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateBranch_EmptyPhone_ThrowsDomainValidationException(string phone)
    {
        Assert.Throws<DomainValidationException>(() =>
            new PharmacyBranch(Guid.NewGuid(), "Branch", "Address", "City", _validCoordinates, phone));
    }

    [Fact]
    public void Branch_DeactivateAndActivate_TransitionsOperationalStatus()
    {
        var branch = new PharmacyBranch(Guid.NewGuid(), "Branch", "Address", "City", _validCoordinates, "+2010");
        Assert.True(branch.IsActive);

        branch.Deactivate();
        Assert.False(branch.IsActive);

        branch.Activate();
        Assert.True(branch.IsActive);
    }

    [Fact]
    public void Branch_UpdateDetails_ModifiesFieldsCorrectly()
    {
        var branch = new PharmacyBranch(Guid.NewGuid(), "Old Name", "Old Addr", "Old City", _validCoordinates, "010");
        branch.UpdateDetails("New Name", "New Addr", "New City", "011", "08:00-20:00", "B2");

        Assert.Equal("New Name", branch.Name);
        Assert.Equal("New Addr", branch.Address);
        Assert.Equal("New City", branch.City);
        Assert.Equal("011", branch.Phone);
        Assert.Equal("08:00-20:00", branch.OpeningHours);
        Assert.Equal("B2", branch.BranchCode);
    }

    [Fact]
    public void Pharmacy_ApprovalWorkflow_TransitionsCorrectly()
    {
        var pharmacy = new Pharmacy("Chain", "LIC-123", "Addr", "City", _validCoordinates, "010", initialStatus: PharmacyStatus.Pending);
        Assert.Equal(PharmacyStatus.Pending, pharmacy.Status);
        Assert.False(pharmacy.IsActive);

        // Approve
        pharmacy.Approve();
        Assert.Equal(PharmacyStatus.Active, pharmacy.Status);
        Assert.True(pharmacy.IsActive);

        // Suspend
        pharmacy.Suspend();
        Assert.Equal(PharmacyStatus.Suspended, pharmacy.Status);
        Assert.False(pharmacy.IsActive);

        // Reactivate
        pharmacy.Reactivate();
        Assert.Equal(PharmacyStatus.Active, pharmacy.Status);
        Assert.True(pharmacy.IsActive);

        // Deactivate
        pharmacy.Deactivate();
        Assert.Equal(PharmacyStatus.Deactivated, pharmacy.Status);
        Assert.False(pharmacy.IsActive);
    }

    [Fact]
    public void Pharmacy_AddBranch_AddsBranchToCollection()
    {
        var pharmacy = new Pharmacy("Chain", "LIC-999", "Addr", "City", _validCoordinates, "010");
        var branch = pharmacy.AddBranch("Branch 1", "Branch Addr", "Cairo", _validCoordinates, "012");

        Assert.Single(pharmacy.Branches);
        Assert.Equal(branch.Id, pharmacy.Branches.First().Id);
        Assert.Equal(pharmacy.Id, branch.PharmacyId);
    }
}
