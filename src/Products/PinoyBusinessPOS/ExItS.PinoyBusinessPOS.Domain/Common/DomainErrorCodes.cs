namespace ExItS.PinoyBusinessPOS.Domain.Common;

public static class DomainErrorCodes
{
    public const string InvalidCustomerId = "pos.customer.id.invalid";
    public const string InvalidOrganizationId = "pos.organization.id.invalid";
    public const string InvalidDisplayName = "pos.customer.display_name.invalid";
    public const string InvalidMobileNumber = "pos.customer.mobile.invalid";
    public const string InvalidAddress = "pos.customer.address.invalid";
    public const string InvalidNotes = "pos.customer.notes.invalid";
    public const string InvalidUtcTimestamp = "pos.timestamp.invalid";
    public const string InvalidCustomerStatusTransition = "pos.customer.status.invalid_transition";
    public const string CustomerNotActive = "pos.customer.not_active";
    public const string InvalidPlatformBusinessCustomerId = "pos.customer.platform_business_customer_id.invalid";
    public const string PlatformBusinessCustomerCorrelationConflict =
        "pos.customer.platform_business_customer.correlation_conflict";

    public const string InvalidCreditEntryId = "pos.credit_entry.id.invalid";
    public const string InvalidCreditAmount = "pos.credit_entry.amount.invalid";
    public const string InvalidCreditRemarks = "pos.credit_entry.remarks.invalid";
    public const string InvalidCreditReversalReason = "pos.credit_entry.reversal_reason.invalid";
    public const string InvalidCreditEntryStatusTransition = "pos.credit_entry.status.invalid_transition";
    public const string CreditReversalWouldMakeOutstandingNegative = "pos.credit_entry.reversal.outstanding_negative";
    public const string CreditDueDateNotAllowedOnReversed = "pos.credit_entry.due_date.reversed";
    public const string InvalidCreditDueDateChangeId = "pos.credit_due_date.id.invalid";
    public const string InvalidCreditDueDateReason = "pos.credit_due_date.reason.invalid";
    public const string InvalidCreditDueDateActor = "pos.credit_due_date.actor.invalid";
    public const string CreditDueDateUnchanged = "pos.credit_due_date.unchanged";

    public const string InvalidRepaymentId = "pos.repayment.id.invalid";
    public const string InvalidRepaymentAmount = "pos.repayment.amount.invalid";
    public const string InvalidRepaymentRemarks = "pos.repayment.remarks.invalid";
    public const string InvalidRepaymentReversalReason = "pos.repayment.reversal_reason.invalid";
    public const string InvalidRepaymentStatusTransition = "pos.repayment.status.invalid_transition";
    public const string InvalidRepaymentActor = "pos.repayment.actor.invalid";
    public const string RepaymentExceedsOutstanding = "pos.repayment.exceeds_outstanding";
    public const string RepaymentOutstandingZero = "pos.repayment.outstanding_zero";

    public const string InvalidCatalogProductId = "pos.catalog.product.id.invalid";
    public const string InvalidProductCategoryId = "pos.catalog.category.id.invalid";
    public const string InvalidUnitOfMeasure = "pos.catalog.unit_of_measure.invalid";
    public const string InvalidSellingMode = "pos.catalog.selling_mode.invalid";
    public const string InvalidSellingModeUnit = "pos.catalog.selling_mode.unit.invalid";
    public const string InvalidWeightQuantity = "pos.catalog.weight.quantity.invalid";
    public const string InvalidWeightInputUnit = "pos.catalog.weight.input_unit.invalid";

    public const string InvalidCategoryName = "pos.category.name.invalid";
    public const string InvalidCategoryStatus = "pos.category.status.invalid";
    public const string InvalidCategoryStatusTransition = "pos.category.status.invalid_transition";
    public const string CategoryNotActive = "pos.category.not_active";

