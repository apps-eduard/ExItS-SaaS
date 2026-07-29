namespace ExItS.Platform.Infrastructure.Persistence.Subscriptions;

internal sealed class SubscriptionRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public Guid PlanVersionId { get; set; }
    public Guid? TrialDefinitionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset? TrialStartUtc { get; set; }
    public DateTimeOffset? TrialEndUtc { get; set; }
    public DateTimeOffset? PaidPeriodStartUtc { get; set; }
    public DateTimeOffset? PaidPeriodEndUtc { get; set; }
    public DateTimeOffset? GracePeriodEndUtc { get; set; }
    public DateTimeOffset? SuspendedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public DateTimeOffset? PastDueAtUtc { get; set; }
    public DateTimeOffset? ExpiredAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int AggregateVersion { get; set; }
    public uint Xmin { get; set; }
}
