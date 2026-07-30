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
}
