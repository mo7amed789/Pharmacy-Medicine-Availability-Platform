using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using PharmacyAvailability.Domain.Identity;

namespace PharmacyAvailability.Api.Authorization;

public class PharmacyOwnershipRequirement : IAuthorizationRequirement
{
}

public class PharmacyOwnershipHandler : AuthorizationHandler<PharmacyOwnershipRequirement, Guid>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PharmacyOwnershipRequirement requirement,
        Guid targetPharmacyId)
    {
        // 1. Platform Admins have global administrative oversight
        if (context.User.IsInRole(UserRole.PlatformAdmin.ToString()))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // 2. Pharmacy Staff and Pharmacy Admins can ONLY access their own pharmacy
        var isPharmacyUser = context.User.IsInRole(UserRole.PharmacyAdmin.ToString()) ||
                             context.User.IsInRole(UserRole.PharmacyStaff.ToString());

        if (isPharmacyUser)
        {
            var pharmacyIdClaim = context.User.FindFirst("pharmacy_id")?.Value;
            if (Guid.TryParse(pharmacyIdClaim, out var userPharmacyId) && userPharmacyId == targetPharmacyId)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        // 3. Customers and mismatched pharmacies are rejected
        context.Fail();
        return Task.CompletedTask;
    }
}
