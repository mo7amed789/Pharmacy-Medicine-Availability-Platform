using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PharmacyAvailability.Api.Middleware;

namespace PharmacyAvailability.Api.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class FeatureGateAttribute : Attribute, IAsyncAuthorizationFilter
{
    public string FeatureName { get; }

    public FeatureGateAttribute(string featureName)
    {
        FeatureName = featureName;
    }

    public Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var isEnabled = configuration.GetValue<bool>($"FeatureFlags:{FeatureName}", false);

        if (!isEnabled)
        {
            var correlationId = context.HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
                ?? Guid.NewGuid().ToString("D");

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Feature Disabled",
                Detail = $"The requested feature '{FeatureName}' is temporarily disabled. The platform is currently operating in query-only medicine availability mode.",
                Instance = context.HttpContext.Request.Path,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4"
            };

            problemDetails.Extensions["correlationId"] = correlationId;

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
        }

        return Task.CompletedTask;
    }
}
