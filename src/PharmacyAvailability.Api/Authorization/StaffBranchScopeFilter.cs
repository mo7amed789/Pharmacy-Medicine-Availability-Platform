using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PharmacyAvailability.Infrastructure.Persistence;

namespace PharmacyAvailability.Api.Authorization;

public sealed class StaffBranchScopeFilter(PharmacyDbContext db) : IAsyncActionFilter, IOrderedFilter
{
    public int Order => -3000;
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.IsInRole("PharmacyStaff") && context.RouteData.Values.TryGetValue("branchId", out var value))
        {
            var ct = context.HttpContext.RequestAborted;
            if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ||
                !Guid.TryParse(value?.ToString(), out var branchId) ||
                !await db.Users.AsNoTracking().AnyAsync(u => u.Id == id && u.IsActive &&
                    u.AssignedBranchId == branchId && db.PharmacyBranches.Any(b => b.Id == branchId && b.IsActive && b.PharmacyId == u.PharmacyId), ct))
            {
                context.Result = new ForbidResult(); return;
            }
        }
        await next();
    }
}