    public const string InvalidProductName = "pos.product.name.invalid";
    public const string InvalidProductDescription = "pos.product.description.invalid";
    public const string InvalidProductSku = "pos.product.sku.invalid";
    public const string InvalidProductBarcode = "pos.product.barcode.invalid";
    public const string InvalidProductSellingPrice = "pos.product.selling_price.invalid";
    public const string InvalidProductStatus = "pos.product.status.invalid";
    public const string InvalidProductStatusTransition = "pos.product.status.invalid_transition";
    public const string ProductNotActive = "pos.product.not_active";

    public const string InvalidSaleId = "pos.sale.id.invalid";
    public const string InvalidSaleLineId = "pos.sale_line.id.invalid";
    public const string InvalidSaleNumber = "pos.sale.number.invalid";
    public const string InvalidSaleStatus = "pos.sale.status.invalid";
    public const string InvalidSaleStatusTransition = "pos.sale.status.invalid_transition";
    public const string InvalidSalePaymentMethod = "pos.sale.payment_method.invalid";
    public const string InvalidSaleActor = "pos.sale.actor.invalid";
    public const string SaleRequiresAtLeastOneLine = "pos.sale.lines.required";
    public const string SaleTotalTooLarge = "pos.sale.total.too_large";
    public const string InvalidSaleAmountTendered = "pos.sale.amount_tendered.invalid";
    public const string SaleAmountTenderedBelowTotal = "pos.sale.amount_tendered.below_total";
    public const string InvalidSaleGCashReference = "pos.sale.gcash_reference.invalid";
    public const string InvalidSaleVoidReason = "pos.sale.void_reason.invalid";
    public const string SaleAwaitingPaymentCannotFinalize =
        "pos.sale.awaiting_payment.cannot_finalize";
    public const string SaleNotAwaitingPayment = "pos.sale.not_awaiting_payment";

    public const string InvalidPaymentAttemptId = "pos.payment_attempt.id.invalid";
    public const string InvalidPaymentAttemptMethod = "pos.payment_attempt.method.invalid";
    public const string InvalidPaymentAttemptStatusTransition =
        "pos.payment_attempt.status.invalid_transition";
    public const string InvalidPaymentAttemptAmount = "pos.payment_attempt.amount.invalid";
    public const string InvalidPaymentAttemptCurrency = "pos.payment_attempt.currency.invalid";
    public const string InvalidPaymentAttemptIdempotencyKey =
        "pos.payment_attempt.idempotency_key.invalid";
    public const string InvalidPaymentAttemptProviderReference =
        "pos.payment_attempt.provider_reference.invalid";
    public const string InvalidPaymentAttemptExternalReference =
        "pos.payment_attempt.external_reference.invalid";
    public const string InvalidPaymentAttemptUrl = "pos.payment_attempt.url.invalid";
    public const string InvalidPaymentAttemptQr = "pos.payment_attempt.qr.invalid";
    public const string InvalidPaymentAttemptFailure = "pos.payment_attempt.failure.invalid";
    public const string InvalidPaymentAttemptMetadata = "pos.payment_attempt.metadata.invalid";
    public const string InvalidPaymentAttemptVerification =
        "pos.payment_attempt.verification.invalid";
    public const string InvalidPaymentAttemptActor = "pos.payment_attempt.actor.invalid";
    public const string InvalidPaymentAttemptTime = "pos.payment_attempt.time.invalid";
    public const string DuplicatePaymentAttemptExternalReference =
        "pos.payment_attempt.external_reference.duplicate";
    public const string PaymentAttemptNotFound = "pos.payment_attempt.not_found";
    public const string PaymentWebhookSignatureInvalid =
        "pos.payment_webhook.signature.invalid";
    public const string PaymentSimulatorDisabled = "pos.payment_simulator.disabled";

    public const string InvalidSaleLineQuantity = "pos.sale_line.quantity.invalid";
    public const string InvalidSaleLineUnitPrice = "pos.sale_line.unit_price.invalid";
    public const string InvalidSaleLineNameSnapshot = "pos.sale_line.name.invalid";
    public const string SaleUtangCustomerRequired = "pos.sale.utang.customer_required";
    public const string SaleUtangLinkageInvalid = "pos.sale.utang.linkage_invalid";
    public const string SaleUtangTotalMustBePositive = "pos.sale.utang.total_must_be_positive";
    public const string SaleCashMustNotLinkCredit = "pos.sale.cash_must_not_link_credit";

