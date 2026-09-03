using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Infrastructure.Security;
using Xunit;

namespace PharmacyAvailability.UnitTests.Security;

public class PasswordHashServiceTests
{
    private readonly PasswordHashService _service = new();

    [Fact]
    public void HashPassword_ProducesNonEmptySecureHashDifferentFromPlaintext()
    {
        var user = new User("user@example.com", "placeholder", "John", UserRole.Customer);
        var password = "SuperSecretPassword123!";

        var hash = _service.HashPassword(user, password);

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual(password, hash);
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var user = new User("user@example.com", "placeholder", "John", UserRole.Customer);
        var password = "SuperSecretPassword123!";
        var hash = _service.HashPassword(user, password);

        var result = _service.VerifyPassword(user, password, hash);

        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var user = new User("user@example.com", "placeholder", "John", UserRole.Customer);
        var password = "SuperSecretPassword123!";
        var hash = _service.HashPassword(user, password);

        var result = _service.VerifyPassword(user, "WrongPassword!", hash);

        Assert.False(result);
    }
}
