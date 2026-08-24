using ExItS.Platform.Domain.Authorization;

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

/// <summary>
/// Permission-derived inclusion flags for Action Center composition.
/// Categories are independent — lacking one never denies the whole response.
/// </summary>
public sealed record ActionCenterAccessScope(
    bool IncludeUsage,
    bool IncludeSubscriptions,
    bool IncludePayments,
    bool IncludeAccounts,
    bool IncludeJobs,
    bool IncludeHealth)
{
    public static ActionCenterAccessScope FromPermissions(IReadOnlySet<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        var isPlatformAdministrator = permissions.Contains(PlatformPermission.ManagePlatformSettings);
        return new ActionCenterAccessScope(
            IncludeUsage: permissions.Contains(PlatformPermission.ViewPortfolio),
            IncludeSubscriptions: permissions.Contains(PlatformPermission.ManageSubscriptions),
            IncludePayments: permissions.Contains(PlatformPermission.ManageManualPayments),
            IncludeAccounts: permissions.Contains(PlatformPermission.ManagePlatformUsers),
            IncludeJobs: isPlatformAdministrator,
            IncludeHealth: isPlatformAdministrator);
    }

    public bool HasAnyCategory =>
        IncludeUsage || IncludeSubscriptions || IncludePayments || IncludeAccounts || IncludeJobs || IncludeHealth;
}