    public const string InvalidInventoryAccountId = "pos.inventory.account.id.invalid";
    public const string InvalidInventoryLotId = "pos.inventory.lot.id.invalid";
    public const string InvalidInventoryLotNumber = "pos.inventory.lot_number.invalid";
    public const string InventoryExpirationRequired = "pos.inventory.expiration.required";
    public const string InventoryLotMismatch = "pos.inventory.lot.mismatch";
    public const string InvalidExpirationWarningDays = "pos.inventory.expiration_warning_days.invalid";
    public const string InvalidStockMovementId = "pos.inventory.movement.id.invalid";
    public const string InvalidInventoryMovementType = "pos.inventory.movement_type.invalid";
    public const string InvalidInventorySourceType = "pos.inventory.source_type.invalid";
    public const string InvalidInventoryQuantity = "pos.inventory.quantity.invalid";
    public const string InventoryNotTracked = "pos.inventory.not_tracked";
    public const string InventoryAlreadyTracked = "pos.inventory.already_tracked";
    public const string InventoryDisableRequiresZero = "pos.inventory.disable_requires_zero";
    public const string InventoryInsufficientStock = "pos.inventory.insufficient_stock";
    public const string InventoryAdjustmentReasonRequired = "pos.inventory.adjustment_reason_required";
    public const string InventoryReorderLevelInvalid = "pos.inventory.reorder_level.invalid";
    public const string InventoryReorderQuantityInvalid = "pos.inventory.reorder_quantity.invalid";
    public const string InvalidInventoryReorderChangeId = "pos.inventory.reorder_change.id.invalid";
    public const string InvalidInventoryReorderReason = "pos.inventory.reorder.reason.invalid";
    public const string InvalidInventoryReorderActor = "pos.inventory.reorder.actor.invalid";
    public const string InventoryReorderUnchanged = "pos.inventory.reorder.unchanged";
    public const string InvalidStockCountId = "pos.stock_count.id.invalid";
    public const string InvalidStockCountLineId = "pos.stock_count_line.id.invalid";
    public const string InvalidStockCountNumber = "pos.stock_count.number.invalid";
    public const string InvalidStockCountStatus = "pos.stock_count.status.invalid";
    public const string InvalidStockCountStatusTransition = "pos.stock_count.status.invalid_transition";
    public const string InvalidStockCountLine = "pos.stock_count.line.invalid";
    public const string InvalidStockCountNotes = "pos.stock_count.notes.invalid";
    public const string StockCountRequiresLines = "pos.stock_count.lines.required";
    public const string StockCountDuplicateProduct = "pos.stock_count.duplicate_product";
    public const string StockCountProductNotFound = "pos.stock_count.product.not_found";
    public const string StockCountProductNotTracked = "pos.stock_count.product.not_tracked";
    public const string StockCountCountedQuantityRequired = "pos.stock_count.counted_quantity.required";
    public const string StockCountNumberConflict = "pos.stock_count.number.conflict";
    public const string InventoryOpeningDuplicate = "pos.inventory.opening_duplicate";
    public const string InventoryUomChangeBlocked = "pos.inventory.uom_change_blocked";
    public const string InvalidBranchId = "pos.branch.id.invalid";
    public const string InvalidInventoryTransferId = "pos.inventory.transfer.id.invalid";
    public const string InvalidInventoryTransferLineId = "pos.inventory.transfer.line.id.invalid";
    public const string InvalidInventoryTransferNumber = "pos.inventory.transfer.number.invalid";
    public const string InvalidInventoryTransferStatus = "pos.inventory.transfer.status.invalid";
    public const string InvalidInventoryTransferStatusTransition = "pos.inventory.transfer.status.invalid_transition";
    public const string InvalidInventoryTransferQuantity = "pos.inventory.transfer.quantity.invalid";
    public const string InvalidInventoryTransferReceiveQty = "pos.inventory.transfer.receive_qty.invalid";
    public const string InvalidInventoryTransferLine = "pos.inventory.transfer.line.invalid";
    public const string InvalidInventoryTransferNotes = "pos.inventory.transfer.notes.invalid";
    public const string InvalidInventoryTransferDiscrepancyReason = "pos.inventory.transfer.discrepancy_reason.invalid";
    public const string InvalidInventoryTransferDiscrepancyNote = "pos.inventory.transfer.discrepancy_note.invalid";
    public const string InventoryTransferRequiresLines = "pos.inventory.transfer.lines.required";
    public const string InventoryTransferReceiveRequiresLines = "pos.inventory.transfer.receive.lines.required";
    public const string InventoryTransferDuplicateProduct = "pos.inventory.transfer.duplicate_product";
    public const string InventoryTransferSameBranch = "pos.inventory.transfer.same_branch";

