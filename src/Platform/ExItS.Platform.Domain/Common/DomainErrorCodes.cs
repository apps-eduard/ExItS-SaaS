namespace ExItS.Platform.Domain.Common;

/// <summary>Stable domain error codes for identity and organization invariants.</summary>
public static class DomainErrorCodes
{
    public const string InvalidPlatformUserId = "platform.user.id.invalid";
    public const string InvalidPlatformOrganizationId = "platform.organization.id.invalid";
    public const string InvalidOrganizationMembershipId = "platform.membership.id.invalid";

    public const string InvalidDisplayName = "platform.display_name.invalid";
    public const string InvalidEmail = "platform.email.invalid";
    public const string InvalidOrganizationSlug = "platform.organization.slug.invalid";
    public const string InvalidProductCode = "platform.product_code.invalid";
    public const string InvalidUtcTimestamp = "platform.timestamp.invalid";

    public const string InvalidAccountStatusTransition = "platform.user.status.invalid_transition";
    public const string InvalidOrganizationStatusTransition = "platform.organization.status.invalid_transition";
    public const string InvalidMembershipStatusTransition = "platform.membership.status.invalid_transition";

    public const string UserNotActive = "platform.user.not_active";
    public const string OrganizationNotActive = "platform.organization.not_active";
    public const string MembershipNotActive = "platform.membership.not_active";
    public const string InvalidOrganizationRole = "platform.membership.role.invalid";

    public const string InvalidProductId = "platform.product.id.invalid";
    public const string InvalidPlanId = "platform.plan.id.invalid";
    public const string InvalidPlanVersionId = "platform.plan_version.id.invalid";
    public const string InvalidTrialDefinitionId = "platform.trial.id.invalid";
    public const string InvalidSubscriptionId = "platform.subscription.id.invalid";
    public const string InvalidEntitlementSnapshotId = "platform.entitlement_snapshot.id.invalid";
    public const string InvalidFeatureOverrideId = "platform.feature_override.id.invalid";

    public const string InvalidFeatureCode = "platform.feature_code.invalid";
    public const string InvalidPlanCode = "platform.plan_code.invalid";
    public const string InvalidFeatureValueType = "platform.feature.value_type.invalid";
    public const string InvalidEntitlementLimit = "platform.entitlement.limit.invalid";
    public const string InvalidTrialDuration = "platform.trial.duration.invalid";
    public const string InvalidPlanVersionNumber = "platform.plan_version.number.invalid";
    public const string InvalidEffectiveRange = "platform.effective_range.invalid";
    public const string InvalidSnapshotVersion = "platform.entitlement_snapshot.version.invalid";
    public const string DuplicateFeatureCode = "platform.feature_code.duplicate";
    public const string ProductMismatch = "platform.product.mismatch";
    public const string PlanVersionImmutable = "platform.plan_version.immutable";
    public const string FeatureRetired = "platform.feature.retired";
    public const string OverrideExpired = "platform.feature_override.expired";
    public const string OverrideReasonRequired = "platform.feature_override.reason_required";
    public const string OverrideCreatorRequired = "platform.feature_override.creator_required";

    public const string InvalidProductStatusTransition = "platform.product.status.invalid_transition";
    public const string InvalidPlanStatusTransition = "platform.plan.status.invalid_transition";
    public const string InvalidPlanVersionTransition = "platform.plan_version.status.invalid_transition";
    public const string InvalidTrialStatusTransition = "platform.trial.status.invalid_transition";
    public const string InvalidSubscriptionTransition = "platform.subscription.status.invalid_transition";
    public const string InvalidFeatureStatusTransition = "platform.feature.status.invalid_transition";
    public const string InvalidOverrideStatusTransition = "platform.feature_override.status.invalid_transition";

    public const string InvalidSaaSPaymentId = "platform.saas_payment.id.invalid";
    public const string InvalidSaaSPaymentTransition = "platform.saas_payment.status.invalid_transition";
    public const string PaymentAlreadyConfirmed = "platform.saas_payment.already_confirmed";
    public const string PaymentAlreadyUsed = "platform.saas_payment.already_used";
    public const string PaymentAmountInvalid = "platform.saas_payment.amount.invalid";
    public const string PaymentCurrencyInvalid = "platform.saas_payment.currency.invalid";
    public const string PaymentReferenceRequired = "platform.saas_payment.reference.required";
    public const string PaymentReasonRequired = "platform.saas_payment.reason.required";
}
