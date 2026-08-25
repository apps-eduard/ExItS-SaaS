namespace ExItS.Platform.Api.Common;

internal sealed class PublicIdResolveRateLimitFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        using var lease = await PublicIdResolveRateLimitGate
            .AcquireAsync(context.HttpContext, context.HttpContext.RequestAborted)
            .ConfigureAwait(false);
        if (!lease.IsAcquired)
        {
            return PublicIdResolveRateLimitGate.RejectionResult();
        }

        return await next(context).ConfigureAwait(false);
    }
}