    public const string InvalidExpenseCategoryId = "pos.expense_category.id.invalid";
    public const string InvalidExpenseCategoryName = "pos.expense_category.name.invalid";
    public const string InvalidExpenseCategoryStatus = "pos.expense_category.status.invalid";
    public const string InvalidExpenseCategoryStatusTransition = "pos.expense_category.status.invalid_transition";
    public const string ExpenseCategoryNotActive = "pos.expense_category.not_active";

    public const string InvalidExpenseId = "pos.expense.id.invalid";
    public const string InvalidExpenseNumber = "pos.expense.number.invalid";
    public const string InvalidExpenseStatus = "pos.expense.status.invalid";
    public const string InvalidExpenseStatusTransition = "pos.expense.status.invalid_transition";
    public const string InvalidExpensePaymentMethod = "pos.expense.payment_method.invalid";
    public const string InvalidExpenseActor = "pos.expense.actor.invalid";
    public const string InvalidExpenseAmount = "pos.expense.amount.invalid";
    public const string InvalidExpenseDescription = "pos.expense.description.invalid";
    public const string InvalidExpensePayee = "pos.expense.payee.invalid";
    public const string InvalidExpenseGCashReference = "pos.expense.gcash_reference.invalid";
    public const string InvalidExpenseVoidReason = "pos.expense.void_reason.invalid";

    public const string InvalidSupplierId = "pos.supplier.id.invalid";
    public const string InvalidSupplierCode = "pos.supplier.code.invalid";
    public const string InvalidSupplierName = "pos.supplier.name.invalid";
    public const string InvalidSupplierContactPerson = "pos.supplier.contact_person.invalid";
    public const string InvalidSupplierTelephone = "pos.supplier.telephone.invalid";
    public const string InvalidSupplierEmail = "pos.supplier.email.invalid";
    public const string InvalidSupplierAddress = "pos.supplier.address.invalid";
    public const string InvalidSupplierTaxNumber = "pos.supplier.tax_number.invalid";
    public const string InvalidSupplierNotes = "pos.supplier.notes.invalid";
    public const string InvalidSupplierStatus = "pos.supplier.status.invalid";
    public const string InvalidSupplierStatusTransition = "pos.supplier.status.invalid_transition";
    public const string SupplierNotActive = "pos.supplier.not_active";

