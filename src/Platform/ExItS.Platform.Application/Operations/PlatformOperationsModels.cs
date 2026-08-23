using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;

namespace ExItS.Platform.Application.Operations;

public static class UsageLimitUsageStatuses
{
    public const string Measured = "Measured";
    public const string NotInstrumented = "NotInstrumented";
    public const string Unavailable = "Unavailable";
}

public sealed record UsageLimitRowDto(
    Guid OrganizationId,
    string OrganizationDisplayName,
    string ProductCode,
    string? ProductDisplayName,
    Guid SubscriptionId,
    string SubscriptionStatus,
    string? PlanDisplayName,
    string? PlanKey,
    string FeatureCode,
    bool EntitlementEnabled,
    int? NumericLimit,
    bool Unlimited,
    int? Usage,
    string UsageStatus,
    decimal? UsagePercent);

public static class PlatformSupportLookupModes
{
    public const string Organization = "organization";
    public const string PublicOrganizationId = "publicOrganizationId";
    public const string UserEmail = "userEmail";
    public const string PublicUserId = "publicUserId";
    public const string SubscriptionId = "subscriptionId";
    public const string PaymentId = "paymentId";
    public const string PaymentReference = "paymentReference";
    public const string DeviceId = "deviceId";
}

public sealed record PlatformSupportLookupRequest(
    string Mode,
    string Query,
    string? PaymentMethod = null);

public sealed record PlatformSupportCommercialSubscriptionDto(
    Guid Id,
    string ProductCode,
    string Status,
    string? PlanDisplayName,
    string? PlanKey);

public sealed record PlatformSupportCommercialEntitlementDto(
    Guid Id,
    string ProductCode,
    string SubscriptionStatus,
    string? ProductDisplayName,
    int? SnapshotVersion);

public sealed record PlatformSupportCommercialPaymentDto(
    Guid Id,
    string ProductCode,
    string Status,
    string? ExternalReference,
    DateTimeOffset? PaidAtUtc);

public sealed record PlatformSupportLookupResponse(
    PlatformOrganizationDto? Organization,
    PlatformUserDto? User,
    SubscriptionDto? Subscription,
    SaaSPaymentDto? Payment,
    PosDeviceDto? Device,
    IReadOnlyList<PlatformSupportCommercialSubscriptionDto> Subscriptions,
    IReadOnlyList<PlatformSupportCommercialEntitlementDto> LatestEntitlements,
    IReadOnlyList<PlatformSupportCommercialPaymentDto> Payments,
    IReadOnlyList<PosDeviceDto> Devices,
    IReadOnlyList<AuditRecordDto> RecentAudit);

public static class PlatformBackgroundJobSources
{
    public const string CatalogImport = "catalog-import";
}

public sealed record PlatformBackgroundJobDto(
    Guid Id,
    string Source,
    string JobType,
    string Status,
    int? TotalCount,
    int? ProcessedCount,
    int? ImportedCount,
    int? SkippedCount,
    int? FailedCount,
    string? CurrentStage,
    string? FailureSummary,
    DateTimeOffset? RequestedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    int? AttemptCount,
    string? DisplayName);

public sealed record PlatformBackgroundJobDetailDto(
    PlatformBackgroundJobDto Summary,
    string? RequestedBy,
    string? FileFormat,
    long? FileSizeBytes,
    string? IdempotencyKey,
    DateTimeOffset? LastHeartbeatAtUtc,
    string? PreviewSummary);
