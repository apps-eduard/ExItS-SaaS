using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Api.Common;

internal static class PosApiResults
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
        ApplicationErrorCodes.CustomerNotFound
            or ApplicationErrorCodes.CreditEntryNotFound
            or ApplicationErrorCodes.RepaymentNotFound => StatusCodes.Status404NotFound,

        ApplicationErrorCodes.MobileConflict
            or ApplicationErrorCodes.ConcurrencyConflict
            or DomainErrorCodes.InvalidCustomerStatusTransition
            or DomainErrorCodes.CustomerNotActive
            or DomainErrorCodes.InvalidCreditEntryStatusTransition
            or DomainErrorCodes.InvalidRepaymentStatusTransition
            or DomainErrorCodes.RepaymentExceedsOutstanding
            or DomainErrorCodes.RepaymentOutstandingZero
            or DomainErrorCodes.CreditReversalWouldMakeOutstandingNegative => StatusCodes.Status409Conflict,

        ApplicationErrorCodes.OrganizationRequired
            or ApplicationErrorCodes.ActorRequired => StatusCodes.Status400BadRequest,

        _ when errorCode.Contains("not_found", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status404NotFound,
        _ when errorCode.Contains("conflict", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
