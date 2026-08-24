namespace ExItS.Platform.Application.Admin;

public static class ActionCenterCategories
{
    public const string Payment = "payment";
    public const string Subscription = "subscription";
    public const string Usage = "usage";
    public const string Account = "account";
    public const string Job = "job";
    public const string Health = "health";
    public const string Organization = "organization";
}

public static class ActionCenterSeverities
{
    public const string Warning = "warning";
    public const string Danger = "danger";
    public const string Neutral = "neutral";
}

public sealed record ActionCenterItemDto(
    string Id,
    string Category,
    string Severity,
    string Title,
    string Reason,
    Guid? OrganizationId,
    string? OrganizationDisplayName,
    string? ProductCode,
    Guid? SubscriptionId,
    Guid? PaymentId,
    Guid? JobId,
    DateTimeOffset? OccurredAtUtc);

public sealed record ActionCenterResponseDto(
    IReadOnlyList<ActionCenterItemDto> Items,
    int TotalCount);