    public const string InvalidPurchaseOrderId = "pos.purchase_order.id.invalid";
    public const string InvalidPurchaseOrderLineId = "pos.purchase_order_line.id.invalid";
    public const string InvalidGoodsReceiptId = "pos.goods_receipt.id.invalid";
    public const string InvalidGoodsReceiptLineId = "pos.goods_receipt_line.id.invalid";
    public const string InvalidPurchaseOrderNumber = "pos.purchase_order.number.invalid";
    public const string InvalidGoodsReceiptNumber = "pos.goods_receipt.number.invalid";
    public const string InvalidPurchaseOrderStatus = "pos.purchase_order.status.invalid";
    public const string InvalidPurchaseOrderStatusTransition = "pos.purchase_order.status.invalid_transition";
    public const string InvalidPurchaseOrderQuantity = "pos.purchase_order.quantity.invalid";
    public const string InvalidPurchaseUnitCost = "pos.purchase_order.unit_cost.invalid";
    public const string InvalidPurchaseOrderLine = "pos.purchase_order.line.invalid";
    public const string InvalidPurchaseOrderNotes = "pos.purchase_order.notes.invalid";
    public const string InvalidPurchaseSupplierReference = "pos.purchase_order.supplier_reference.invalid";
    public const string InvalidPurchaseExpectedDeliveryDate = "pos.purchase_order.expected_delivery.invalid";
    public const string PurchaseOrderRequiresLines = "pos.purchase_order.lines.required";
    public const string PurchaseOrderDuplicateProduct = "pos.purchase_order.duplicate_product";
    public const string PurchaseReceiveRequiresLines = "pos.purchase_order.receive.lines_required";
    public const string InvalidPurchaseReceiveQuantity = "pos.purchase_order.receive.quantity.invalid";
    public const string PurchaseOverReceipt = "pos.purchase_order.receive.over_receipt";
    public const string InvalidGoodsReceiptLine = "pos.goods_receipt.line.invalid";
    public const string InvalidGoodsReceiptNotes = "pos.goods_receipt.notes.invalid";

    public const string InvalidCashierShiftId = "pos.cashier_shift.id.invalid";
    public const string InvalidCashierShiftMovementId = "pos.cashier_shift_movement.id.invalid";
    public const string InvalidCashierShiftNumber = "pos.cashier_shift.number.invalid";
    public const string InvalidCashierShiftStatus = "pos.cashier_shift.status.invalid";
    public const string InvalidCashierShiftStatusTransition = "pos.cashier_shift.status.invalid_transition";
    public const string InvalidCashierShiftOpeningCash = "pos.cashier_shift.opening_cash.invalid";
    public const string InvalidCashierShiftClosingCash = "pos.cashier_shift.closing_cash.invalid";
    public const string InvalidCashCountMode = "pos.operational_setup.cash_count_mode.invalid";
    public const string CashierShiftOpeningCashCountRequired = "pos.cashier_shift.opening_cash_count.required";
    public const string CashierShiftClosingCashCountRequired = "pos.cashier_shift.closing_cash_count.required";
    public const string InvalidCashierShiftClosingNotes = "pos.cashier_shift.closing_notes.invalid";
    public const string InvalidCashierShiftMovementAmount = "pos.cashier_shift_movement.amount.invalid";
    public const string InvalidCashierShiftMovementReason = "pos.cashier_shift_movement.reason.invalid";
    public const string InvalidCashierShiftMovementReference = "pos.cashier_shift_movement.reference.invalid";
    public const string CashierShiftCancelBlockedByActivity = "pos.cashier_shift.cancel.blocked_by_activity";
    public const string CashierShiftExpectedCashNegative = "pos.cashier_shift.expected_cash.negative";
    public const string SaleCashierShiftRequired = "pos.sale.cashier_shift.required";
    public const string SaleVoidBlockedByReturns = "pos.sale.void.blocked_by_returns";

    public const string InvalidSaleReturnId = "pos.sale_return.id.invalid";
    public const string InvalidSaleReturnLineId = "pos.sale_return_line.id.invalid";
    public const string InvalidSaleReturnNumber = "pos.sale_return.number.invalid";
    public const string InvalidSaleReturnReason = "pos.sale_return.reason.invalid";
    public const string InvalidSaleReturnLine = "pos.sale_return_line.invalid";
    public const string InvalidSaleReturnRefundAmount = "pos.sale_return.refund_amount.invalid";
    public const string InvalidSaleReturnRestockDisposition = "pos.sale_return.restock_disposition.invalid";
    public const string SaleReturnRequiresAtLeastOneLine = "pos.sale_return.lines.required";
    public const string SaleReturnOrganizationMismatch = "pos.sale_return.organization_mismatch";
    public const string SaleReturnSaleNotReturnable = "pos.sale_return.sale_not_returnable";
    public const string SaleReturnQuantityExceedsRefundable = "pos.sale_return.quantity.exceeds_refundable";
    public const string SaleReturnDuplicateSaleLine = "pos.sale_return.duplicate_sale_line";
    public const string SaleReturnCashShiftRequired = "pos.sale_return.cash_shift.required";
    public const string SaleReturnNonCashMustNotLinkShift = "pos.sale_return.non_cash_must_not_link_shift";
    public const string SaleReturnUtangOutstandingInsufficient = "pos.sale_return.utang.outstanding_insufficient";

