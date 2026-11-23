using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Domain.Identity;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Api.Authorization;

// Signature validation is necessary but not enough after account deactivation.
public static class CurrentUserTokenValidator
{
    public static async Task ValidateAsync(TokenValidatedContext context)
    {
        var principal = context.Principal;
        if (!Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            context.Fail("Invalid user identity.");
            return;
        }
        var db = context.HttpContext.RequestServices.GetRequiredService<PharmacyDbContext>();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id,
            context.HttpContext.RequestAborted);
        if (!Matches(user, principal!)) context.Fail("Account or permissions changed. Sign in again.");
    }

    public static bool Matches(User? user, ClaimsPrincipal principal)
    {
        if (user is null || !user.IsActive) return false;
        if (principal.FindFirstValue(ClaimTypes.NameIdentifier) != user.Id.ToString()) return false;
        var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        if (roles.Length != 1 || roles[0] != user.Role.ToString()) return false;
        var pharmacyClaims = principal.FindAll("pharmacy_id").Select(c => c.Value).ToArray();
        if (!user.PharmacyId.HasValue) return pharmacyClaims.Length == 0;
        return pharmacyClaims.Length == 1 && Guid.TryParse(pharmacyClaims[0], out var pharmacyId)
            && pharmacyId == user.PharmacyId.Value;
    }
}
