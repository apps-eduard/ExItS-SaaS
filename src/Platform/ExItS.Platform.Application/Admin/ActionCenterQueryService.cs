using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Operations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Admin;

public sealed class ActionCenterQueryService
{
    private const int MaxItems = 24;
    private const decimal UsageWarningPercent = 80m;

    private readonly IAdminPortfolioReadStore _store;
    private readonly SaaSPaymentQueryService _payments;
    private readonly SubscriptionQueryService _subscriptions;
    private readonly PlatformUserQueryService _users;
    private readonly PlatformUsageLimitsQueryService _usageLimits;
    private readonly PlatformBackgroundJobsQueryService _jobs;
    private readonly ISystemHealthQueryService _health;

    public ActionCenterQueryService(
        IAdminPortfolioReadStore store,
        SaaSPaymentQueryService payments,
        SubscriptionQueryService subscriptions,
        PlatformUserQueryService users,
        PlatformUsageLimitsQueryService usageLimits,
        PlatformBackgroundJobsQueryService jobs,
        ISystemHealthQueryService health)
    {
        _store = store;
        _payments = payments;
        _subscriptions = subscriptions;
        _users = users;
        _usageLimits = usageLimits;
        _jobs = jobs;
        _health = health;
    }

    public async Task<ActionCenterResponseDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<ActionCenterItemDto>();

