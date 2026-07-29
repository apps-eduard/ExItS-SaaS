using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;

namespace ExItS.Platform.Application.Admin;

public sealed record PortfolioSummaryDto(
    int ActiveProductCount,
    int PublishedPlanVersionCount,
    int OrganizationCount,
    int TrialingSubscriptionCount,
    int ActiveSubscriptionCount,
    int GracePeriodSubscriptionCount,
    int PastDueSubscriptionCount,
    int SuspendedSubscriptionCount,
    int PendingManualPaymentCount,
    int LatestEntitlementSnapshotCount,
    IReadOnlyList<string> PartialFailures);

public sealed record ProductOverviewDto(
    ProductDto Product,
    IReadOnlyList<FeatureDefinitionDto> Features,
    IReadOnlyList<PlanDto> Plans,
    IReadOnlyList<PlanVersionDto> PublishedPlanVersions,
    IReadOnlyList<TrialDefinitionDto> Trials);

public sealed record OrganizationCommercialSummaryDto(
    PlatformOrganizationDto Organization,
    IReadOnlyList<SubscriptionDto> Subscriptions,
    IReadOnlyList<SaaSPaymentDto> Payments,
    IReadOnlyList<EntitlementLatestSummaryDto> LatestEntitlements);

public sealed record EntitlementLatestSummaryDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid SubscriptionId,
    string SubscriptionStatus,
    int SnapshotVersion,
    int SchemaVersion,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset RefreshByUtc,
    DateTimeOffset? ExpiresAtUtc,
    bool InGracePeriod);
