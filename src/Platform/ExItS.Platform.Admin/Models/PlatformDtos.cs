namespace ExItS.Platform.Admin.Models;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record ProductDto(
    Guid Id,
    string Code,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record FeatureDefinitionDto(
    string ProductCode,
    string FeatureCode,
    string DisplayName,
    string ValueType,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record FeatureGrantDto(string FeatureCode, bool Enabled, int? NumericLimit);

public sealed record PlanDto(
    Guid Id,
    string ProductCode,
    string Code,
    string DisplayName,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record PlanVersionDto(
    Guid Id,
    Guid PlanId,
    string ProductCode,
    int VersionNumber,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    string BillingPeriod,
    bool TrialEligible,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<FeatureGrantDto> Grants);

public sealed record TrialDefinitionDto(
    Guid Id,
    string ProductCode,
    Guid? PlanId,
    string DisplayName,
    long DurationTicks,
    string DurationIso,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<FeatureGrantDto> FeatureGrants,
    IReadOnlyList<FeatureGrantDto> PostExpiryFeatureGrants);

public sealed record ProductOverviewDto(
    ProductDto Product,
    IReadOnlyList<FeatureDefinitionDto> Features,
    IReadOnlyList<PlanDto> Plans,
    IReadOnlyList<PlanVersionDto> PublishedPlanVersions,
    IReadOnlyList<TrialDefinitionDto> Trials);

public sealed record OrganizationDto(
    Guid Id,
    string DisplayName,
    string Slug,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record SubscriptionDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid PlanId,
    Guid PlanVersionId,
    Guid? TrialDefinitionId,
    string Status,
    DateTimeOffset? TrialStartUtc,
    DateTimeOffset? TrialEndUtc,
    DateTimeOffset? PaidPeriodStartUtc,
    DateTimeOffset? PaidPeriodEndUtc,
    DateTimeOffset? GracePeriodEndUtc,
    DateTimeOffset? SuspendedAtUtc,
    DateTimeOffset? PastDueAtUtc,
    DateTimeOffset? CancelledAtUtc,
    DateTimeOffset? ExpiredAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version);

public sealed record PaymentDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid? SubscriptionId,
    decimal Amount,
    string CurrencyCode,
    string Method,
    string ExternalReference,
    string Status,
    DateTimeOffset PaidAtUtc,
    DateTimeOffset? ConfirmedAtUtc,
    string? ConfirmedBy,
    DateTimeOffset? RejectedAtUtc,
    string? RejectedBy,
    string? RejectionReason,
    DateTimeOffset? VoidedAtUtc,
    string? VoidedBy,
    string? VoidReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version);

public sealed record EntitlementGrantDto(
    string FeatureCode,
    bool Enabled,
    int? NumericLimit,
    string Source,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset? ExpiresAtUtc);

public sealed record EntitlementSnapshotDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid SubscriptionId,
    string PlanCode,
    int PlanVersionNumber,
    int SnapshotVersion,
    int SchemaVersion,
    string SubscriptionStatus,
    bool InGracePeriod,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset RefreshByUtc,
    DateTimeOffset? ExpiresAtUtc,
    int SourceAggregateVersion,
    IReadOnlyList<EntitlementGrantDto> Grants);

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

public sealed record FeatureOverrideDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    string FeatureCode,
    bool Enabled,
    int? NumericLimit,
    string Reason,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? ExpiresAtUtc,
    string Status,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedByUserId,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedByUserId,
    string? RevocationReason);

public sealed record OrganizationCommercialSummaryDto(
    OrganizationDto Organization,
    IReadOnlyList<SubscriptionDto> Subscriptions,
    IReadOnlyList<PaymentDto> Payments,
    IReadOnlyList<EntitlementLatestSummaryDto> LatestEntitlements);

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
