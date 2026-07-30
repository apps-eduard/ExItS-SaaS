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
    public const string InvalidSaleLineQuantity = "pos.sale_line.quantity.invalid";
    public const string InvalidSaleLineUnitPrice = "pos.sale_line.unit_price.invalid";
    public const string InvalidSaleLineNameSnapshot = "pos.sale_line.name.invalid";
    public const string SaleUtangCustomerRequired = "pos.sale.utang.customer_required";
    public const string SaleUtangLinkageInvalid = "pos.sale.utang.linkage_invalid";
    public const string SaleUtangTotalMustBePositive = "pos.sale.utang.total_must_be_positive";
    public const string SaleCashMustNotLinkCredit = "pos.sale.cash_must_not_link_credit";

    public const string InvalidInventoryAccountId = "pos.inventory.account.id.invalid";
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
}
