using System.Globalization;
using ExItS.PinoyBusinessPOS.Application.Catalog;
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

        return Problem(result.ErrorCode!, result.ErrorMessage!, MapStatusCode(result.ErrorCode!), result.ErrorDetails);
    }

    public static IResult ImageFile(HttpResponse response, ProductImageBytes image)
    {
        response.Headers["X-ExItS-Image-Version"] = image.Version.ToString(CultureInfo.InvariantCulture);
        return Results.File(image.Content, image.ContentType);
    }

    public static IResult FromResult(ApplicationResult result, Func<IResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess();
        }

        return Problem(result.ErrorCode!, result.ErrorMessage!, MapStatusCode(result.ErrorCode!), result.ErrorDetails);
    }

    public static IResult Problem(
        string errorCode,
        string detail,
        int statusCode,
        IReadOnlyDictionary<string, string>? errorDetails = null)
    {
        var extensions = new Dictionary<string, object?> { ["errorCode"] = errorCode };
        if (errorDetails is not null)
        {
            foreach (var pair in errorDetails)
            {
                extensions[pair.Key] = pair.Value;
            }
        }

        return Results.Problem(detail: detail, statusCode: statusCode, extensions: extensions);
    }

    public static int MapStatusCode(string errorCode) => errorCode switch
    {
        ApplicationErrorCodes.CustomerNotFound
            or ApplicationErrorCodes.LinkedCustomerNotFound
            or ApplicationErrorCodes.CreditEntryNotFound
            or ApplicationErrorCodes.RepaymentNotFound
            or ApplicationErrorCodes.CategoryNotFound
            or ApplicationErrorCodes.ProductNotFound
            or ApplicationErrorCodes.SaleNotFound
            or DomainErrorCodes.PaymentAttemptNotFound
            or ApplicationErrorCodes.InventoryAccountNotFound
            or ApplicationErrorCodes.InventoryProductNotFound
            or ApplicationErrorCodes.InventoryTransferNotFound
            or ApplicationErrorCodes.ExpenseCategoryNotFound
            or ApplicationErrorCodes.ExpenseNotFound
            or ApplicationErrorCodes.SupplierNotFound
            or ApplicationErrorCodes.RegisterNotFound => StatusCodes.Status404NotFound,

        ApplicationErrorCodes.MobileConflict
            or ApplicationErrorCodes.PlatformBusinessCustomerCorrelationConflict
            or DomainErrorCodes.PlatformBusinessCustomerCorrelationConflict
            or DomainErrorCodes.CustomerExItsIdentityLinkConflict
            or DomainErrorCodes.InvalidPlatformBusinessCustomerId
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
            or ApplicationErrorCodes.BrandNameConflict
            or ApplicationErrorCodes.ProductSkuConflict
            or ApplicationErrorCodes.ProductBarcodeConflict
            or ApplicationErrorCodes.CatalogConcurrencyConflict
            or DomainErrorCodes.InvalidCategoryStatusTransition
            or DomainErrorCodes.InvalidBrandStatusTransition
            or DomainErrorCodes.InvalidProductStatusTransition
            or DomainErrorCodes.CategoryNotActive
            or DomainErrorCodes.BrandNotActive
            or DomainErrorCodes.ProductNotActive
            or ApplicationErrorCodes.SaleNumberConflict
            or ApplicationErrorCodes.SaleProductNotActive
            or ApplicationErrorCodes.SaleProductNotSellable
            or ApplicationErrorCodes.PaymentAttemptConflict
            or DomainErrorCodes.InvalidSaleStatusTransition
            or ApplicationErrorCodes.CreditReversalRequiresSaleVoid
            or ApplicationErrorCodes.SaleVoidBlockedBySubsequentUtangActivity
            or ApplicationErrorCodes.SaleVoidBlockedByReturns
            or ApplicationErrorCodes.SaleReturnNumberConflict
            or ApplicationErrorCodes.InsufficientStock
            or ApplicationErrorCodes.InventoryConcurrencyConflict
            or ApplicationErrorCodes.InventoryTransferAlreadyReceived
            or ApplicationErrorCodes.InventoryTransferNumberConflict
            or ApplicationErrorCodes.InventoryTransferConcurrencyConflict
            or ApplicationErrorCodes.ExpirationAllocationStockChanged
            or ApplicationErrorCodes.ExpirationTrackingAlreadyEnabled
            or ApplicationErrorCodes.ExpirationDisableRequiresZeroOnHand
            or ApplicationErrorCodes.ExpirationInitializationRequired
            or ApplicationErrorCodes.ExpirationAllocationMismatch
            or DomainErrorCodes.InventoryInsufficientStock
            or DomainErrorCodes.GoodsReceiptVoidInsufficient
            or DomainErrorCodes.DirectPurchaseReceiptVoidInsufficient
            or DomainErrorCodes.ProductionVoidOutputInsufficient
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
            or DomainErrorCodes.InvalidCashierShiftStatusTransition
            or DomainErrorCodes.PosRoleAssignmentConflict
            or DomainErrorCodes.PosRoleLastOwnerProtected
            or ApplicationErrorCodes.RegisterNameConflict
            or ApplicationErrorCodes.RegisterCodeConflict
            or ApplicationErrorCodes.RegisterConcurrencyConflict
            or DomainErrorCodes.RegisterDeactivateBlockedByOpenShift
            or DomainErrorCodes.RegisterNotActive
            or DomainErrorCodes.CashierShiftRegisterConflict
            or DomainErrorCodes.InvalidRegisterStatusTransition
            or ApplicationErrorCodes.OperationalBranchSwitchBlocked => StatusCodes.Status409Conflict,

        ApplicationErrorCodes.SaleProductNotFound => StatusCodes.Status400BadRequest,

        ApplicationErrorCodes.CatalogImportProductAlreadyImported => StatusCodes.Status409Conflict,

        ApplicationErrorCodes.OrganizationRequired
            or ApplicationErrorCodes.ActorRequired
            or ApplicationErrorCodes.StatementInvalidPeriod
            or ApplicationErrorCodes.CategoryNotAssignable
            or ApplicationErrorCodes.BrandNotAssignable
            or ApplicationErrorCodes.ExpenseCategoryNotAssignable => StatusCodes.Status400BadRequest,

        ApplicationErrorCodes.ReportInvalidDateRange
            or ApplicationErrorCodes.ReportRangeTooLarge => StatusCodes.Status400BadRequest,

        ApplicationErrorCodes.CommercialAccessUnknown
            or ApplicationErrorCodes.CommercialCapabilityDenied
            or ApplicationErrorCodes.DevelopmentHeadersUnavailable
            or ApplicationErrorCodes.LinkedCustomerDenied
            or ApplicationErrorCodes.ExtendedHistoryRequired
            or ApplicationErrorCodes.InventoryTransferBranchForbidden
            or ApplicationErrorCodes.CustomerOrderOrderingUnavailable
            or ApplicationErrorCodes.CustomerOrderPartyMismatch => StatusCodes.Status403Forbidden,

        ApplicationErrorCodes.PlatformAuthUnavailable
            or ApplicationErrorCodes.CatalogImportPlatformUnavailable => StatusCodes.Status503ServiceUnavailable,

        ApplicationErrorCodes.ReceiptNotFound => StatusCodes.Status404NotFound,

        _ when errorCode.Contains("not_found", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status404NotFound,
        _ when errorCode.Contains("conflict", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest
    };
}
