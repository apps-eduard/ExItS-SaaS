namespace ExItS.PinoyBusinessPOS.Application.Common;

public static class ApplicationErrorCodes
{
    public const string CustomerNotFound = "pos.customer.not_found";
    public const string MobileConflict = "pos.customer.mobile.conflict";
    public const string CustomerConcurrencyConflict = "pos.customer.concurrency_conflict";
    public const string CreditEntryNotFound = "pos.credit_entry.not_found";
    public const string RepaymentNotFound = "pos.repayment.not_found";
    public const string ActorRequired = "pos.actor.required";
    public const string ConcurrencyConflict = "pos.concurrency_conflict";
    public const string OrganizationRequired = "pos.organization.required";
    public const string DomainViolation = "pos.domain_violation";
    public const string CommercialAccessUnknown = "pos.commercial.access_unknown";
    public const string CommercialCapabilityDenied = "pos.commercial.capability_denied";
    public const string StatementInvalidPeriod = "pos.statement.invalid_period";
    public const string ReceiptNotFound = "pos.receipt.not_found";

    public const string CategoryNotFound = "pos.category.not_found";
    public const string CategoryNameConflict = "pos.category.name.conflict";
    public const string CategoryNotAssignable = "pos.category.not_assignable";
    public const string ProductNotFound = "pos.product.not_found";
    public const string ProductSkuConflict = "pos.product.sku.conflict";
    public const string ProductBarcodeConflict = "pos.product.barcode.conflict";
    public const string CatalogConcurrencyConflict = "pos.catalog.concurrency_conflict";

    public const string SaleNotFound = "pos.sale.not_found";
    public const string SaleProductNotFound = "pos.sale.product.not_found";
    public const string SaleProductNotActive = "pos.sale.product.not_active";
    public const string SaleNumberConflict = "pos.sale.number.conflict";
}

public sealed class PersistenceConflictException : Exception
{
    public string ErrorCode { get; }

    public PersistenceConflictException(string errorCode, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ErrorCode = errorCode;
    }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public static class PosPagination
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public static (int Skip, int Take) Normalize(int? page, int? pageSize)
    {
        var take = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var pageNumber = Math.Max(page ?? 1, 1);
        return ((pageNumber - 1) * take, take);
    }
}
