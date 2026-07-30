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
}