        var pendingCount = await _store
            .CountPaymentsByStatusAsync(SaaSPaymentStatus.PendingConfirmation, cancellationToken)
            .ConfigureAwait(false);
        if (pendingCount > 0)
        {
            items.Add(new ActionCenterItemDto(
                "summary-pending-payments",
                ActionCenterCategories.Payment,
                ActionCenterSeverities.Warning,
                "Pending manual payments",
                $"{pendingCount} payment(s) awaiting confirmation",
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        var pastDueCount = await _store
            .CountSubscriptionsByStatusAsync(SubscriptionStatus.PastDue, cancellationToken)
            .ConfigureAwait(false);
        if (pastDueCount > 0)
        {
            items.Add(new ActionCenterItemDto(
                "summary-past-due-subscriptions",
                ActionCenterCategories.Subscription,
                ActionCenterSeverities.Danger,
                "Past-due subscriptions",
                $"{pastDueCount} subscription(s) need billing attention",
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        var graceCount = await _store
            .CountSubscriptionsByStatusAsync(SubscriptionStatus.GracePeriod, cancellationToken)
            .ConfigureAwait(false);
        if (graceCount > 0)
        {
            items.Add(new ActionCenterItemDto(
                "summary-grace-subscriptions",
                ActionCenterCategories.Subscription,
                ActionCenterSeverities.Warning,
                "Grace-period subscriptions",
                $"{graceCount} subscription(s) are in grace period",
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        var rejectedCount = await _store
            .CountPaymentsByStatusAsync(SaaSPaymentStatus.Rejected, cancellationToken)
            .ConfigureAwait(false);
        if (rejectedCount > 0)
        {
            items.Add(new ActionCenterItemDto(
                "summary-rejected-payments",
                ActionCenterCategories.Payment,
                ActionCenterSeverities.Danger,
                "Rejected payments",
                $"{rejectedCount} payment(s) were rejected",
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        var unassigned = await _users
            .ListAsync(
                status: null,
                search: null,
                page: 1,
                pageSize: 1,
                directoryFilter: UserDirectoryFilter.Unassigned,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        if (unassigned.TotalCount > 0)
        {
            items.Add(new ActionCenterItemDto(
                "summary-unassigned-accounts",
                ActionCenterCategories.Account,
                ActionCenterSeverities.Warning,
                "Accounts needing review",
                $"{unassigned.TotalCount} unassigned account(s)",
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        var pendingPayments = await _payments
            .ListByStatusAsync(SaaSPaymentStatus.PendingConfirmation, 1, 5, cancellationToken)
            .ConfigureAwait(false);
        foreach (var payment in pendingPayments.Items)
        {
            items.Add(new ActionCenterItemDto(
                $"payment-pending-{payment.Id:D}",
                ActionCenterCategories.Payment,
                ActionCenterSeverities.Warning,
                "Pending payment confirmation",
                payment.ExternalReference,
                payment.OrganizationId,
                null,
                payment.ProductCode,
                payment.SubscriptionId,
                payment.Id,
                null,
                payment.UpdatedAtUtc));
        }

        var pastDue = await _subscriptions
            .ListPastDueAsync(1, 5, cancellationToken)
            .ConfigureAwait(false);
        foreach (var subscription in pastDue.Items)
        {
            items.Add(new ActionCenterItemDto(
                $"subscription-past-due-{subscription.Id:D}",
                ActionCenterCategories.Subscription,
                ActionCenterSeverities.Danger,
                "Past-due subscription",
                subscription.ProductDisplayName ?? subscription.ProductCode,
                subscription.OrganizationId,
                subscription.OrganizationDisplayName,
                subscription.ProductCode,
                subscription.Id,
                null,
                null,
                subscription.PastDueAtUtc ?? subscription.UpdatedAtUtc));
        }

        var usageRows = await _usageLimits
            .ListAsync(null, null, 1, 100, cancellationToken)
            .ConfigureAwait(false);
        foreach (var row in usageRows.Items.Where(IsUsageWarning))
        {
            items.Add(new ActionCenterItemDto(
                $"usage-{row.OrganizationId:D}-{row.ProductCode}-{row.FeatureCode}",
                ActionCenterCategories.Usage,
                row.UsagePercent is >= 100 ? ActionCenterSeverities.Danger : ActionCenterSeverities.Warning,
                "Usage approaching plan limit",
                $"{row.FeatureCode}: {row.Usage}/{row.NumericLimit}",
                row.OrganizationId,
                row.OrganizationDisplayName,
                row.ProductCode,
                row.SubscriptionId,
                null,
                null,
                null));
        }

        var failedJobs = await _jobs
            .ListAsync(CatalogImportJobStatus.Failed.ToString(), null, 1, 5, cancellationToken)
            .ConfigureAwait(false);
        foreach (var job in failedJobs.Items)
        {
            items.Add(new ActionCenterItemDto(
                $"job-failed-{job.Id:D}",
                ActionCenterCategories.Job,
                ActionCenterSeverities.Danger,
                "Background job failed",
                job.DisplayName ?? job.JobType,
                null,
                null,
                null,
                null,
                null,
                job.Id,
                job.CompletedAtUtc ?? job.RequestedAtUtc));
        }

        var health = await _health.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        if (health.OverallStatus is SystemHealthStatuses.Unhealthy or SystemHealthStatuses.Degraded)
        {
            items.Add(new ActionCenterItemDto(
                "health-overall",
                ActionCenterCategories.Health,
                health.OverallStatus == SystemHealthStatuses.Unhealthy
                    ? ActionCenterSeverities.Danger
                    : ActionCenterSeverities.Warning,
                "Platform health needs attention",
                $"Overall status: {health.OverallStatus}",
                null,
                null,
                null,
                null,
                null,
                null,
                null));
        }

        foreach (var service in health.Services.Where(s =>
                     s.Status is SystemHealthStatuses.Unhealthy or SystemHealthStatuses.Degraded))
        {
            items.Add(new ActionCenterItemDto(
                $"health-service-{service.Name}",
                ActionCenterCategories.Health,
                service.Status == SystemHealthStatuses.Unhealthy
                    ? ActionCenterSeverities.Danger
                    : ActionCenterSeverities.Warning,
                $"Service health: {service.Name}",
                service.Status,
                null,
                null,
                null,
                null,
                null,
                null,
                service.CheckedAtUtc));
        }

        var ordered = items
            .OrderByDescending(i => SeverityRank(i.Severity))
            .ThenByDescending(i => i.OccurredAtUtc ?? DateTimeOffset.MinValue)
            .Take(MaxItems)
            .ToList();

        return new ActionCenterResponseDto(ordered, ordered.Count);
    }

    private static bool IsUsageWarning(UsageLimitRowDto row) =>
        row.UsageStatus == UsageLimitUsageStatuses.Measured
        && row.NumericLimit is > 0
        && row.Usage.HasValue
        && (row.UsagePercent is >= UsageWarningPercent || row.Usage >= row.NumericLimit);

    private static int SeverityRank(string severity) =>
        severity switch
        {
            ActionCenterSeverities.Danger => 2,
            ActionCenterSeverities.Warning => 1,
            _ => 0,
        };
}
