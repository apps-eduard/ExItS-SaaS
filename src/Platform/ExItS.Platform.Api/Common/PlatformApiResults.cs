using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;

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
            or ApplicationErrorCodes.TrialNotFound
            or ApplicationErrorCodes.PaymentNotFound => StatusCodes.Status404NotFound,

        ApplicationErrorCodes.SlugConflict
            or ApplicationErrorCodes.ActiveSubscriptionConflict
            or ApplicationErrorCodes.ConcurrencyConflict
            or ApplicationErrorCodes.OrganizationNotEligible
            or ApplicationErrorCodes.ProductNotActive
            or ApplicationErrorCodes.PaymentReferenceConflict
            or ApplicationErrorCodes.PaymentAlreadyConfirmed
            or ApplicationErrorCodes.PaymentNotConfirmed
            or ApplicationErrorCodes.PaymentAlreadyUsed
            or ApplicationErrorCodes.PaymentInvalidTransition
            or ApplicationErrorCodes.PaymentProductMismatch
            or ApplicationErrorCodes.PaymentOrganizationMismatch
            or ApplicationErrorCodes.PaymentSubscriptionConflict
            or DomainErrorCodes.PaymentAlreadyConfirmed
            or DomainErrorCodes.PaymentAlreadyUsed
            or DomainErrorCodes.InvalidSaaSPaymentTransition => StatusCodes.Status409Conflict,

        ApplicationErrorCodes.PaymentAmountInvalid
            or ApplicationErrorCodes.PaymentCurrencyInvalid
            or DomainErrorCodes.PaymentAmountInvalid
            or DomainErrorCodes.PaymentCurrencyInvalid
            or DomainErrorCodes.PaymentReferenceRequired
            or DomainErrorCodes.PaymentReasonRequired => StatusCodes.Status400BadRequest,

        _ when errorCode.Contains("conflict", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
        _ when errorCode.Contains("invalid_transition", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
        _ when errorCode.Contains("not_eligible", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,

        _ => StatusCodes.Status400BadRequest
    };
}
