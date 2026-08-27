using ExItS.PinoyBuyNowPayLater.Application.Access;
using ExItS.PinoyBuyNowPayLater.Domain.Access;

namespace ExItS.PinoyBuyNowPayLater.Api.Access;

internal static class BnplApiResults
{
    public static IResult FromDenial(BnplOperationalAccessDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.IsAllowed)
        {
            throw new InvalidOperationException("Cannot map an allowed access decision to a denial response.");
        }

        var statusCode = MapStatusCode(decision.DenialReason);
        return Results.Problem(
            detail: decision.Detail ?? "Access denied.",
            statusCode: statusCode,
            extensions: new Dictionary<string, object?>
            {
                ["errorCode"] = decision.ErrorCode ?? BnplAccessErrorCodes.ContextUnavailable
            });
    }

    private static int MapStatusCode(BnplOperationalAccessDenialReason? reason) => reason switch
    {
        BnplOperationalAccessDenialReason.ContextUnavailable => StatusCodes.Status503ServiceUnavailable,
        BnplOperationalAccessDenialReason.ActorMissing => StatusCodes.Status401Unauthorized,
        _ => StatusCodes.Status403Forbidden
    };
}
