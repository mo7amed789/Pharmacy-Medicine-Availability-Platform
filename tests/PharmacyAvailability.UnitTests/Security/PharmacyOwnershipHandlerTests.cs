using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PharmacyAvailability.Api.Authorization;
using PharmacyAvailability.Domain.Identity;
using Xunit;

namespace PharmacyAvailability.UnitTests.Security;

public class PharmacyOwnershipHandlerTests
{
    private readonly PharmacyOwnershipHandler _handler = new();
    private readonly PharmacyOwnershipRequirement _requirement = new();

    [Fact]
    public async Task PlatformAdmin_CanAccessAnyPharmacy_Succeeds()
    {
        var targetPharmacyId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRole.PlatformAdmin.ToString())
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, targetPharmacyId);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PharmacyStaff_MatchingPharmacyId_Succeeds()
    {
        var pharmacyId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRole.PharmacyStaff.ToString()),
            new Claim("pharmacy_id", pharmacyId.ToString())
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, pharmacyId);

        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PharmacyStaff_DifferentPharmacyId_Fails()
    {
        var myPharmacyId = Guid.NewGuid();
        var otherPharmacyId = Guid.NewGuid();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRole.PharmacyStaff.ToString()),
            new Claim("pharmacy_id", myPharmacyId.ToString())
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, otherPharmacyId);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task PharmacyAdmin_DifferentPharmacyId_Fails()
    {
        var myPharmacyId = Guid.NewGuid();
        var targetPharmacyId = Guid.NewGuid();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRole.PharmacyAdmin.ToString()),
            new Claim("pharmacy_id", myPharmacyId.ToString())
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, targetPharmacyId);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Customer_AttemptingPharmacyAccess_Fails()
    {
        var targetPharmacyId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, UserRole.Customer.ToString())
        }, "TestAuth"));

        var context = new AuthorizationHandlerContext(new[] { _requirement }, user, targetPharmacyId);

        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
