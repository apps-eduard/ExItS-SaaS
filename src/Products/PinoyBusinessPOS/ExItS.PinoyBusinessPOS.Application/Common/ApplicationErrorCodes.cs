namespace ExItS.PinoyBusinessPOS.Application.Common;

public static class ApplicationErrorCodes
{
    public const string CustomerNotFound = "pos.customer.not_found";
    public const string CheckoutCustomerSearchRequired = "pos.customer.checkout_search.required";
    public const string LinkedCustomerNotFound = "pos.linked_customer.not_found";
    public const string LinkedCustomerDenied = "pos.linked_customer.denied";
    public const string ExtendedHistoryRequired = "pos.personal.extended_history_required";
    public const string MobileConflict = "pos.customer.mobile.conflict";
    public const string PlatformBusinessCustomerCorrelationConflict =
        "pos.customer.platform_business_customer.correlation_conflict";
    public const string CustomerConcurrencyConflict = "pos.customer.concurrency_conflict";
    public const string CreditEntryNotFound = "pos.credit_entry.not_found";
    public const string RepaymentNotFound = "pos.repayment.not_found";
    public const string WriteOffNotFound = "pos.write_off.not_found";
    public const string ActorRequired = "pos.actor.required";
    public const string ConcurrencyConflict = "pos.concurrency_conflict";
    public const string OrganizationRequired = "pos.organization.required";
    public const string DomainViolation = "pos.domain_violation";
    public const string CommercialAccessUnknown = "pos.commercial.access_unknown";
    public const string CommercialCapabilityDenied = "pos.commercial.capability_denied";
    public const string DevelopmentHeadersUnavailable = "pos.development_headers.unavailable";
    public const string PlatformAuthUnavailable = "pos.platform_auth.unavailable";
    public const string StatementInvalidPeriod = "pos.statement.invalid_period";
    public const string ReceiptNotFound = "pos.receipt.not_found";

    public const string CategoryNotFound = "pos.category.not_found";
    public const string CategoryNameConflict = "pos.category.name.conflict";
    public const string BrandNotFound = "pos.brand.not_found";
    public const string BrandNameConflict = "pos.brand.name.conflict";
    public const string BrandNotAssignable = "pos.brand.not_assignable";
    public const string CategoryNotAssignable = "pos.category.not_assignable";
    public const string ProductNotFound = "pos.product.not_found";
    public const string ProductNameConflict = "pos.catalog.product.name.conflict";
    public const string ProductSkuConflict = "pos.product.sku.conflict";
    public const string ProductBarcodeConflict = "pos.product.barcode.conflict";
    public const string CatalogConcurrencyConflict = "pos.catalog.concurrency_conflict";
    public const string CatalogPriceBulkEmpty = "pos.catalog.price_bulk_empty";
    public const string CatalogPriceBulkDuplicate = "pos.catalog.price_bulk_duplicate";
    public const string CatalogBulkValidation = "pos.catalog.bulk_validation";
    public const string ProductScopeForbidden = "pos.catalog.product_scope_forbidden";
    public const string ProductOriginBranchForbidden = "pos.catalog.product_origin_branch_forbidden";
    public const string ProductNotOfferedAtBranch = "pos.catalog.product_not_offered_at_branch";
    public const string ProductPromotionForbidden = "pos.catalog.product_promotion_forbidden";
    public const string ProductAvailabilityForbidden = "pos.catalog.product_availability_forbidden";
    public const string ProductActingBranchRequired = "pos.catalog.product_acting_branch_required";
    public const string ProductBranchInvalid = "pos.catalog.product_branch_invalid";

