namespace ExItS.Platform.Application.Admin;

public static class BillingIssueTypes
{
    public const string PendingPayment = "pending-payment";
    public const string RejectedPayment = "rejected-payment";
    public const string VoidedPayment = "voided-payment";
    public const string PastDueSubscription = "past-due-subscription";
    public const string GracePeriodSubscription = "grace-period-subscription";
}

public static class BillingIssueSeverities
{
    public const string Warning = "warning";
    public const string Danger = "danger";
}

public sealed record BillingOperationsSummaryDto(
    int PendingPaymentCount,
    int RejectedPaymentCount,
    int VoidedPaymentCount,
    int ConfirmedPaymentCount,
    int PastDueSubscriptionCount,
    int GracePeriodSubscriptionCount);

public sealed record BillingIssueDto(
    string IssueType,
    string Severity,
    string Summary,
    string? Detail,
    Guid? OrganizationId,
    string? OrganizationDisplayName,
    string? ProductCode,
    string? ProductDisplayName,
    Guid? SubscriptionId,
    Guid? PaymentId,
    DateTimeOffset? OccurredAtUtc);
