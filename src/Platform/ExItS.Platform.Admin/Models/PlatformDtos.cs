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

public sealed record PlatformUserDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Email,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    string? SuspensionReason);

public sealed record PlatformCredentialStatusDto(
    Guid UserId,
    bool HasPassword,
    bool EmailVerified,
    DateTimeOffset? EmailVerifiedAtUtc,
    bool IsLockedOut,
    DateTimeOffset? LockoutEndUtc,
    int FailedAccessCount,
    DateTimeOffset? PasswordChangedAtUtc);

public sealed record CredentialWorkflowAckDto(
    string Message,
    string? DebugToken,
    DateTimeOffset? ExpiresAtUtc);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string UsernameOrEmail);
public sealed record ResetPasswordRequest(string Token, string NewPassword);
public sealed record ConfirmEmailVerificationRequest(string Token);
public sealed record SetUserPasswordRequest(string Password);

public sealed record OrganizationMembershipDto(
    Guid Id,
    Guid OrganizationId,
    Guid UserId,
    string Role,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    DateTimeOffset? RemovedAtUtc,
    string? Reason,
    string? ActorReference);

public sealed record ProductAccessAssignmentDto(
    Guid Id,
    Guid UserId,
    Guid OrganizationId,
    Guid MembershipId,
    string ProductCode,
    string Status,
    DateTimeOffset GrantedAtUtc,
    string GrantedByActor,
    DateTimeOffset? RevokedAtUtc,
    string? RevokedByActor,
    string? Reason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EffectiveProductAccessResultDto(
    bool Allowed,
    string ReasonCode,
    Guid UserId,
    Guid OrganizationId,
    string ProductCode,
    Guid? MembershipId,
    Guid? AssignmentId,
    Guid? SubscriptionId,
    Guid? SnapshotId,
    DateTimeOffset EvaluatedAtUtc,
    string? SubscriptionStatus = null,
    IReadOnlyList<string>? EnabledFeatureCodes = null);

public sealed record CreatePlatformUserRequest(string Username, string DisplayName, string Email);
public sealed record UpdatePlatformUserRequest(string DisplayName, string Email);
public sealed record LifecycleReasonRequest(string? Reason);
public sealed record AddMemberRequest(Guid UserId, string Role);
public sealed record ChangeRoleRequest(string Role, string? ActorReference);
public sealed record MembershipLifecycleRequest(string? Reason, string? ActorReference);
public sealed record GrantProductAccessRequest(Guid UserId, string ProductCode, string GrantedByActor, string? Reason);
public sealed record RevokeProductAccessRequest(string RevokedByActor, string? Reason);

public sealed record StartTrialRequest(Guid PlanId, Guid PlanVersionId, Guid TrialDefinitionId);
public sealed record ActivateSubscriptionRequest(DateTimeOffset PeriodStartUtc, DateTimeOffset PeriodEndUtc);
public sealed record GracePeriodRequest(DateTimeOffset GracePeriodEndUtc);
public sealed record ReactivateSubscriptionRequest(DateTimeOffset? PeriodStartUtc, DateTimeOffset? PeriodEndUtc);

public sealed record CreateManualPaymentRequest(
    Guid OrganizationId,
    string ProductCode,
    decimal Amount,
    string CurrencyCode,
    string Method,
    string ExternalReference,
    DateTimeOffset PaidAtUtc);

public sealed record ConfirmPaymentRequest(string ConfirmedBy);
public sealed record RejectPaymentRequest(string RejectedBy, string Reason);
public sealed record VoidPaymentRequest(string VoidedBy, string Reason);
public sealed record ActivateSubscriptionForPaymentRequest(
    string ConfirmedBy,
    Guid SubscriptionId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc);

public sealed record PaymentActivationResultDto(PaymentDto Payment, SubscriptionDto Subscription);

public sealed record AuditRecordDto(
    Guid Id,
    DateTimeOffset OccurredAtUtc,
    string ActorIdentifier,
    string ActorType,
    string ActionCode,
    string TargetType,
    string TargetId,
    Guid? OrganizationId,
    string? ProductCode,
    string? CorrelationId,
    string Outcome,
    string? Reason,
    string? Summary);

public sealed record AuditQuery(
    DateTimeOffset? OccurredFromUtc = null,
    DateTimeOffset? OccurredToUtc = null,
    string? ActorIdentifier = null,
    string? ActionCode = null,
    Guid? OrganizationId = null,
    string? ProductCode = null,
    string? Outcome = null,
    string? CorrelationId = null,
    int Page = 1,
    int PageSize = 25);

public sealed record ResolvedPermissionsDto(
    string ActorIdentifier,
    string ActorType,
    Guid? PlatformUserId,
    Guid? OrganizationId,
    IReadOnlyList<string> Permissions);

public sealed record PlatformRoleAssignmentDto(
    Guid Id,
    Guid PlatformUserId,
    string Role,
    Guid? OrganizationId,
    string Status,
    string GrantedByActor,
    DateTimeOffset GrantedAtUtc,
    string? Reason,
    string? RevokedByActor,
    DateTimeOffset? RevokedAtUtc,
    string? RevokeReason);

public sealed record PlatformRoleCatalogEntryDto(string Role, IReadOnlyList<string> Permissions);
