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
    public const string DevelopmentHeadersUnavailable = "pos.development_headers.unavailable";
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
    public const string CreditReversalRequiresSaleVoid = "pos.credit_entry.reversal.requires_sale_void";
    public const string SaleVoidBlockedBySubsequentUtangActivity = "pos.sale.void.blocked_by_subsequent_utang";

    public const string InventoryAccountNotFound = "pos.inventory.account.not_found";
    public const string InventoryProductNotFound = "pos.inventory.product.not_found";
    public const string InventoryConcurrencyConflict = "pos.inventory.concurrency_conflict";
    public const string StockCountNotFound = "pos.stock_count.not_found";
    public const string StockCountNumberConflict = "pos.stock_count.number.conflict";
    public const string InsufficientStock = "pos.inventory.insufficient_stock";

    public const string ExpenseCategoryNotFound = "pos.expense_category.not_found";
    public const string ExpenseCategoryNameConflict = "pos.expense_category.name.conflict";
    public const string ExpenseCategoryNotAssignable = "pos.expense_category.not_assignable";
    public const string ExpenseNotFound = "pos.expense.not_found";
    public const string ExpenseNumberConflict = "pos.expense.number.conflict";
    public const string ExpenseConcurrencyConflict = "pos.expense.concurrency_conflict";

    public const string ReportInvalidDateRange = "pos.report.invalid_date_range";
    public const string ReportRangeTooLarge = "pos.report.range_too_large";

    public const string SupplierNotFound = "pos.supplier.not_found";
    public const string SupplierNameConflict = "pos.supplier.name.conflict";
    public const string SupplierCodeConflict = "pos.supplier.code.conflict";
    public const string SupplierEmailConflict = "pos.supplier.email.conflict";
    public const string SupplierMobileConflict = "pos.supplier.mobile.conflict";
    public const string SupplierTaxConflict = "pos.supplier.tax_number.conflict";
    public const string SupplierConcurrencyConflict = "pos.supplier.concurrency_conflict";

    public const string PurchaseOrderNotFound = "pos.purchase_order.not_found";
    public const string GoodsReceiptNotFound = "pos.goods_receipt.not_found";
    public const string PurchaseOrderNumberConflict = "pos.purchase_order.number.conflict";
    public const string GoodsReceiptNumberConflict = "pos.goods_receipt.number.conflict";
    public const string PurchaseOrderConcurrencyConflict = "pos.purchase_order.concurrency_conflict";
    public const string PurchaseSupplierNotActive = "pos.purchase_order.supplier.not_active";
    public const string PurchaseProductNotFound = "pos.purchase_order.product.not_found";
    public const string PurchaseProductNotActive = "pos.purchase_order.product.not_active";

    public const string CashierShiftNotFound = "pos.cashier_shift.not_found";
    public const string CashierShiftNumberConflict = "pos.cashier_shift.number.conflict";
    public const string CashierShiftOpenConflict = "pos.cashier_shift.open.conflict";
    public const string CashierShiftConcurrencyConflict = "pos.cashier_shift.concurrency_conflict";
    public const string CashierShiftMovementNotFound = "pos.cashier_shift_movement.not_found";
    public const string CashierShiftMovementConflict = "pos.cashier_shift_movement.conflict";
    public const string CashierShiftNoOpenShift = "pos.cashier_shift.no_open_shift";
    public const string CashierShiftMismatch = "pos.cashier_shift.mismatch";

    public const string SaleReturnNotFound = "pos.sale_return.not_found";
    public const string SaleReturnNumberConflict = "pos.sale_return.number.conflict";
    public const string SaleVoidBlockedByReturns = "pos.sale.void.blocked_by_returns";

    public const string RegisterNotFound = "pos.register.not_found";
    public const string RegisterNameConflict = "pos.register.name.conflict";
    public const string RegisterCodeConflict = "pos.register.code.conflict";
    public const string RegisterConcurrencyConflict = "pos.register.concurrency_conflict";

    public const string OperationalSetupConcurrencyConflict = "pos.operational_setup.concurrency_conflict";

    public const string CatalogImportJobNotFound = "pos.catalog_import.job.not_found";
    public const string CatalogImportIdempotencyConflict = "pos.catalog_import.idempotency.conflict";
    public const string CatalogImportPlatformUnavailable = "pos.catalog_import.platform_unavailable";
    public const string CatalogImportTemplateNotFound = "pos.catalog_import.template.not_found";
    public const string CatalogImportNoProducts = "pos.catalog_import.no_products";
    public const string CatalogImportProductAlreadyImported = "pos.catalog_import.product.already_imported";
    public const string CatalogImportDuplicateGlobalProduct = "pos.catalog_import.product.duplicate_global";
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
