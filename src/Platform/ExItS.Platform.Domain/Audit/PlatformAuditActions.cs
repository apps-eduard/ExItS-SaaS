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
    public const string PlatformRoleDefinitionCreated = "platform.role_definition.created";
    public const string PlatformRoleDefinitionUpdated = "platform.role_definition.updated";
    public const string PlatformRoleDefinitionActivated = "platform.role_definition.activated";
    public const string PlatformRoleDefinitionDeactivated = "platform.role_definition.deactivated";
    public const string PlatformRoleDefinitionRetired = "platform.role_definition.retired";
    public const string PlatformCustomRoleAssigned = "platform.custom_role_assignment.granted";
    public const string PlatformCustomRoleRevoked = "platform.custom_role_assignment.revoked";
    public const string OrganizationRoleDefinitionCreated = "platform.organization_role_definition.created";
    public const string OrganizationRoleDefinitionUpdated = "platform.organization_role_definition.updated";
    public const string OrganizationRoleDefinitionActivated = "platform.organization_role_definition.activated";
    public const string OrganizationRoleDefinitionDeactivated = "platform.organization_role_definition.deactivated";
    public const string OrganizationRoleDefinitionRetired = "platform.organization_role_definition.retired";
    public const string OrganizationCustomRoleAssigned = "platform.organization_custom_role_assignment.granted";
    public const string OrganizationCustomRoleRevoked = "platform.organization_custom_role_assignment.revoked";

    public const string OrganizationCreated = "platform.organization.created";
    public const string OrganizationUpdated = "platform.organization.updated";
    public const string OrganizationBrandingUpdated = "platform.organization.branding_updated";
    public const string OrganizationSuspended = "platform.organization.suspended";
    public const string OrganizationReactivated = "platform.organization.reactivated";
    public const string OrganizationClosed = "platform.organization.closed";

    public const string PlatformUserCreated = "platform.user.created";
    public const string PlatformUserProfileUpdated = "platform.user.profile_updated";
    public const string PlatformUserPublicIdAssigned = "platform.user.public_id_assigned";
    public const string PlatformUserPublicIdResolved = "platform.user.public_id_resolved";
    public const string OrganizationPublicIdResolved = "platform.organization.public_id_resolved";
    public const string ExItsQrResolved = "platform.qr.resolved";
    public const string PosDeviceRegistrationTokenCreated = "platform.pos_device.registration_token.created";
    public const string PosDeviceRegistrationTokenRedeemed = "platform.pos_device.registration_token.redeemed";
    public const string PlatformUserSuspended = "platform.user.suspended";
    public const string PlatformUserReactivated = "platform.user.reactivated";
    public const string PlatformUserDeactivated = "platform.user.deactivated";
    public const string PlatformUserMovedToSuspended = "platform.user.moved_to_suspended";
    public const string PlatformUserGlobalSuspended = "platform.user.global_suspended";
    public const string PlatformUserGlobalReactivated = "platform.user.global_reactivated";
    public const string PlatformUserReactivationStepUpSucceeded = "platform.user.reactivation_step_up_succeeded";

    public const string PlatformUserPasswordSet = "platform.user.password_set";
    public const string PlatformUserCredentialUnlocked = "platform.user.credential_unlocked";
    public const string PlatformUserEmailVerified = "platform.user.email_verified";
    public const string PlatformAuthBootstrapCompleted = "platform.auth.bootstrap_completed";
    public const string PlatformAuthLoginSucceeded = "platform.auth.login_succeeded";
    public const string PlatformAccountProfileSelected = "platform.auth.account_profile_selected";
    public const string PlatformAccountScopeDenied = "platform.auth.account_scope_denied";
    public const string PlatformAuthLoginFailed = "platform.auth.login_failed";
    public const string PlatformAuthLogout = "platform.auth.logout";
    public const string PlatformAuthSessionRevoked = "platform.auth.session_revoked";
    public const string PlatformAuthPasswordChanged = "platform.auth.password_changed";
    public const string PlatformAuthPasswordResetRequested = "platform.auth.password_reset_requested";
    public const string PlatformAuthPasswordResetCompleted = "platform.auth.password_reset_completed";
    public const string PlatformAuthEmailVerificationRequested = "platform.auth.email_verification_requested";
    public const string PlatformAuthEmailVerificationCompleted = "platform.auth.email_verification_completed";
    public const string PersonalAccountRegistrationStarted = "platform.personal.registration_started";
    public const string PersonalAccountRegistrationActivated = "platform.personal.registration_activated";
    public const string PlatformAuthRecoveryEmailRequested = "platform.auth.recovery_email_requested";
    public const string PlatformAuthRecoveryEmailConfirmed = "platform.auth.recovery_email_confirmed";
    public const string PlatformAuthRecoveryEmailSkipped = "platform.auth.recovery_email_skipped";
    public const string PlatformAuthRecoveryEmailCleared = "platform.auth.recovery_email_cleared";
    public const string PlatformAuthLockoutStarted = "platform.auth.lockout_started";
    public const string PlatformAuthOrganizationContextChanged = "platform.auth.organization_context_changed";
    public const string PlatformAuthAccessTokenIssued = "platform.auth.access_token_issued";
    public const string PlatformAuthAccessTokenBound = "platform.auth.access_token_bound";
    public const string PlatformAuthAccessTokenRevoked = "platform.auth.access_token_revoked";
    public const string PlatformAuthExternalLoginSucceeded = "platform.auth.external_login_succeeded";
    public const string PlatformAuthExternalLoginLinked = "platform.auth.external_login_linked";
    public const string PlatformAuthExternalLoginFailed = "platform.auth.external_login_failed";
    /// <summary>Reserved for a future MFA enrollment WP — not emitted in P13-WP07.</summary>
    public const string PlatformAuthMfaEnrollmentStarted = "platform.auth.mfa_enrollment_started";
    /// <summary>Reserved for a future MFA enrollment WP — not emitted in P13-WP07.</summary>
    public const string PlatformAuthMfaEnrollmentCompleted = "platform.auth.mfa_enrollment_completed";
    /// <summary>Reserved for a future MFA challenge WP — not emitted in P13-WP07.</summary>
    public const string PlatformAuthMfaChallengeSucceeded = "platform.auth.mfa_challenge_succeeded";
    /// <summary>Reserved for a future MFA challenge WP — not emitted in P13-WP07.</summary>
    public const string PlatformAuthMfaChallengeFailed = "platform.auth.mfa_challenge_failed";

    public const string MembershipAdded = "platform.membership.added";
    public const string MembershipRoleChanged = "platform.membership.role_changed";
    public const string MembershipSuspended = "platform.membership.suspended";
    public const string MembershipReactivated = "platform.membership.reactivated";
    public const string MembershipRevoked = "platform.membership.revoked";

    public const string InvitationCreated = "platform.invitation.created";
    public const string InvitationResent = "platform.invitation.resent";
    public const string InvitationRevoked = "platform.invitation.revoked";
    public const string InvitationAccepted = "platform.invitation.accepted";

    public const string BusinessCustomerCreated = "platform.business_customer.created";
    public const string BusinessCustomerUpdated = "platform.business_customer.updated";
    public const string BusinessCustomerArchived = "platform.business_customer.archived";
    public const string CreditCustomerEnabled = "platform.credit_customer.enabled";
    public const string CreditCustomerClosed = "platform.credit_customer.closed";
    public const string CustomerLinkRequestCreated = "platform.customer_link_request.created";
    public const string CustomerLinkRequestResent = "platform.customer_link_request.resent";
    public const string CustomerLinkRequestRevoked = "platform.customer_link_request.revoked";
    public const string CustomerLinkRequestAccepted = "platform.customer_link_request.accepted";
    public const string CustomerLinkRequestDeclined = "platform.customer_link_request.declined";
    public const string LinkedCustomerAppUserRevoked = "platform.linked_customer_app_user.revoked";
    public const string CustomerToStaffConversionDenied = "platform.customer.staff_conversion_denied";

    public const string CatalogProductCreated = "platform.catalog.product.created";
    public const string CatalogProductUpdated = "platform.catalog.product.updated";
    public const string CatalogProductActivated = "platform.catalog.product.activated";
    public const string CatalogProductDeactivated = "platform.catalog.product.deactivated";
    public const string CatalogProductRetired = "platform.catalog.product.retired";
    public const string CatalogPlanCreated = "platform.catalog.plan.created";
    public const string CatalogPlanUpdated = "platform.catalog.plan.updated";
    public const string CatalogPlanActivated = "platform.catalog.plan.activated";
    public const string CatalogPlanRetired = "platform.catalog.plan.retired";
    public const string CatalogPlanVersionPublished = "platform.catalog.plan_version.published";
    public const string PersonalFeatureDefinitionUpdated = "platform.personal.feature_definition.updated";

    public const string GlobalCategoryCreated = "platform.global_catalog.category.created";
    public const string GlobalCategoryUpdated = "platform.global_catalog.category.updated";
    public const string GlobalCategoryStatusChanged = "platform.global_catalog.category.status_changed";
    public const string GlobalProductCreated = "platform.global_catalog.product.created";
    public const string GlobalProductUpdated = "platform.global_catalog.product.updated";
    public const string GlobalProductStatusChanged = "platform.global_catalog.product.status_changed";
    public const string BusinessTypeCreated = "platform.global_catalog.business_type.created";
    public const string BusinessTypeUpdated = "platform.global_catalog.business_type.updated";
    public const string BusinessTypeStatusChanged = "platform.global_catalog.business_type.status_changed";
    public const string CatalogTemplateCreated = "platform.global_catalog.template.created";
    public const string CatalogTemplateUpdated = "platform.global_catalog.template.updated";
    public const string CatalogTemplatePublished = "platform.global_catalog.template.published";
    public const string CatalogTemplateUnpublished = "platform.global_catalog.template.unpublished";
    public const string CatalogTemplateArchived = "platform.global_catalog.template.archived";
    public const string CatalogTemplateCompositionChanged = "platform.global_catalog.template.composition_changed";
    public const string CatalogImportCreated = "platform.global_catalog.import.created";
    public const string CatalogImportConfirmed = "platform.global_catalog.import.confirmed";
    public const string CatalogImportCompleted = "platform.global_catalog.import.completed";

    public const string ProductAccessGranted = "platform.product_access.granted";
    public const string ProductAccessRevoked = "platform.product_access.revoked";

    public const string SubscriptionTrialStarted = "platform.subscription.trial_started";
    public const string SubscriptionTrialConverted = "platform.subscription.trial_converted";
    public const string SubscriptionPaidStarted = "platform.subscription.paid_started";
    public const string SubscriptionActivated = "platform.subscription.activated";
    public const string SubscriptionEnteredGracePeriod = "platform.subscription.grace_period_entered";
    public const string SubscriptionMarkedPastDue = "platform.subscription.past_due_marked";
    public const string SubscriptionSuspended = "platform.subscription.suspended";
    public const string SubscriptionReactivated = "platform.subscription.reactivated";
    public const string SubscriptionCancelled = "platform.subscription.cancelled";
    public const string SubscriptionExpired = "platform.subscription.expired";
    public const string SubscriptionUpgraded = "platform.subscription.upgraded";
    public const string SubscriptionDowngradeScheduled = "platform.subscription.downgrade_scheduled";
    public const string SubscriptionPendingPlanApplied = "platform.subscription.pending_plan_applied";
    public const string SubscriptionPlanChangePreviewed = "platform.subscription.plan_change_previewed";

    public const string ManualPaymentCreated = "platform.payment.created";
    public const string ManualPaymentConfirmed = "platform.payment.confirmed";
    public const string ManualPaymentRejected = "platform.payment.rejected";
    public const string ManualPaymentVoided = "platform.payment.voided";
    public const string ProviderPaymentLinked = "platform.payment.provider_linked";

    public const string FeatureOverrideCreated = "platform.feature_override.created";
    public const string FeatureOverrideRevoked = "platform.feature_override.revoked";
    public const string EntitlementSnapshotGenerated = "platform.entitlement.snapshot_generated";
    public const string EntitlementSnapshotReconciled = "platform.entitlement.snapshot_reconciled";

    /// <summary>Generic action code recorded when a permission-gated read is denied (e.g. audit or role-assignment views).</summary>
    public const string PlatformAccessChecked = "platform.access.checked";

    public const string PersonalAccountSettingsUpdated = "platform.personal.account_settings.updated";
    public const string PersonalContactCreated = "platform.personal.contact.created";
    public const string PersonalContactLinked = "platform.personal.contact.linked";
    public const string PersonalUtangRelationshipCreated = "platform.personal.utang_relationship.created";
    public const string PersonalUtangParticipantAuthorized = "platform.personal.utang_participant.authorized";
    public const string PersonalUtangEntryRecorded = "platform.personal.utang_entry.recorded";
    public const string PersonalUtangInvitationCreated = "platform.personal.utang_invitation.created";
    public const string PersonalUtangInvitationResent = "platform.personal.utang_invitation.resent";
    public const string PersonalUtangInvitationRevoked = "platform.personal.utang_invitation.revoked";
    public const string PersonalUtangInvitationAccepted = "platform.personal.utang_invitation.accepted";
    public const string PersonalUtangInvitationDeclined = "platform.personal.utang_invitation.declined";
    public const string PersonalUtangInvitationExpired = "platform.personal.utang_invitation.expired";
    public const string PersonalReminderCreated = "platform.personal.reminder.created";
    public const string PersonalReminderDelivered = "platform.personal.reminder.delivered";
    public const string PersonalReminderCancelled = "platform.personal.reminder.cancelled";
    public const string PersonalNotificationDelivered = "platform.personal.notification.delivered";

    public const string BusinessUpgradeStarted = "platform.business_upgrade.started";
    public const string BusinessUpgradeCompleted = "platform.business_upgrade.completed";
    public const string UtangMigrationPreviewed = "platform.utang_migration.previewed";
    public const string UtangMigrationExecuted = "platform.utang_migration.executed";
    public const string ProductLocalRoleGranted = "platform.product_local_role.granted";
    public const string ProductLocalRoleRevoked = "platform.product_local_role.revoked";
    public const string EnabledProductsDiscovered = "platform.enabled_products.discovered";
    public const string ProductAuthorizationChecked = "platform.product_authorization.checked";
    public const string ProductLaunched = "platform.product.launched";

    public const string PrivacyComplianceCatalogEnsured = "platform.privacy_compliance.catalog.ensured";
    public const string PrivacyComplianceRequirementStatusUpdated =
        "platform.privacy_compliance.requirement.status_updated";
    public const string PrivacyComplianceRequirementDetailsUpdated =
        "platform.privacy_compliance.requirement.details_updated";
    public const string PrivacyComplianceEvidenceAdded = "platform.privacy_compliance.evidence.added";
    public const string PrivacyComplianceRequirementExported =
        "platform.privacy_compliance.requirement.exported";
}
