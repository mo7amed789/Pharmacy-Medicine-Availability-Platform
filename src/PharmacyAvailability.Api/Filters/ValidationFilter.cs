using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PharmacyAvailability.Api.Middleware;

namespace PharmacyAvailability.Api.Filters;

public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument == null) continue;

            var argumentType = argument.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argumentType);
            var validator = _serviceProvider.GetService(validatorType) as IValidator;

            if (validator != null)
            {
                var validationContext = new ValidationContext<object>(argument);
                var validationResult = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                {
                    var correlationId = context.HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
                        ?? Guid.NewGuid().ToString("D");

                    var problemDetails = new HttpValidationProblemDetails
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "One or more validation errors occurred.",
                        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                        Detail = "Please refer to the errors property for additional details.",
                        Instance = context.HttpContext.Request.Path
                    };

                    problemDetails.Extensions["correlationId"] = correlationId;

                    foreach (var failure in validationResult.Errors)
                    {
                        if (problemDetails.Errors.TryGetValue(failure.PropertyName, out var existingErrors))
                        {
                            problemDetails.Errors[failure.PropertyName] = existingErrors.Concat(new[] { failure.ErrorMessage }).ToArray();
                        }
                        else
                        {
                            problemDetails.Errors[failure.PropertyName] = new[] { failure.ErrorMessage };
                        }
                    }

                    context.Result = new BadRequestObjectResult(problemDetails);
                    return;
                }
            }
        }

        await next();
    }
}