    public const string SaleNotFound = "pos.sale.not_found";
    public const string TaxDocumentIssuanceNotAvailable =
        "pos.sales_document.tax_document_issuance_not_available";
    public const string PaymentAttemptConflict = "pos.payment_attempt.conflict";
    public const string ManualGCashTransferDisabled = "pos.payment.manual_gcash_transfer.disabled";
    public const string PaymentProviderUnsupported = "pos.payment.provider.unsupported";
    public const string PaymentSimulationOutcomeInvalid = "pos.payment.simulation.outcome.invalid";
    public const string SaleProductNotFound = "pos.sale.product.not_found";
    public const string SaleProductNotActive = "pos.sale.product.not_active";
    public const string SaleProductNotSellable = "pos.sale.product.not_sellable";
    public const string SaleSnapshotIncomplete = "pos.sale.snapshot.incomplete";
    public const string SaleSnapshotLineTotalMismatch = "pos.sale.snapshot.line_total_mismatch";
    public const string SaleSnapshotInvalid = "pos.sale.snapshot.invalid";
    public const string SaleDiscountOfflineNotSupported = "pos.sale.discount.offline_not_supported";
    public const string SalePriceOverrideOfflineNotSupported =
        "pos.sale.price_override.offline_not_supported";
    public const string SaleNumberConflict = "pos.sale.number.conflict";
    public const string OfflinePriceAuthorityRequestInvalid = "pos.offline_price_authority.request.invalid";
    public const string OfflinePriceAuthorityTampered = "pos.offline_price_authority.tampered";
    public const string OfflinePriceAuthorityExpired = "pos.offline_price_authority.expired";
    public const string OfflinePriceAuthorityWrongOrganization = "pos.offline_price_authority.wrong_organization";
    public const string OfflinePriceAuthorityWrongBranch = "pos.offline_price_authority.wrong_branch";
    public const string OfflinePriceAuthorityWrongProduct = "pos.offline_price_authority.wrong_product";
    public const string OfflinePriceAuthorityRequiredOnEveryLine =
        "pos.offline_price_authority.required_on_every_line";
    public const string OfflinePriceAuthorityLineMismatch = "pos.offline_price_authority.line_mismatch";
    public const string OfflinePriceAuthorityOnlineNotSupported =
        "pos.offline_price_authority.online_not_supported";
    public const string OfflineOperatingGrantInvalidScope = "pos.offline_operating_grant.invalid_scope";
    public const string OfflineOperatingGrantDeviceRequired = "pos.offline_operating_grant.device_required";
    public const string OfflineOperatingGrantSigningUnavailable = "pos.offline_operating_grant.signing_unavailable";
    public const string OfflineOperatingGrantTampered = "pos.offline_operating_grant.tampered";
    public const string OfflineOperatingGrantExpired = "pos.offline_operating_grant.expired";
    public const string OfflineOperatingGrantDenied = "pos.offline_operating_grant.denied";
    public const string CreditReversalRequiresSaleVoid = "pos.credit_entry.reversal.requires_sale_void";
    public const string SaleVoidBlockedBySubsequentUtangActivity = "pos.sale.void.blocked_by_subsequent_utang";

    public const string InventoryAccountNotFound = "pos.inventory.account.not_found";
    public const string InventoryProductNotFound = "pos.inventory.product.not_found";
    public const string InventoryMovementNotFound = "pos.inventory.movement.not_found";
    public const string InventoryConcurrencyConflict = "pos.inventory.concurrency_conflict";
    public const string StockCountNotFound = "pos.stock_count.not_found";
    public const string StockCountNumberConflict = "pos.stock_count.number.conflict";
    public const string InsufficientStock = "pos.inventory.insufficient_stock";
    public const string ProductImageInvalid = "pos.product.image.invalid";
    public const string ProductImageTooLarge = "pos.product.image.too_large";
    public const string ProductImageUnsupportedType = "pos.product.image.unsupported_type";
    public const string ProductImageNotFound = "pos.product.image.not_found";
    public const string InventoryTransferNotFound = "pos.inventory.transfer.not_found";
    public const string InventoryTransferNumberConflict = "pos.inventory.transfer.number.conflict";
    public const string InventoryTransferConcurrencyConflict = "pos.inventory.transfer.concurrency_conflict";
    public const string InventoryTransferBranchNotFound = "pos.inventory.transfer.branch.not_found";
    public const string InventoryTransferProductNotTracked = "pos.inventory.transfer.product.not_tracked";
    public const string InventoryTransferAlreadyReceived = "pos.inventory.transfer.already_received";
    public const string InventoryTransferBranchForbidden = "pos.inventory.transfer.branch.forbidden";

    /// <summary>On-hand stock must be allocated into lots via enable-expiration-tracking.</summary>
    public const string ExpirationInitializationRequired = "pos.inventory.expiration.initialization_required";
    /// <summary>Submitted lot quantities do not sum exactly to authoritative on-hand.</summary>
    public const string ExpirationAllocationMismatch = "pos.inventory.expiration.allocation_mismatch";
    /// <summary>On-hand changed concurrently during expiration lot allocation.</summary>
    public const string ExpirationAllocationStockChanged = "pos.inventory.expiration.allocation_stock_changed";
    /// <summary>Expiration tracking is already enabled with an inconsistent lot/on-hand state.</summary>
    public const string ExpirationTrackingAlreadyEnabled = "pos.inventory.expiration.already_enabled";
    /// <summary>Cannot disable expiration tracking while on-hand quantity is greater than zero.</summary>
    public const string ExpirationDisableRequiresZeroOnHand = "pos.inventory.expiration.disable_requires_zero_on_hand";
    /// <summary>An existing-stock lot line has invalid quantity (must be &gt; 0).</summary>
    public const string ExpirationLotQuantityInvalid = "pos.inventory.expiration.lot_quantity_invalid";

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

