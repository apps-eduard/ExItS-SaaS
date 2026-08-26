using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Admin;

public sealed class BillingOperationsQueryService
{
    private const int IssueSourcePageSize = 50;

    private readonly IAdminPortfolioReadStore _store;
    private readonly SaaSPaymentQueryService _payments;
    private readonly SubscriptionQueryService _subscriptions;

    public BillingOperationsQueryService(
        IAdminPortfolioReadStore store,
        SaaSPaymentQueryService payments,
        SubscriptionQueryService subscriptions)
    {
        _store = store;
        _payments = payments;
        _subscriptions = subscriptions;
    }

    public async Task<BillingOperationsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _store.CountPaymentsByStatusAsync(SaaSPaymentStatus.PendingConfirmation, cancellationToken)
            .ConfigureAwait(false);
        var rejected = await _store.CountPaymentsByStatusAsync(SaaSPaymentStatus.Rejected, cancellationToken)
            .ConfigureAwait(false);
        var voided = await _store.CountPaymentsByStatusAsync(SaaSPaymentStatus.Voided, cancellationToken)
            .ConfigureAwait(false);
        var confirmed = await _store.CountPaymentsByStatusAsync(SaaSPaymentStatus.Confirmed, cancellationToken)
            .ConfigureAwait(false);
        var pastDue = await _store.CountSubscriptionsByStatusAsync(SubscriptionStatus.PastDue, cancellationToken)
            .ConfigureAwait(false);
        var grace = await _store.CountSubscriptionsByStatusAsync(SubscriptionStatus.GracePeriod, cancellationToken)
            .ConfigureAwait(false);

        return new BillingOperationsSummaryDto(
            pending,
            rejected,
            voided,
            confirmed,
            pastDue,
            grace);
    }

    public async Task<PagedResult<BillingIssueDto>> ListIssuesAsync(
        string? issueType,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var issues = new List<BillingIssueDto>();
        var normalizedType = issueType?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedType)
            || normalizedType == BillingIssueTypes.PendingPayment)
        {
            var pending = await _payments
                .ListByStatusAsync(SaaSPaymentStatus.PendingConfirmation, 1, IssueSourcePageSize, cancellationToken)
                .ConfigureAwait(false);
            issues.AddRange(pending.Items.Select(MapPendingPayment));
        }

        if (string.IsNullOrWhiteSpace(normalizedType)
            || normalizedType == BillingIssueTypes.RejectedPayment)
        {
            var rejected = await _payments
                .ListByStatusAsync(SaaSPaymentStatus.Rejected, 1, IssueSourcePageSize, cancellationToken)
                .ConfigureAwait(false);
            issues.AddRange(rejected.Items.Select(MapRejectedPayment));
        }

        if (string.IsNullOrWhiteSpace(normalizedType)
            || normalizedType == BillingIssueTypes.VoidedPayment)
        {
            var voided = await _payments
                .ListByStatusAsync(SaaSPaymentStatus.Voided, 1, IssueSourcePageSize, cancellationToken)
                .ConfigureAwait(false);
            issues.AddRange(voided.Items.Select(MapVoidedPayment));
        }

        if (string.IsNullOrWhiteSpace(normalizedType)
            || normalizedType == BillingIssueTypes.PastDueSubscription)
        {
            var pastDue = await _subscriptions
                .ListPastDueAsync(1, IssueSourcePageSize, cancellationToken)
                .ConfigureAwait(false);
            issues.AddRange(pastDue.Items.Select(MapPastDueSubscription));
        }

        if (string.IsNullOrWhiteSpace(normalizedType)
            || normalizedType == BillingIssueTypes.GracePeriodSubscription)
        {
            var grace = await _subscriptions
                .ListGracePeriodAsync(1, IssueSourcePageSize, cancellationToken)
                .ConfigureAwait(false);
            issues.AddRange(grace.Items.Select(MapGraceSubscription));
        }

        var ordered = issues
            .OrderByDescending(i => i.OccurredAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(i => i.Summary, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var pageNumber = Math.Max(page ?? 1, 1);
        var pageItems = ordered.Skip(skip).Take(take).ToList();
        return new PagedResult<BillingIssueDto>(pageItems, ordered.Count, pageNumber, take);
    }

    private static BillingIssueDto MapPendingPayment(SaaSPaymentDto payment) =>
        new(
            BillingIssueTypes.PendingPayment,
            BillingIssueSeverities.Warning,
            "Manual payment awaiting confirmation",
            payment.ExternalReference,
            payment.OrganizationId,
            null,
            payment.ProductCode,
            null,
            payment.SubscriptionId,
            payment.Id,
            payment.UpdatedAtUtc);

    private static BillingIssueDto MapRejectedPayment(SaaSPaymentDto payment) =>
        new(
            BillingIssueTypes.RejectedPayment,
            BillingIssueSeverities.Danger,
            "Manual payment rejected",
            payment.RejectionReason,
            payment.OrganizationId,
            null,
            payment.ProductCode,
            null,
            payment.SubscriptionId,
            payment.Id,
            payment.RejectedAtUtc ?? payment.UpdatedAtUtc);

    private static BillingIssueDto MapVoidedPayment(SaaSPaymentDto payment) =>
        new(
            BillingIssueTypes.VoidedPayment,
            BillingIssueSeverities.Danger,
            "Manual payment voided",
            payment.VoidReason,
            payment.OrganizationId,
            null,
            payment.ProductCode,
            null,
            payment.SubscriptionId,
            payment.Id,
            payment.VoidedAtUtc ?? payment.UpdatedAtUtc);

    private static BillingIssueDto MapPastDueSubscription(SubscriptionDto subscription) =>
        new(
            BillingIssueTypes.PastDueSubscription,
            BillingIssueSeverities.Danger,
            "Subscription past due",
            subscription.PlanDisplayName ?? subscription.PlanKey,
            subscription.OrganizationId,
            subscription.OrganizationDisplayName,
            subscription.ProductCode,
            subscription.ProductDisplayName,
            subscription.Id,
            null,
            subscription.PastDueAtUtc ?? subscription.UpdatedAtUtc);

    private static BillingIssueDto MapGraceSubscription(SubscriptionDto subscription) =>
        new(
            BillingIssueTypes.GracePeriodSubscription,
            BillingIssueSeverities.Warning,
            "Subscription in grace period",
            subscription.PlanDisplayName ?? subscription.PlanKey,
            subscription.OrganizationId,
            subscription.OrganizationDisplayName,
            subscription.ProductCode,
            subscription.ProductDisplayName,
            subscription.Id,
            null,
            subscription.GracePeriodEndUtc ?? subscription.UpdatedAtUtc);
}
