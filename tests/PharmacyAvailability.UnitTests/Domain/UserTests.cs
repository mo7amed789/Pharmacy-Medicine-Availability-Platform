using PharmacyAvailability.Domain.Common.Exceptions;
using PharmacyAvailability.Domain.Identity;
using Xunit;

namespace PharmacyAvailability.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void CreateCustomer_WithoutPharmacyId_Succeeds()
    {
        // Arrange & Act
        var user = new User("patient@example.com", "hashed_pwd", "Ahmed Ali", UserRole.Customer);

        // Assert
        Assert.Equal("patient@example.com", user.Email);
        Assert.Equal(UserRole.Customer, user.Role);
        Assert.Null(user.PharmacyId);
        Assert.True(user.IsActive);
    }

    [Fact]
    public void CreatePharmacyStaff_WithoutPharmacyId_ThrowsDomainValidationException()
    {
        // Arrange & Act & Assert
        Assert.Throws<DomainValidationException>(() =>
            new User("pharmacist@example.com", "hashed_pwd", "Mona Hassan", UserRole.PharmacyStaff));
    }

    [Fact]
    public void CreatePharmacyStaff_WithPharmacyId_Succeeds()
    {
        // Arrange
        var pharmacyId = Guid.NewGuid();

        // Act
        var user = new User("pharmacist@example.com", "hashed_pwd", "Mona Hassan", UserRole.PharmacyStaff, "+201100000000", pharmacyId);

        // Assert
        Assert.Equal(pharmacyId, user.PharmacyId);
        Assert.Equal(UserRole.PharmacyStaff, user.Role);
    }

    [Fact]
    public void AddRefreshToken_AddsActiveToken()
    {
        // Arrange
        var user = new User("user@test.com", "pwd", "John", UserRole.Customer);
        var expiry = DateTime.UtcNow.AddDays(7);

        // Act
        var token = user.AddRefreshToken("hash123", expiry);

        // Assert
        Assert.Single(user.RefreshTokens);
        Assert.True(token.IsActive);
        Assert.False(token.IsExpired);
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void RevokeAllRefreshTokens_RevokesAllActiveTokens()
    {
        // Arrange
        var user = new User("user@test.com", "pwd", "John", UserRole.Customer);
        var t1 = user.AddRefreshToken("h1", DateTime.UtcNow.AddDays(1));
        var t2 = user.AddRefreshToken("h2", DateTime.UtcNow.AddDays(2));

        // Act
        user.RevokeAllRefreshTokens();

        // Assert
        Assert.False(t1.IsActive);
        Assert.True(t1.IsRevoked);
        Assert.False(t2.IsActive);
        Assert.True(t2.IsRevoked);
    }
}
