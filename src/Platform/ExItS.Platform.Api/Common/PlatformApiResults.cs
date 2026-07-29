using ExItS.Platform.Application.Common;

namespace ExItS.Platform.Api.Common;

/// <summary>Shared ProblemDetails mapping for Organization and Subscription endpoints (mirrors CatalogResults).</summary>
internal static class PlatformApiResults
{
    public static IResult FromResult<T>(ApplicationResult<T> result, Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        return Problem(result.ErrorCode!, result.ErrorMessage!, MapStatusCode(result.ErrorCode!));
    }

    public static IResult Problem(string errorCode, string detail, int statusCode) =>
        Results.Problem(
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["errorCode"] = errorCode });

    public static int MapStatusCode(string errorCode) => errorCode switch
    {
        ApplicationErrorCodes.OrganizationNotFound
            or ApplicationErrorCodes.SubscriptionNotFound
            or ApplicationErrorCodes.PlanNotFound
            or ApplicationErrorCodes.PlanVersionNotFound
            or ApplicationErrorCodes.ProductNotFound
            or ApplicationErrorCodes.TrialNotFound => StatusCodes.Status404NotFound,

        ApplicationErrorCodes.SlugConflict
            or ApplicationErrorCodes.ActiveSubscriptionConflict
            or ApplicationErrorCodes.ConcurrencyConflict
            or ApplicationErrorCodes.OrganizationNotEligible
            or ApplicationErrorCodes.ProductNotActive => StatusCodes.Status409Conflict,

        _ when errorCode.Contains("conflict", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
        _ when errorCode.Contains("invalid_transition", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
        _ when errorCode.Contains("not_eligible", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,

        _ => StatusCodes.Status400BadRequest
    };
}
