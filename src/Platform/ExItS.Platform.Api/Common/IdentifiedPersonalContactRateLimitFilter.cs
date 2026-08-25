using ExItS.Platform.Application.Personal;

namespace ExItS.Platform.Api.Common;

internal sealed class IdentifiedPersonalContactRateLimitFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var body = context.Arguments.OfType<CreatePersonalContactRequest>().FirstOrDefault();
        if (body is not null && RequiresPublicIdRateLimit(body))
        {
            using var lease = await PublicIdResolveRateLimitGate
                .AcquireAsync(context.HttpContext, context.HttpContext.RequestAborted)
                .ConfigureAwait(false);
            if (!lease.IsAcquired)
            {
                return PublicIdResolveRateLimitGate.RejectionResult();
            }
        }

        return await next(context).ConfigureAwait(false);
    }

    private static bool RequiresPublicIdRateLimit(CreatePersonalContactRequest body) =>
        !string.IsNullOrWhiteSpace(body.ResolvedPublicUserId)
        || body.ResolvedUserIdentityId is Guid supplied && supplied != Guid.Empty;
}
