namespace ExItS.Platform.Domain.Audit;

/// <summary>
/// Stable audit action codes for Platform mutations, aligned with the permission families in
/// docs/engineering/authorization-matrix.md. New mutation call sites should reuse these codes
/// rather than inventing free-text action names.
/// </summary>
public static class PlatformAuditActions
{
    public const string PlatformRoleAssigned = "platform.role_assignment.granted";
    public const string PlatformRoleRevoked = "platform.role_assignment.revoked";

    public const string OrganizationCreated = "platform.organization.created";
    public const string OrganizationSuspended = "platform.organization.suspended";
    public const string OrganizationReactivated = "platform.organization.reactivated";

    public const string PlatformUserCreated = "platform.user.created";
    public const string PlatformUserProfileUpdated = "platform.user.profile_updated";
    public const string PlatformUserSuspended = "platform.user.suspended";
    public const string PlatformUserReactivated = "platform.user.reactivated";
    public const string PlatformUserDeactivated = "platform.user.deactivated";

    public const string PlatformUserPasswordSet = "platform.user.password_set";
    public const string PlatformUserCredentialUnlocked = "platform.user.credential_unlocked";
    public const string PlatformUserEmailVerified = "platform.user.email_verified";
    public const string PlatformAuthBootstrapCompleted = "platform.auth.bootstrap_completed";
    public const string PlatformAuthLoginSucceeded = "platform.auth.login_succeeded";
    public const string PlatformAuthLoginFailed = "platform.auth.login_failed";
    public const string PlatformAuthLogout = "platform.auth.logout";
    public const string PlatformAuthSessionRevoked = "platform.auth.session_revoked";
    public const string PlatformAuthPasswordChanged = "platform.auth.password_changed";
    public const string PlatformAuthPasswordResetRequested = "platform.auth.password_reset_requested";
    public const string PlatformAuthPasswordResetCompleted = "platform.auth.password_reset_completed";
    public const string PlatformAuthEmailVerificationRequested = "platform.auth.email_verification_requested";
    public const string PlatformAuthEmailVerificationCompleted = "platform.auth.email_verification_completed";
    public const string PlatformAuthLockoutStarted = "platform.auth.lockout_started";

    public const string MembershipAdded = "platform.membership.added";
    public const string MembershipRoleChanged = "platform.membership.role_changed";
    public const string MembershipSuspended = "platform.membership.suspended";
    public const string MembershipReactivated = "platform.membership.reactivated";
    public const string MembershipRevoked = "platform.membership.revoked";

    public const string ProductAccessGranted = "platform.product_access.granted";
    public const string ProductAccessRevoked = "platform.product_access.revoked";

    public const string SubscriptionTrialStarted = "platform.subscription.trial_started";
    public const string SubscriptionActivated = "platform.subscription.activated";
    public const string SubscriptionEnteredGracePeriod = "platform.subscription.grace_period_entered";
    public const string SubscriptionMarkedPastDue = "platform.subscription.past_due_marked";
    public const string SubscriptionSuspended = "platform.subscription.suspended";
    public const string SubscriptionReactivated = "platform.subscription.reactivated";
    public const string SubscriptionCancelled = "platform.subscription.cancelled";
    public const string SubscriptionExpired = "platform.subscription.expired";

    public const string ManualPaymentCreated = "platform.payment.created";
    public const string ManualPaymentConfirmed = "platform.payment.confirmed";
    public const string ManualPaymentRejected = "platform.payment.rejected";
    public const string ManualPaymentVoided = "platform.payment.voided";

    public const string FeatureOverrideCreated = "platform.feature_override.created";
    public const string FeatureOverrideRevoked = "platform.feature_override.revoked";

    /// <summary>Generic action code recorded when a permission-gated read is denied (e.g. audit or role-assignment views).</summary>
    public const string PlatformAccessChecked = "platform.access.checked";
}
