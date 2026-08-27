using ExItS.PinoyBuyNowPayLater.Api.Access;
using ExItS.PinoyBuyNowPayLater.Application.Access;
using ExItS.PinoyBuyNowPayLater.Domain.Access;

namespace ExItS.PinoyBuyNowPayLater.Api;

internal static class BnplStaffEndpointAuth
{
    public const string BranchHeaderName = "X-Bnpl-Branch-Id";

    public static async Task<(BnplAccessContext? Context, IResult? Denial)> AuthorizeAsync(
        HttpContext httpContext,
        string capability,
        CancellationToken cancellationToken)
    {
        if (!TryResolveBranchId(httpContext, out var branchId, out var branchError))
        {
            return (null, branchError);
        }

        var guard = httpContext.RequestServices.GetRequiredService<IBnplOperationalAccessGuard>();
        var decision = await guard
            .EvaluateAsync(BnplAccessRequirement.ForBranchAndCapability(branchId, capability), cancellationToken)
            .ConfigureAwait(false);
        if (!decision.IsAllowed || decision.Context is null)
        {
            return (null, BnplApiResults.FromDenial(decision));
        }

        httpContext.Items[BnplAccessHttpContextKeys.Context] = decision.Context;
        return (decision.Context, null);
    }

    public static bool TryResolveBranchId(HttpContext httpContext, out Guid branchId, out IResult? error)
    {
        branchId = Guid.Empty;
        error = null;
        var raw = httpContext.Request.Headers[BranchHeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(raw) || !Guid.TryParse(raw, out branchId) || branchId == Guid.Empty)
        {
            error = Results.Problem(
                detail: "X-Bnpl-Branch-Id header with a non-empty Guid is required for BNPL staff operations.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?>
                {
                    ["errorCode"] = BnplAccessErrorCodes.BranchRequired
                });
            return false;
        }

        return true;
    }
}
