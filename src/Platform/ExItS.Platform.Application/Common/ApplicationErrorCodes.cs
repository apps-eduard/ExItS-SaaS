namespace ExItS.Platform.Application.Common;

public static class ApplicationErrorCodes
{
    public const string UserNotFound = "application.user.not_found";
    public const string OrganizationNotFound = "application.organization.not_found";
    public const string MembershipNotFound = "application.membership.not_found";
    public const string InvitationNotFound = "application.invitation.not_found";
    public const string InvitationConflict = "application.invitation.conflict";
    public const string EmailConflict = "application.user.email_conflict";
    public const string UsernameConflict = "application.user.username_conflict";
    public const string SlugConflict = "application.organization.slug_conflict";
    public const string MembershipConflict = "application.membership.conflict";
    public const string ProductAccessConflict = "application.product_access.conflict";
    public const string ProductAccessNotFound = "application.product_access.not_found";
    public const string CrossOrganizationMismatch = "application.access.cross_organization";
    public const string SubscriptionIneligible = "application.subscription.ineligible";
    public const string TrialNotAllowed = "application.subscription.trial_not_allowed";
    public const string TrialAlreadyConsumed = "application.subscription.trial_already_consumed";
    public const string InvalidBillingCycle = "application.subscription.invalid_billing_cycle";
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
    public const string PaymentNotConfigured = "application.payment.not_configured";
    public const string PaymentAlreadyUsed = "application.payment.already_used";
    public const string PaymentInvalidTransition = "application.payment.invalid_transition";
    public const string PaymentAmountInvalid = "application.payment.amount_invalid";
    public const string PaymentCurrencyInvalid = "application.payment.currency_invalid";
    public const string PaymentProductMismatch = "application.payment.product_mismatch";
    public const string PaymentOrganizationMismatch = "application.payment.organization_mismatch";
    public const string PaymentSubscriptionConflict = "application.payment.subscription_conflict";
    public const string PaymentRequiredForPaidActivation = "application.payment.required_for_paid_activation";
    public const string RuntimeOrganizationCreationDisabled = "application.organization.runtime_creation_disabled";
    public const string RuntimeProductCreationDisabled = "application.product.runtime_creation_disabled";

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
    public const string RoleDefinitionNotFound = "application.role_definition.not_found";
    public const string RoleDefinitionConflict = "application.role_definition.conflict";
    public const string CustomRoleAssignmentNotFound = "application.custom_role_assignment.not_found";
    public const string CustomRoleAssignmentConflict = "application.custom_role_assignment.conflict";
    public const string OrganizationRoleDefinitionNotFound = "application.organization_role_definition.not_found";
    public const string OrganizationRoleDefinitionConflict = "application.organization_role_definition.conflict";
    public const string OrganizationCustomRoleAssignmentNotFound = "application.organization_custom_role_assignment.not_found";
    public const string OrganizationCustomRoleAssignmentConflict = "application.organization_custom_role_assignment.conflict";
    public const string LastPlatformAdministratorProtected = "application.role_assignment.last_platform_administrator";
    public const string StepUpRequired = "application.auth.step_up_required";
    public const string MfaStepUpRequired = "application.auth.mfa_step_up_required";
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
    public const string AccountProfileNotAvailable = "application.auth.account_profile_not_available";
    public const string AccountScopeDenied = "application.auth.account_scope_denied";
    public const string CredentialTokenInvalid = "application.auth.credential_token_invalid";
    public const string CredentialTokenExpired = "application.auth.credential_token_expired";
    public const string CurrentPasswordInvalid = "application.auth.current_password_invalid";
    public const string AccessTokenInvalid = "application.auth.access_token_invalid";
    public const string ProductEntryDenied = "application.auth.product_entry_denied";
    public const string ExternalAuthDisabled = "application.auth.external_disabled";
    public const string ExternalAuthFailed = "application.auth.external_failed";
    public const string ExternalAuthEmailUnverified = "application.auth.external_email_unverified";
    public const string ExternalAuthProviderUnsupported = "application.auth.external_provider_unsupported";
    public const string RecoveryEmailConflict = "application.auth.recovery_email_conflict";
    public const string RecoveryEmailInvalid = "application.auth.recovery_email_invalid";

    public const string LocalValidationUnavailable = "application.local_validation.unavailable";
    public const string LocalValidationNotInitialized = "application.local_validation.not_initialized";
    public const string LocalValidationIdentityUnknown = "application.local_validation.identity_unknown";