    public const string InvalidPosRoleAssignmentId = "pos.role_assignment.id.invalid";
    public const string InvalidPosRole = "pos.role.invalid";
    public const string InvalidPosRoleRevocationReason = "pos.role_assignment.revocation_reason.invalid";
    public const string PosRoleAssignmentAlreadyRevoked = "pos.role_assignment.already_revoked";
    public const string PosRoleAssignmentConflict = "pos.role_assignment.conflict";
    public const string PosRoleRequired = "pos.role.required";
    public const string PosRoleDenied = "pos.role.denied";
    public const string PosRoleLastOwnerProtected = "pos.role.last_owner.protected";
    public const string PosRoleBootstrapRequired = "pos.role.bootstrap.required";
    public const string PosRoleAssignForbidden = "pos.role.assign.forbidden";
    public const string CreditReduceWouldMakeOutstandingNegative = "pos.credit_entry.reduce.outstanding_negative";

    public const string InvalidRegisterId = "pos.register.id.invalid";
    public const string InvalidRegisterCode = "pos.register.code.invalid";
    public const string InvalidRegisterName = "pos.register.name.invalid";
    public const string InvalidRegisterDescription = "pos.register.description.invalid";
    public const string InvalidRegisterStatus = "pos.register.status.invalid";
    public const string InvalidRegisterStatusTransition = "pos.register.status.invalid_transition";
    public const string RegisterNotActive = "pos.register.not_active";
    public const string RegisterDeactivateBlockedByOpenShift = "pos.register.deactivate.blocked_by_open_shift";
    public const string RegisterRequired = "pos.register.required";
    public const string CashierShiftRegisterRequired = "pos.cashier_shift.register.required";
    public const string CashierShiftRegisterConflict = "pos.cashier_shift.register.open_conflict";
    public const string SaleRegisterRequired = "pos.sale.register.required";

    public const string InvalidOperationalSetupStoreDisplayName = "pos.operational_setup.store_display_name.invalid";
    public const string InvalidOperationalSetupCurrencyCode = "pos.operational_setup.currency_code.invalid";
    public const string InvalidOperationalSetupTaxRate = "pos.operational_setup.tax_rate.invalid";
    public const string InvalidOperationalSetupReceiptHeader = "pos.operational_setup.receipt_header.invalid";
    public const string InvalidOperationalSetupReceiptFooter = "pos.operational_setup.receipt_footer.invalid";
    public const string InvalidOperationalSetupBusinessAddress = "pos.operational_setup.business_address.invalid";
    public const string InvalidOperationalSetupContactPhone = "pos.operational_setup.contact_phone.invalid";
    public const string OperationalSetupIncomplete = "pos.operational_setup.incomplete";
    public const string OperationalSetupDefaultRegisterRequired = "pos.operational_setup.default_register.required";

    public const string InvalidCatalogImportJobId = "pos.catalog_import.job.id.invalid";
    public const string InvalidCatalogImportItemId = "pos.catalog_import.item.id.invalid";
    public const string InvalidCatalogImportJob = "pos.catalog_import.job.invalid";
    public const string InvalidCatalogImportItem = "pos.catalog_import.item.invalid";
    public const string CatalogImportEmpty = "pos.catalog_import.empty";
    public const string CatalogImportTooLarge = "pos.catalog_import.too_large";
    public const string InvalidCatalogImportStatusTransition = "pos.catalog_import.status.invalid_transition";
    public const string InvalidCatalogSource = "pos.catalog.source.invalid";
}
