using ExItS.PinoyBuyNowPayLater.Application.Access;

namespace ExItS.PinoyBuyNowPayLater.Api.Access;

internal static class BnplAccessHttpContextKeys
{
    public const string Context = "bnpl.access.context";
    public const string Requirement = "bnpl.access.requirement";
}

internal sealed class RequireBnplOperationalAccessFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var requirement = context.HttpContext.Items.TryGetValue(BnplAccessHttpContextKeys.Requirement, out var raw)
            && raw is BnplAccessRequirement typed
            ? typed
            : BnplAccessRequirement.None;

        var guard = context.HttpContext.RequestServices.GetRequiredService<IBnplOperationalAccessGuard>();
        var decision = await guard.EvaluateAsync(requirement, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!decision.IsAllowed)
        {
            return BnplApiResults.FromDenial(decision);
        }

        context.HttpContext.Items[BnplAccessHttpContextKeys.Context] = decision.Context;
        return await next(context).ConfigureAwait(false);
    }
}

public static class BnplOperationalAccessEndpointExtensions
{
    public static RouteHandlerBuilder RequireBnplOperationalAccess(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEndpointFilter<RequireBnplOperationalAccessFilter>();
    }

    public static RouteHandlerBuilder RequireBnplOperationalAccess(
        this RouteHandlerBuilder builder,
        BnplAccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(requirement);
        return builder
            .AddEndpointFilter(async (context, next) =>
            {
                context.HttpContext.Items[BnplAccessHttpContextKeys.Requirement] = requirement;
                return await next(context).ConfigureAwait(false);
            })
            .AddEndpointFilter<RequireBnplOperationalAccessFilter>();
    }
}
