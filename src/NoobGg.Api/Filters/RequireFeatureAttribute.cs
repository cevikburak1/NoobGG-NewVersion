using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NoobGg.Application.Common.Interfaces;

namespace NoobGg.Api.Filters;

/// <summary>
/// Action filter that gates access to endpoints behind premium features.
/// Usage: [RequireFeature(PremiumFeature.AdvancedFilters)]
/// Returns 403 if the user's plan doesn't include the feature.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class RequireFeatureAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _featureKey;

    public RequireFeatureAttribute(string featureKey)
    {
        _featureKey = featureKey;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                type = "Error",
                title = "Authentication required",
                status = 401
            });
            return;
        }

        var userId = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var entitlementService = context.HttpContext.RequestServices.GetRequiredService<IEntitlementService>();
        var hasFeature = await entitlementService.HasFeatureAsync(userId, _featureKey);

        if (!hasFeature)
        {
            context.Result = new ObjectResult(new
            {
                type = "Error",
                title = "This feature requires a premium subscription",
                status = 403,
                requiredFeature = _featureKey
            })
            {
                StatusCode = 403
            };
            return;
        }

        await next();
    }
}
