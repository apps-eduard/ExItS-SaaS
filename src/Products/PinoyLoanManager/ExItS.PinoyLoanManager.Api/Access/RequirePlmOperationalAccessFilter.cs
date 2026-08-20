using ExItS.PinoyLoanManager.Application.Access;

namespace ExItS.PinoyLoanManager.Api.Access;

internal static class PlmAccessHttpContextKeys
{
    public const string Context = "plm.access.context";
}

internal sealed class RequirePlmOperationalAccessFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var guard = context.HttpContext.RequestServices.GetRequiredService<IPlmOperationalAccessGuard>();
        var decision = await guard.EvaluateAsync(context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!decision.IsAllowed)
        {
            return PlmApiResults.FromDenial(decision);
        }

        context.HttpContext.Items[PlmAccessHttpContextKeys.Context] = decision.Context;
        return await next(context).ConfigureAwait(false);
    }
}

public static class PlmOperationalAccessEndpointExtensions
{
    /// <summary>
    /// Applies the fail-closed PLM operational access guard to a future operational endpoint.
    /// </summary>
    public static RouteHandlerBuilder RequirePlmOperationalAccess(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEndpointFilter<RequirePlmOperationalAccessFilter>();
    }
}
