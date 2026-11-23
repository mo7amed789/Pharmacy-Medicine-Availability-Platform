using System.Security.Claims;
using PharmacyAvailability.Api.Authorization;
using PharmacyAvailability.Domain.Identity;

namespace PharmacyAvailability.UnitTests.Security;

public class CurrentUserTokenValidatorTests
{
    private static ClaimsPrincipal Claims(User u, string? role = null, Guid? pharmacy = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Role, role ?? u.Role.ToString()) };
        if (pharmacy.HasValue) claims.Add(new("pharmacy_id", pharmacy.Value.ToString()));
        return new(new ClaimsIdentity(claims, "test"));
    }

    [Fact]
    public void ActiveMatchingUserAccepted_DeactivatedOrDeletedRejected()
    {
        var u = new User("synthetic@test.invalid", "fake-hash", "Test", UserRole.Customer, "123");
        var claims = Claims(u);
        Assert.True(CurrentUserTokenValidator.Matches(u, claims));
        u.Deactivate();
        Assert.False(CurrentUserTokenValidator.Matches(u, claims));
        Assert.False(CurrentUserTokenValidator.Matches(null, claims));
    }

    [Fact]
    public void StaleRoleOrPharmacyRejected()
    {
        var id = Guid.NewGuid();
        var u = new User("synthetic@test.invalid", "fake-hash", "Test", UserRole.PharmacyStaff, "123", id);
        Assert.True(CurrentUserTokenValidator.Matches(u, Claims(u, pharmacy:id)));
        Assert.False(CurrentUserTokenValidator.Matches(u, Claims(u, role:"PlatformAdmin", pharmacy:id)));
        Assert.False(CurrentUserTokenValidator.Matches(u, Claims(u, pharmacy:Guid.NewGuid())));
        Assert.False(CurrentUserTokenValidator.Matches(u, Claims(u)));
    }
}