    public const string PersonalContactNotFound = "application.personal.contact.not_found";
    public const string PersonalContactEmailConflict = "application.personal.contact.email.conflict";
    public const string PersonalUtangRelationshipNotFound = "application.personal.utang_relationship.not_found";
    public const string PersonalUtangUnauthorized = "application.personal.utang.unauthorized";
    public const string PersonalUtangEntryInvalid = "application.personal.utang_entry.invalid";
    public const string PersonalUtangInvitationNotFound = "application.personal.utang_invitation.not_found";
    public const string PersonalUtangInvitationConflict = "application.personal.utang_invitation.conflict";
    public const string PersonalUtangInvitationRateLimited = "application.personal.utang_invitation.rate_limited";
    public const string PersonalReminderNotFound = "application.personal.reminder.not_found";
    public const string PersonalReminderRateLimited = "application.personal.reminder.rate_limited";
    public const string PersonalNotificationNotFound = "application.personal.notification.not_found";

    public const string BusinessCustomerNotFound = "application.business_customer.not_found";
    public const string CreditCustomerNotFound = "application.credit_customer.not_found";
    public const string CreditCustomerConflict = "application.credit_customer.conflict";
    public const string CustomerLinkRequestNotFound = "application.customer_link_request.not_found";
    public const string CustomerLinkRequestConflict = "application.customer_link_request.conflict";

    public const string UtangMigrationConsentRequired = "application.utang_migration.consent_required";
    public const string UtangMigrationSelectionRequired = "application.utang_migration.selection_required";
    public const string UtangMigrationAlreadyMigrated = "application.utang_migration.already_migrated";
    public const string UtangMigrationPreviewRequired = "application.utang_migration.preview_required";
    public const string UtangMigrationConfirmationMismatch = "application.utang_migration.confirmation_mismatch";
    public const string UtangMigrationBatchNotFound = "application.utang_migration.batch_not_found";
    public const string ProductLocalRoleGrantConflict = "application.product_local_role.conflict";
    public const string ProductLocalRoleGrantNotFound = "application.product_local_role.not_found";
    public const string ProductLocalRoleMissing = "application.product_local_role.missing";
    public const string StartBusinessOwnerRequired = "application.start_business.owner_required";
    public const string BranchNotFound = "application.branch.not_found";
    public const string BranchCapacityExceeded = "application.branch.capacity_exceeded";
    public const string BranchCodeConflict = "application.branch.code_conflict";
    public const string PosDeviceNotFound = "application.pos_device.not_found";
    public const string PosDeviceCapacityExceeded = "application.pos_device.capacity_exceeded";
    public const string PosDeviceNotAuthorized = "application.pos_device.not_authorized";
    public const string PosDeviceRevoked = "application.pos_device.revoked";

    public const string GlobalCategoryNotFound = "application.global_catalog.category.not_found";
    public const string GlobalProductNotFound = "application.global_catalog.product.not_found";
    public const string BusinessTypeNotFound = "application.global_catalog.business_type.not_found";
    public const string DuplicateBusinessTypeCode = "application.global_catalog.business_type.code_conflict";
    public const string DuplicateBusinessTypeName = "application.global_catalog.business_type.name_conflict";
    public const string BusinessTypeInUse = "application.global_catalog.business_type.in_use";
    public const string DuplicateGlobalCategoryName = "application.global_catalog.category.name_conflict";
    public const string DuplicateGlobalProductBarcode = "application.global_catalog.product.barcode_conflict";
    public const string DuplicateGlobalProductSku = "application.global_catalog.product.sku_conflict";
    public const string CatalogTemplateNotFound = "application.global_catalog.template.not_found";
    public const string CatalogTemplateNotPublished = "application.global_catalog.template.not_published";
    public const string DuplicateCatalogTemplateSlug = "application.global_catalog.template.slug_conflict";
    public const string CatalogImportJobNotFound = "application.global_catalog.import.not_found";
    public const string CatalogImportNotConfirmable = "application.global_catalog.import.not_confirmable";
    public const string CatalogImportIdempotencyConflict =
        "application.global_catalog.import.idempotency_conflict";
    public const string CatalogImportFileTooLarge = "application.global_catalog.import.file_too_large";
    public const string CatalogImportUnsupportedType = "application.global_catalog.import.unsupported_type";
    public const string CatalogImportEmpty = "application.global_catalog.import.empty";
    public const string CatalogImportTooManyRows = "application.global_catalog.import.too_many_rows";
    public const string CatalogImportHeadersInvalid = "application.global_catalog.import.headers.invalid";
    public const string CatalogImportCategoryWillCreate =
        "application.global_catalog.import.category.will_create";

    public const string ComplianceRequirementNotFound =
        "application.privacy_compliance.requirement.not_found";
    public const string ProcessingSystemNotFound =
        "application.privacy_compliance.processing_system.not_found";
}