    public const string SupplierPayableNotFound = "pos.supplier_payable.not_found";
    public const string SupplierPayableConcurrencyConflict = "pos.supplier_payable.concurrency_conflict";

    public const string DirectPurchaseReceiptNotFound = "pos.direct_purchase_receipt.not_found";
    public const string StockUseNotFound = "pos.stock_use.not_found";
    public const string StockUseNumberConflict = "pos.stock_use.number.conflict";
    public const string ProductionDefinitionNotFound = "pos.production.definition.not_found";
    public const string ProductionRunNotFound = "pos.production.run.not_found";
    public const string ProductionNumberConflict = "pos.production.run.number.conflict";
    public const string WasteLossNotFound = "pos.waste_loss.not_found";
    public const string WasteLossNumberConflict = "pos.waste_loss.number.conflict";
    public const string DirectPurchaseReceiptNumberConflict = "pos.direct_purchase_receipt.number.conflict";
    public const string DirectPurchaseProductNotPurchasable = "pos.direct_purchase_receipt.product.not_purchasable";

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
    /// <summary>
    /// Prior ReturnToStock account restock exists for an expiration-tracked product without lot
    /// SaleReturnRestock evidence — cannot safely allocate further lot restores.
    /// </summary>
    public const string ExpiryReturnHistoryReconciliationGap =
        "RMAP14_EXPIRY_RETURN_HISTORY_RECONCILIATION_GAP";
    public const string SaleReturnBranchRequired = "pos.sale_return.branch_required";
    public const string SaleReturnLotRestoreInsufficient =
        "pos.sale_return.lot_restore.insufficient";

    public const string RegisterNotFound = "pos.register.not_found";
    public const string RegisterNameConflict = "pos.register.name.conflict";
    public const string RegisterCodeConflict = "pos.register.code.conflict";
    public const string RegisterConcurrencyConflict = "pos.register.concurrency_conflict";

    public const string OperationalSetupConcurrencyConflict = "pos.operational_setup.concurrency_conflict";
    public const string TaxConfigurationNotEnabled = "pos.operational_setup.tax_configuration_not_enabled";

    public const string OnboardingProgressNotFound = "pos.onboarding.progress.not_found";
    public const string OnboardingProgressConcurrencyConflict = "pos.onboarding.progress.concurrency_conflict";

    public const string CatalogImportJobNotFound = "pos.catalog_import.job.not_found";
    public const string CatalogImportIdempotencyConflict = "pos.catalog_import.idempotency.conflict";
    public const string CatalogImportPlatformUnavailable = "pos.catalog_import.platform_unavailable";
    public const string CatalogImportPlatformSessionRequired = "pos.catalog_import.platform_session_required";
    public const string CatalogImportTemplateNotFound = "pos.catalog_import.template.not_found";
    public const string CatalogImportNoProducts = "pos.catalog_import.no_products";
    public const string CatalogImportProductAlreadyImported = "pos.catalog_import.product.already_imported";
    public const string CatalogImportDuplicateGlobalProduct = "pos.catalog_import.product.duplicate_global";

    public const string CustomerOrderNotFound = "pos.customer_order.not_found";
    public const string CustomerOrderNumberConflict = "pos.customer_order.number.conflict";
    public const string CustomerOrderBranchNotFound = "pos.customer_order.branch.not_found";
    public const string ReportBranchNotFound = "pos.report.branch.not_found";
    public const string OperationalBranchSwitchBlocked = "pos.branch.switch.shift_open";
    public const string SaleBranchRequired = "pos.sale.branch_required";
    public const string InventoryBranchRequired = "pos.inventory.branch_required";
    public const string CustomerOrderBranchCapability = "pos.customer_order.branch.capability";
    public const string CustomerOrderPartyMismatch = "pos.customer_order.party.mismatch";
    public const string CustomerOrderDeliveryUnavailable = "pos.customer_order.delivery.unavailable";
    public const string CustomerOrderDeliveryServiceAreaInvalid = "pos.customer_order.delivery.service_area.invalid";
    public const string CustomerOrderOrderingUnavailable = "pos.customer_order.ordering.unavailable";
    public const string CustomerOrderLinkedCustomerRequired = "pos.customer_order.linked_customer.required";
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
