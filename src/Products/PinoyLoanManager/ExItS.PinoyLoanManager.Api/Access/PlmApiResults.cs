using ExItS.PinoyLoanManager.Application.Access;

namespace ExItS.PinoyLoanManager.Api.Access;

internal static class PlmApiResults
{
    public static IResult FromDenial(PlmOperationalAccessDecision decision)
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
                ["errorCode"] = decision.ErrorCode ?? PlmAccessErrorCodes.ContextUnavailable
            });
    }

    private static int MapStatusCode(PlmOperationalAccessDenialReason? reason) => reason switch
    {
        PlmOperationalAccessDenialReason.ContextUnavailable => StatusCodes.Status503ServiceUnavailable,
        PlmOperationalAccessDenialReason.ActorMissing => StatusCodes.Status401Unauthorized,
        PlmOperationalAccessDenialReason.OrganizationMissing => StatusCodes.Status403Forbidden,
        PlmOperationalAccessDenialReason.WrongProduct => StatusCodes.Status403Forbidden,
        PlmOperationalAccessDenialReason.ProductAccessDenied => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status403Forbidden
    };
}
