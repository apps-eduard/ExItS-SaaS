using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Development-stage commercial entitlement headers for POS APIs.
/// Not production authentication. Missing headers default to Active + full grants only in
/// Development/Testing so local and integration tests continue; Production fails closed.
/// </summary>
public static class PosCommercialHeaders
{
    public const string SubscriptionStatusHeaderName = "X-Pos-Subscription-Status";
    public const string FeatureGrantsHeaderName = "X-Pos-Feature-Grants";
}

internal static class PosCommercialScope
{
    public static void BindFromRequest(
        HttpRequest request,
        IPosCommercialAccessAccessor accessor,
        IHostEnvironment environment)
    {
        var isDevLike = PosDevelopmentEnvironment.IsApprovedDevelopmentEnvironment(environment);

        // Outside Development/Testing, commercial headers are ignored and access fails closed.
        // Not production authentication — Platform-backed commercial evaluation is required later.
        if (!isDevLike)
        {
            accessor.Current = PosCommercialAccess.Unknown;
            return;
        }

        var hasStatus = request.Headers.TryGetValue(PosCommercialHeaders.SubscriptionStatusHeaderName, out var statusValues)
                        && !string.IsNullOrWhiteSpace(statusValues.FirstOrDefault());
        var hasGrants = request.Headers.TryGetValue(PosCommercialHeaders.FeatureGrantsHeaderName, out var grantValues)
                        && !string.IsNullOrWhiteSpace(grantValues.FirstOrDefault());

        if (!hasStatus && !hasGrants)
        {
            accessor.Current = PosCommercialAccess.DevelopmentDefault;
            return;
        }

        var status = hasStatus ? statusValues.First()!.Trim() : null;
        IReadOnlyList<string> grants = [];
        if (hasGrants)
        {
            grants = grantValues.First()!
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        // Explicit empty grants header means no features (fail closed for capabilities).
        accessor.Current = new PosCommercialAccess(status, grants, IsKnown: true);
    }

    public static bool TryAuthorize(
        IPosCommercialAccessAccessor accessor,
        UtangCapability capability,
        out IResult? problem)
    {
        problem = null;
        var gate = CommercialAccessGuard.Require(accessor, capability);
        if (gate.IsSuccess)
        {
            return true;
        }

        problem = PosApiResults.Problem(
            gate.ErrorCode!,
            gate.ErrorMessage!,
            PosApiResults.MapStatusCode(gate.ErrorCode!));
        return false;
    }
}

/// <summary>Binds commercial access headers into the scoped accessor for each request.</summary>
internal sealed class PosCommercialAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IPosCommercialAccessAccessor accessor,
        IHostEnvironment environment)
    {
        PosCommercialScope.BindFromRequest(context.Request, accessor, environment);
        await next(context).ConfigureAwait(false);
    }
}
