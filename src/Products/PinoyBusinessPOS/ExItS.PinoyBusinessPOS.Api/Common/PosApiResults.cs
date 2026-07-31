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
            or ApplicationErrorCodes.RepaymentNotFound
            or ApplicationErrorCodes.CategoryNotFound
            or ApplicationErrorCodes.ProductNotFound
            or ApplicationErrorCodes.SaleNotFound
            or ApplicationErrorCodes.InventoryAccountNotFound
            or ApplicationErrorCodes.InventoryProductNotFound
            or ApplicationErrorCodes.ExpenseCategoryNotFound
            or ApplicationErrorCodes.ExpenseNotFound
            or ApplicationErrorCodes.SupplierNotFound => StatusCodes.Status404NotFound,

        ApplicationErrorCodes.MobileConflict
            or ApplicationErrorCodes.ConcurrencyConflict
            or ApplicationErrorCodes.CustomerConcurrencyConflict
            or DomainErrorCodes.InvalidCustomerStatusTransition
            or DomainErrorCodes.CustomerNotActive
            or DomainErrorCodes.InvalidCreditEntryStatusTransition
            or DomainErrorCodes.InvalidRepaymentStatusTransition
            or DomainErrorCodes.RepaymentExceedsOutstanding
            or DomainErrorCodes.RepaymentOutstandingZero
            or DomainErrorCodes.CreditReversalWouldMakeOutstandingNegative
            or DomainErrorCodes.CreditDueDateNotAllowedOnReversed
            or DomainErrorCodes.CreditDueDateUnchanged
            or ApplicationErrorCodes.CategoryNameConflict
            or ApplicationErrorCodes.ProductSkuConflict
            or ApplicationErrorCodes.ProductBarcodeConflict
            or ApplicationErrorCodes.CatalogConcurrencyConflict
            or DomainErrorCodes.InvalidCategoryStatusTransition
            or DomainErrorCodes.InvalidProductStatusTransition
            or DomainErrorCodes.CategoryNotActive
            or DomainErrorCodes.ProductNotActive
            or ApplicationErrorCodes.SaleNumberConflict
            or ApplicationErrorCodes.SaleProductNotActive
            or DomainErrorCodes.InvalidSaleStatusTransition
            or ApplicationErrorCodes.CreditReversalRequiresSaleVoid
            or ApplicationErrorCodes.SaleVoidBlockedBySubsequentUtangActivity
            or ApplicationErrorCodes.InsufficientStock
            or ApplicationErrorCodes.InventoryConcurrencyConflict
            or DomainErrorCodes.InventoryInsufficientStock
            or DomainErrorCodes.InventoryDisableRequiresZero
            or DomainErrorCodes.InventoryOpeningDuplicate
            or DomainErrorCodes.InventoryUomChangeBlocked
            or DomainErrorCodes.InventoryAlreadyTracked
            or DomainErrorCodes.InventoryNotTracked
            or ApplicationErrorCodes.ExpenseCategoryNameConflict
            or ApplicationErrorCodes.ExpenseNumberConflict
            or ApplicationErrorCodes.ExpenseConcurrencyConflict
            or DomainErrorCodes.InvalidExpenseCategoryStatusTransition
            or DomainErrorCodes.ExpenseCategoryNotActive
            or DomainErrorCodes.InvalidExpenseStatusTransition
            or ApplicationErrorCodes.SupplierNameConflict
            or ApplicationErrorCodes.SupplierCodeConflict
            or ApplicationErrorCodes.SupplierEmailConflict
            or ApplicationErrorCodes.SupplierMobileConflict
            or ApplicationErrorCodes.SupplierTaxConflict
            or ApplicationErrorCodes.SupplierConcurrencyConflict
            or DomainErrorCodes.InvalidSupplierStatusTransition
            or ApplicationErrorCodes.CashierShiftNotFound
            or ApplicationErrorCodes.CashierShiftNumberConflict
            or ApplicationErrorCodes.CashierShiftOpenConflict
            or ApplicationErrorCodes.CashierShiftConcurrencyConflict
            or ApplicationErrorCodes.CashierShiftMovementConflict
            or ApplicationErrorCodes.CashierShiftNoOpenShift
            or ApplicationErrorCodes.CashierShiftMismatch
            or DomainErrorCodes.CashierShiftCancelBlockedByActivity
            or DomainErrorCodes.CashierShiftExpectedCashNegative
            or DomainErrorCodes.InvalidCashierShiftStatusTransition => StatusCodes.Status409Conflict,

        ApplicationErrorCodes.SaleProductNotFound => StatusCodes.Status400BadRequest,

        ApplicationErrorCodes.OrganizationRequired
            or ApplicationErrorCodes.ActorRequired
            or ApplicationErrorCodes.StatementInvalidPeriod
            or ApplicationErrorCodes.CategoryNotAssignable
            or ApplicationErrorCodes.ExpenseCategoryNotAssignable => StatusCodes.Status400BadRequest,

        ApplicationErrorCodes.ReportInvalidDateRange
            or ApplicationErrorCodes.ReportRangeTooLarge => StatusCodes.Status400BadRequest,

        ApplicationErrorCodes.CommercialAccessUnknown
            or ApplicationErrorCodes.CommercialCapabilityDenied
            or ApplicationErrorCodes.DevelopmentHeadersUnavailable => StatusCodes.Status403Forbidden,

        ApplicationErrorCodes.ReceiptNotFound => StatusCodes.Status404NotFound,

        _ when errorCode.Contains("not_found", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status404NotFound,
        _ when errorCode.Contains("conflict", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
