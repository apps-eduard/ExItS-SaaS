namespace ExItS.Platform.Application.Common;

public static class ApplicationErrorCodes
{
    public const string UserNotFound = "application.user.not_found";
    public const string OrganizationNotFound = "application.organization.not_found";
    public const string MembershipNotFound = "application.membership.not_found";
    public const string EmailConflict = "application.user.email_conflict";
    public const string SlugConflict = "application.organization.slug_conflict";
    public const string MembershipConflict = "application.membership.conflict";
    public const string DomainViolation = "application.domain_violation";

    public const string ProductNotFound = "application.product.not_found";
    public const string PlanNotFound = "application.plan.not_found";
    public const string PlanVersionNotFound = "application.plan_version.not_found";
    public const string TrialNotFound = "application.trial.not_found";
    public const string SubscriptionNotFound = "application.subscription.not_found";
    public const string FeatureNotFound = "application.feature.not_found";
    public const string FeatureOverrideNotFound = "application.feature_override.not_found";

    public const string DuplicateProductCode = "application.product.code_conflict";
    public const string DuplicatePlanCode = "application.plan.code_conflict";
    public const string DuplicateFeatureCode = "application.feature.code_conflict";
    public const string SnapshotVersionConflict = "application.entitlement_snapshot.version_conflict";

    public const string UnsupportedContractVersion = "application.contract.version.unsupported";
    public const string ProjectionConflict = "application.projection.conflict";
    public const string ReconciliationRequired = "application.projection.reconciliation_required";
}
