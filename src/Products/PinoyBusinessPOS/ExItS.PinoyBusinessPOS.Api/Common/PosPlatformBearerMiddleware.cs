using ExItS.PinoyBusinessPOS.Application.Commercial;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Introspects Authorization Bearer tokens against Platform and stores identity/commercial
/// claims in HttpContext.Items for organization scope and commercial middleware.
/// </summary>
internal sealed class PosPlatformBearerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IPlatformTokenIntrospectionClient introspection,
        IPosCommercialAccessAccessor commercialAccess)
    {
        var bearer = ExtractBearer(context.Request);
        if (string.IsNullOrWhiteSpace(bearer))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        PlatformTokenIntrospectionResult result;
        try
        {
            result = await introspection.IntrospectAsync(bearer, context.RequestAborted).ConfigureAwait(false);
        }
        catch
        {
            context.Items[PosAuthItems.Denied] = true;
            await next(context).ConfigureAwait(false);
            return;
        }

        if (!result.Active || result.UserId is not Guid userId || userId == Guid.Empty)
        {
            context.Items[PosAuthItems.Denied] = true;
            await next(context).ConfigureAwait(false);
            return;
        }

        context.Items[PosAuthItems.UserId] = userId;
        if (result.OrganizationId is Guid orgId && orgId != Guid.Empty)
        {
            context.Items[PosAuthItems.OrganizationId] = orgId;
        }

        var productAllowed = result.ProductAccessAllowed == true;
        context.Items[PosAuthItems.ProductAccessAllowed] = productAllowed;
        if (!string.IsNullOrWhiteSpace(result.SubscriptionStatus))
        {
            context.Items[PosAuthItems.SubscriptionStatus] = result.SubscriptionStatus;
        }

        if (result.EnabledFeatureCodes is { Count: > 0 })
        {
            context.Items[PosAuthItems.EnabledFeatureCodes] = result.EnabledFeatureCodes;
        }

        if (!string.IsNullOrWhiteSpace(result.ProductLocalRoleCode))
        {
            context.Items[PosAuthItems.ProductLocalRoleCode] = result.ProductLocalRoleCode;
        }

        if (!string.IsNullOrWhiteSpace(result.MappedPosRoleCode))
        {
            context.Items[PosAuthItems.MappedPosRoleCode] = result.MappedPosRoleCode;
        }

        if (productAllowed && result.OrganizationId is Guid boundOrg && boundOrg != Guid.Empty)
        {
            var grants = result.EnabledFeatureCodes ?? Array.Empty<string>();
            commercialAccess.Current = new PosCommercialAccess(
                result.SubscriptionStatus,
                grants,
                IsKnown: true);
            context.Items[PosAuthItems.CommercialBound] = true;
        }

        await next(context).ConfigureAwait(false);
    }

    private static string? ExtractBearer(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out var values))
        {
            return null;
        }

        var header = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header)
            || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header["Bearer ".Length..].Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }
}
