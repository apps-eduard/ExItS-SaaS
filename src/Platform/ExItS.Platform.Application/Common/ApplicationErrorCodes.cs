namespace ExItS.Platform.Application.Common;

public static class ApplicationErrorCodes
{
    public const string UserNotFound = "application.user.not_found";
    public const string OrganizationNotFound = "application.organization.not_found";
    public const string MembershipNotFound = "application.membership.not_found";
    public const string EmailConflict = "application.user.email_conflict";
    public const string UsernameConflict = "application.user.username_conflict";
    public const string SlugConflict = "application.organization.slug_conflict";
    public const string MembershipConflict = "application.membership.conflict";
    public const string ProductAccessConflict = "application.product_access.conflict";
    public const string ProductAccessNotFound = "application.product_access.not_found";
    public const string CrossOrganizationMismatch = "application.access.cross_organization";
    public const string SubscriptionIneligible = "application.subscription.ineligible";
    public const string EntitlementMissing = "application.entitlement.missing";
    public const string EntitlementStale = "application.entitlement.stale";
    public const string EntitlementDenied = "application.entitlement.denied";
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

    public const string ActiveSubscriptionConflict = "application.subscription.active_conflict";
    public const string ConcurrencyConflict = "application.concurrency_conflict";
    public const string OrganizationNotEligible = "application.organization.not_eligible";
    public const string OrganizationContextNotEligible = "application.auth.organization_context_not_eligible";
    public const string ProductNotActive = "application.product.not_active";

    public const string UnsupportedContractVersion = "application.contract.version.unsupported";
    public const string ProjectionConflict = "application.projection.conflict";
    public const string ReconciliationRequired = "application.projection.reconciliation_required";

    public const string MigrationBatchInvalid = "application.migration.batch_invalid";
    public const string DuplicateIdentityMapping = "application.migration.duplicate_identity";
    public const string AmbiguousIdentityMapping = "application.migration.ambiguous_identity";
    public const string OrganizationMappingConflict = "application.migration.organization_conflict";
    public const string MembershipMappingConflict = "application.migration.membership_conflict";
    public const string UnsupportedMigrationContract = "application.migration.unsupported_contract";
    public const string MigrationValidationBlocked = "application.migration.blocked";
    public const string RollbackEvidenceMissing = "application.migration.rollback_evidence_missing";
    public const string SensitiveMigrationDataDetected = "application.migration.sensitive_data";

    public const string PaymentNotFound = "application.payment.not_found";
    public const string PaymentReferenceConflict = "application.payment.reference_conflict";
    public const string PaymentAlreadyConfirmed = "application.payment.already_confirmed";
    public const string PaymentNotConfirmed = "application.payment.not_confirmed";
    public const string PaymentAlreadyUsed = "application.payment.already_used";
    public const string PaymentInvalidTransition = "application.payment.invalid_transition";
    public const string PaymentAmountInvalid = "application.payment.amount_invalid";
    public const string PaymentCurrencyInvalid = "application.payment.currency_invalid";
    public const string PaymentProductMismatch = "application.payment.product_mismatch";
    public const string PaymentOrganizationMismatch = "application.payment.organization_mismatch";
    public const string PaymentSubscriptionConflict = "application.payment.subscription_conflict";

    public const string EntitlementSnapshotNotFound = "application.entitlement_snapshot.not_found";
    public const string EntitlementSnapshotInvalid = "application.entitlement_snapshot.invalid";
    public const string EntitlementSchemaUnsupported = "application.entitlement_snapshot.schema_unsupported";
    public const string FeatureOverrideConflict = "application.feature_override.conflict";
    public const string FeatureOverrideInvalidTransition = "application.feature_override.invalid_transition";
    public const string EntitlementProductMismatch = "application.entitlement.product_mismatch";
    public const string EntitlementSubscriptionInvalid = "application.entitlement.subscription_invalid";
    public const string EntitlementRefreshPolicyMissing = "application.entitlement.refresh_policy_missing";

    public const string RoleAssignmentNotFound = "application.role_assignment.not_found";
    public const string RoleAssignmentConflict = "application.role_assignment.conflict";
    public const string AuditRecordNotFound = "application.audit_record.not_found";

    public const string CredentialNotFound = "application.credential.not_found";
    public const string CredentialAlreadyExists = "application.credential.already_exists";
    public const string PasswordInvalid = "application.credential.password_invalid";
    public const string CredentialLockedOut = "application.credential.locked_out";
    public const string BootstrapDisabled = "application.auth.bootstrap_disabled";
    public const string BootstrapAlreadyCompleted = "application.auth.bootstrap_already_completed";
    public const string BootstrapConfigurationInvalid = "application.auth.bootstrap_configuration_invalid";
    public const string BootstrapUnauthorized = "application.auth.bootstrap_unauthorized";
    public const string BootstrapForbiddenInEnvironment = "application.auth.bootstrap_forbidden_environment";

    public const string LoginFailed = "application.auth.login_failed";
    public const string SessionInvalid = "application.auth.session_invalid";
    public const string SessionExpired = "application.auth.session_expired";
    public const string AccountNotEligibleForLogin = "application.auth.account_not_eligible";
    public const string CredentialTokenInvalid = "application.auth.credential_token_invalid";
    public const string CredentialTokenExpired = "application.auth.credential_token_expired";
    public const string CurrentPasswordInvalid = "application.auth.current_password_invalid";
    public const string AccessTokenInvalid = "application.auth.access_token_invalid";
    public const string ProductEntryDenied = "application.auth.product_entry_denied";
}
