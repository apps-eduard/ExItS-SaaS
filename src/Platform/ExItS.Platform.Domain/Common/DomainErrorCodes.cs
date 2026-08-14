namespace ExItS.Platform.Domain.Common;

/// <summary>Stable domain error codes for identity and organization invariants.</summary>
public static class DomainErrorCodes
{
    public const string InvalidPlatformUserId = "platform.user.id.invalid";
    public const string InvalidPlatformOrganizationId = "platform.organization.id.invalid";
    public const string InvalidOrganizationMembershipId = "platform.membership.id.invalid";
    public const string InvalidOrganizationInvitationId = "platform.invitation.id.invalid";
    public const string InvalidOrganizationOwnershipTransferId = "platform.ownership_transfer.id.invalid";
    public const string InvalidProductAccessAssignmentId = "platform.product_access.id.invalid";
    public const string ActorReferenceRequired = "platform.actor.required";
    public const string InvalidInvitationStatusTransition = "platform.invitation.status.invalid_transition";
    public const string InvitationExpired = "platform.invitation.expired";
    public const string InvitationEmailMismatch = "platform.invitation.email_mismatch";
    public const string InvalidInvitationToken = "platform.invitation.token.invalid";
    public const string InvalidOwnershipTransferStatusTransition =
        "platform.ownership_transfer.status.invalid_transition";
    public const string OwnershipTransferExpired = "platform.ownership_transfer.expired";
    public const string OwnershipTransferSelfDenied = "platform.ownership_transfer.self_denied";
    public const string OwnershipTransferActorMismatch = "platform.ownership_transfer.actor_mismatch";
    public const string OwnershipTransferTargetInvalid = "platform.ownership_transfer.target_invalid";
    public const string OwnershipTransferQrPurposeRejected = "platform.ownership_transfer.qr_purpose_rejected";
    public const string OwnershipTransferPendingConflict = "platform.ownership_transfer.pending_conflict";
    public const string OwnershipTransferOwnerInvariant =
        "platform.ownership_transfer.owner_invariant";
    public const string LastGoverningAdminProtected = "platform.membership.last_governing_admin";
    public const string OrganizationOwnerAssignmentDenied = "platform.membership.owner_assignment_denied";
    public const string OrganizationOwnerUniqueViolation = "platform.membership.owner_unique_violation";

    public const string InvalidDisplayName = "platform.display_name.invalid";
    public const string InvalidEmail = "platform.email.invalid";
    public const string InvalidUsername = "platform.username.invalid";
    public const string InvalidPhone = "platform.phone.invalid";
    public const string InvalidEmployeeCode = "platform.employee_code.invalid";
    public const string InvalidStaffNumber = "platform.staff_number.invalid";
    public const string StaffNumberImmutable = "platform.staff_number.immutable";
    public const string InvalidPublicUserId = "platform.public_user_id.invalid";
    public const string PublicUserIdImmutable = "platform.public_user_id.immutable";
    public const string PublicUserIdRequired = "platform.public_user_id.required";
    public const string InvalidPublicOrganizationId = "platform.public_organization_id.invalid";
    public const string PublicOrganizationIdImmutable = "platform.public_organization_id.immutable";
    public const string PublicOrganizationIdRequired = "platform.public_organization_id.required";
    public const string HomeOrganizationImmutable = "platform.user.home_organization.immutable";
    public const string HomeOrganizationRequired = "platform.user.home_organization.required";
    public const string StaffOrganizationSwitchDenied = "platform.user.staff_organization_switch_denied";
    public const string InvalidOrganizationSlug = "platform.organization.slug.invalid";
    public const string PrimaryBusinessTypeImmutable = "platform.organization.primary_business_type.immutable";
    public const string DuplicateBusinessTypeGrant = "platform.plan_version.business_type_grant.duplicate";
    public const string DuplicateBusinessTypeActivation = "platform.organization.business_type_activation.duplicate";
    public const string PrimaryBusinessTypeActivationForbidden =
        "platform.organization.business_type_activation.primary_forbidden";
    public const string InvalidOrganizationProfile = "platform.organization.profile.invalid";
    public const string InvalidOrganizationBranding = "platform.organization.branding.invalid";
    public const string InvalidProductCode = "platform.product_code.invalid";
    public const string InvalidUtcTimestamp = "platform.timestamp.invalid";

    public const string InvalidAccountStatusTransition = "platform.user.status.invalid_transition";
    public const string InvalidOrganizationStatusTransition = "platform.organization.status.invalid_transition";
    public const string InvalidOrganizationBranchId = "platform.organization_branch.id.invalid";
    public const string InvalidOrganizationBranchCode = "platform.organization_branch.code.invalid";
    public const string InvalidOrganizationBranchStatusTransition = "platform.organization_branch.status.invalid_transition";
    public const string OrganizationBranchNotActive = "platform.organization_branch.not_active";
    public const string OrganizationBranchPrimaryRequired = "platform.organization_branch.primary.required";
    public const string InvalidPosDeviceId = "platform.pos_device.id.invalid";
    public const string InvalidPosDeviceInstallationId = "platform.pos_device.installation_id.invalid";
    public const string PosDeviceNotActive = "platform.pos_device.not_active";
    public const string InvalidPosDeviceRegistrationTokenId = "platform.pos_device.registration_token.id.invalid";
    public const string InvalidPosDeviceRegistrationToken = "platform.pos_device.registration_token.invalid";
    public const string PosDeviceRegistrationTokenExpired = "platform.pos_device.registration_token.expired";
    public const string PosDeviceRegistrationTokenAlreadyRedeemed =
        "platform.pos_device.registration_token.already_redeemed";
    public const string PosDeviceRegistrationTokenRevoked = "platform.pos_device.registration_token.revoked";
    public const string PosDeviceRegistrationTokenNotActive = "platform.pos_device.registration_token.not_active";
    public const string PosDeviceRegistrationTokenOrganizationMismatch =
        "platform.pos_device.registration_token.organization_mismatch";
    public const string InvalidExItsQrPayload = "platform.qr.payload.invalid";
    public const string InvalidExItsQrPurpose = "platform.qr.purpose.invalid";
    public const string ExItsQrPurposeMismatch = "platform.qr.purpose.mismatch";
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
    public const string UnsupportedSubscriptionStatus = "platform.subscription.status.unsupported";
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

    public const string InvalidPlatformRoleAssignmentId = "platform.role_assignment.id.invalid";
    public const string InvalidPlatformSystemRole = "platform.role_assignment.role.invalid";
    public const string InvalidPermissionCode = "platform.permission.invalid";
    public const string AuthorizationDenied = "platform.authorization.denied";
    public const string InvalidPlatformRoleDefinitionId = "platform.role_definition.id.invalid";
    public const string InvalidPlatformCustomRoleAssignmentId = "platform.custom_role_assignment.id.invalid";
    public const string InvalidOrganizationRoleDefinitionId = "platform.organization_role_definition.id.invalid";
    public const string InvalidOrganizationCustomRoleAssignmentId = "platform.organization_custom_role_assignment.id.invalid";
    public const string InvalidPlatformRoleCode = "platform.role_definition.code.invalid";
    public const string InvalidOrganizationRoleCode = "platform.organization_role_definition.code.invalid";
    public const string InvalidPlatformRoleStatusTransition = "platform.role_definition.status.invalid_transition";
    public const string InvalidOrganizationRoleStatusTransition = "platform.organization_role_definition.status.invalid_transition";
    public const string BuiltInRoleProtected = "platform.role_definition.built_in_protected";
    public const string RoleDefinitionNotAssignable = "platform.role_definition.not_assignable";
    public const string LastPlatformAdministratorProtected = "platform.role_assignment.last_platform_administrator";
    public const string InvalidOrganizationPermissionCode = "platform.organization_permission.invalid";

    public const string InvalidAuditRecordId = "platform.audit_record.id.invalid";
    public const string InvalidAuditActorType = "platform.audit_record.actor_type.invalid";
    public const string InvalidAuditOutcome = "platform.audit_record.outcome.invalid";
    public const string InvalidAuditActorIdentifier = "platform.audit_record.actor_identifier.invalid";
    public const string InvalidAuditActionCode = "platform.audit_record.action_code.invalid";
    public const string InvalidAuditTargetType = "platform.audit_record.target_type.invalid";
    public const string InvalidAuditTargetId = "platform.audit_record.target_id.invalid";
    public const string InvalidAuditCorrelationId = "platform.audit_record.correlation_id.invalid";
    public const string InvalidAuditReason = "platform.audit_record.reason.invalid";
    public const string InvalidAuditSummary = "platform.audit_record.summary.invalid";

    public const string PersonalAccountSettingsConcurrencyConflict =
        "platform.personal.account_settings.concurrency_conflict";

    public const string InvalidPersonalContactId = "platform.personal.contact.id.invalid";
    public const string InvalidPersonalContactDisplayName = "platform.personal.contact.display_name.invalid";
    public const string PersonalContactEmailConflict = "platform.personal.contact.email.conflict";
    public const string InvalidPersonalDebtRelationshipId = "platform.personal.debt_relationship.id.invalid";
    public const string InvalidPersonalDebtRelationship = "platform.personal.debt_relationship.invalid";
    public const string InvalidPersonalUtangEntryId = "platform.personal.utang_entry.id.invalid";
    public const string InvalidPersonalUtangEntryType = "platform.personal.utang_entry.type.invalid";
    public const string PersonalUtangAmountInvalid = "platform.personal.utang.amount.invalid";
    public const string PersonalUtangUnauthorized = "platform.personal.utang.unauthorized";
    public const string PersonalUtangConcurrencyConflict = "platform.personal.utang.concurrency_conflict";

    public const string InvalidPersonalUtangInvitationId = "platform.personal.utang_invitation.id.invalid";
    public const string InvalidPersonalUtangInvitationStatusTransition =
        "platform.personal.utang_invitation.status.invalid_transition";
    public const string PersonalUtangInvitationExpired = "platform.personal.utang_invitation.expired";
    public const string PersonalUtangInvitationTokenInvalid = "platform.personal.utang_invitation.token.invalid";
    public const string PersonalUtangInvitationEmailMismatch = "platform.personal.utang_invitation.email_mismatch";
    public const string PersonalUtangInvitationRateLimited = "platform.personal.utang_invitation.rate_limited";
    public const string PersonalContactAlreadyLinked = "platform.personal.contact.already_linked";
    public const string PersonalContactLinkInvalid = "platform.personal.contact.link.invalid";

    public const string InvalidPersonalReminderId = "platform.personal.reminder.id.invalid";
    public const string InvalidPersonalReminder = "platform.personal.reminder.invalid";
    public const string PersonalReminderRateLimited = "platform.personal.reminder.rate_limited";
    public const string PersonalReminderUnauthorized = "platform.personal.reminder.unauthorized";

    public const string InvalidPersonalNotificationId = "platform.personal.notification.id.invalid";

    public const string InvalidBusinessCustomerId = "platform.business_customer.id.invalid";
    public const string InvalidBusinessCustomerDisplayName = "platform.business_customer.display_name.invalid";
    public const string InvalidBusinessCustomerStatusTransition =
        "platform.business_customer.status.invalid_transition";
    public const string BusinessCustomerAlreadyLinked = "platform.business_customer.already_linked";
    public const string InvalidCreditCustomerId = "platform.credit_customer.id.invalid";
    public const string InvalidCreditCustomerCurrency = "platform.credit_customer.currency.invalid";
    public const string InvalidCustomerLinkRequestId = "platform.customer_link_request.id.invalid";
    public const string InvalidCustomerLinkRequestStatusTransition =
        "platform.customer_link_request.status.invalid_transition";
    public const string CustomerLinkRequestExpired = "platform.customer_link_request.expired";
    public const string InvalidCustomerLinkRequestToken = "platform.customer_link_request.token.invalid";
    public const string CustomerLinkRequestEmailMismatch = "platform.customer_link_request.email_mismatch";
    public const string CustomerLinkRequestTargetMismatch = "platform.customer_link_request.target_mismatch";
    public const string InvalidLinkedCustomerAppUserId = "platform.linked_customer_app_user.id.invalid";
    public const string InvalidOrganizationNotificationId = "platform.organization.notification.id.invalid";
    public const string CustomerToStaffConversionDenied = "platform.customer.staff_conversion_denied";
    public const string CustomerLinkMustNotCreateStaff = "platform.customer_link.must_not_create_staff";
    public const string CustomerLinkPersonalIdentityRequired =
        "platform.customer_link.personal_identity_required";
    public const string LinkedCustomerAppUserNotFound = "platform.linked_customer_app_user.not_found";
    public const string StaffCannotAccessUnrelatedPersonalRecords =
        "platform.staff.unrelated_personal_records_denied";

    public const string InvalidPersonalUtangMigrationBatchId =
        "platform.personal.utang_migration.batch_id.invalid";
    public const string InvalidPersonalUtangMigrationItemId =
        "platform.personal.utang_migration.item_id.invalid";
    public const string PersonalUtangMigrationConsentRequired =
        "platform.personal.utang_migration.consent_required";
    public const string PersonalUtangMigrationSelectionRequired =
        "platform.personal.utang_migration.selection_required";
    public const string PersonalUtangAlreadyMigrated =
        "platform.personal.utang_migration.already_migrated";
    public const string InvalidPersonalUtangMigrationStatusTransition =
        "platform.personal.utang_migration.status.invalid_transition";
    public const string InvalidBusinessCreditOpeningBalanceId =
        "platform.business_credit_opening_balance.id.invalid";
    public const string InvalidProductLocalRoleGrantId =
        "platform.product_local_role_grant.id.invalid";
    public const string InvalidProductLocalRoleCode =
        "platform.product_local_role.code.invalid";
    public const string InvalidProductLocalRoleStatusTransition =
        "platform.product_local_role.status.invalid_transition";

    public const string InvalidGlobalCategoryId = "platform.global_catalog.category.id.invalid";
    public const string InvalidGlobalProductId = "platform.global_catalog.product.id.invalid";
    public const string InvalidGlobalCatalogName = "platform.global_catalog.name.invalid";
    public const string InvalidGlobalCategoryIcon = "platform.global_catalog.category.icon.invalid";
    public const string InvalidGlobalCategoryParent = "platform.global_catalog.category.parent.invalid";
    public const string InvalidGlobalCategoryStatusTransition =
        "platform.global_catalog.category.status.invalid_transition";
    public const string InvalidGlobalProductStatusTransition =
        "platform.global_catalog.product.status.invalid_transition";
    public const string InvalidGlobalProductBarcode = "platform.global_catalog.product.barcode.invalid";
    public const string InvalidGlobalProductSku = "platform.global_catalog.product.sku.invalid";
    public const string InvalidGlobalProductBrand = "platform.global_catalog.product.brand.invalid";
    public const string InvalidGlobalProductCategory =
        "platform.global_catalog.product.category.required";
    public const string InvalidGlobalProductDescription =
        "platform.global_catalog.product.description.invalid";
    public const string InvalidGlobalProductImage = "platform.global_catalog.product.image.invalid";
    public const string InvalidGlobalProductMoney = "platform.global_catalog.product.money.invalid";
    public const string InvalidGlobalProductPriceRelationship =
        "platform.global_catalog.product.price.relationship.invalid";
    public const string InvalidGlobalProductSearchTag =
        "platform.global_catalog.product.search_tag.invalid";
    public const string InvalidGlobalProductUnit = "platform.global_catalog.product.unit.invalid";
    public const string InvalidGlobalProductSellingMode =
        "platform.global_catalog.product.selling_mode.invalid";
    public const string InvalidGlobalProductSellingModeUnit =
        "platform.global_catalog.product.selling_mode.unit.invalid";
    public const string InvalidGlobalProductSortField =
        "platform.global_catalog.product.sort.invalid";
    public const string InvalidGlobalCategorySortField =
        "platform.global_catalog.category.sort.invalid";
    public const string InvalidCatalogTemplateSortField =
        "platform.global_catalog.template.sort.invalid";
    public const string InvalidGlobalCatalogBusinessType =
        "platform.global_catalog.business_type.invalid";
    public const string GlobalCatalogConcurrencyConflict =
        "platform.global_catalog.concurrency_conflict";

    public const string InvalidCatalogTemplateId = "platform.global_catalog.template.id.invalid";
    public const string InvalidCatalogTemplateSlug = "platform.global_catalog.template.slug.invalid";
    public const string InvalidCatalogTemplateBatchSize =
        "platform.global_catalog.template.batch_size.invalid";
    public const string InvalidCatalogTemplateSelectionMode =
        "platform.global_catalog.template.selection_mode.invalid";
    public const string InvalidCatalogTemplateStatusTransition =
        "platform.global_catalog.template.status.invalid_transition";
    public const string CatalogTemplateProductDuplicate =
        "platform.global_catalog.template.product.duplicate";
    public const string CatalogTemplateProductNotFound =
        "platform.global_catalog.template.product.not_found";
    public const string CatalogTemplatePublishRequiresProducts =
        "platform.global_catalog.template.publish.requires_products";
    public const string CatalogTemplateCompositionOrderInvalid =
        "platform.global_catalog.template.composition.order_invalid";

    public const string InvalidCatalogImportJobId = "platform.global_catalog.import.job_id.invalid";
    public const string InvalidCatalogImportItemId = "platform.global_catalog.import.item_id.invalid";
    public const string InvalidCatalogImportStatusTransition =
        "platform.global_catalog.import.status.invalid_transition";
    public const string CatalogImportFileInvalid = "platform.global_catalog.import.file.invalid";
    public const string CatalogImportHeadersInvalid =
        "platform.global_catalog.import.headers.invalid";
    public const string CatalogImportFormulaInjection =
        "platform.global_catalog.import.formula_injection";
    public const string CatalogImportRowInvalid = "platform.global_catalog.import.row.invalid";
    public const string CatalogImportDuplicateInFile =
        "platform.global_catalog.import.duplicate_in_file";
    public const string CatalogImportNoConfirmableRows =
        "platform.global_catalog.import.no_confirmable_rows";
    public const string InvalidGlobalProductStatus =
        "platform.global_catalog.product.status.invalid";

    public const string InvalidComplianceStatusTransition =
        "platform.privacy_compliance.status.invalid_transition";
    public const string InvalidComplianceRequirementCode =
        "platform.privacy_compliance.requirement.code.invalid";
    public const string InvalidComplianceRequirementField =
        "platform.privacy_compliance.requirement.field.invalid";
    public const string InvalidComplianceEvidence =
        "platform.privacy_compliance.evidence.invalid";
    public const string InvalidProcessingSystemCode =
        "platform.privacy_compliance.processing_system.code.invalid";
    public const string InvalidProcessingSystemField =
        "platform.privacy_compliance.processing_system.field.invalid";
    public const string ComplianceRequirementNotFound =
        "platform.privacy_compliance.requirement.not_found";
    public const string ProcessingSystemNotFound =
        "platform.privacy_compliance.processing_system.not_found";

    public const string InvalidPersonalFeatureGrantSource =
        "platform.personal.feature.grant_source.invalid";
    public const string InvalidPersonalFeatureDuration =
        "platform.personal.feature_duration.invalid";

    public const string InvalidPersonalRewardPoints =
        "platform.personal.reward_points.invalid";
    public const string InsufficientPersonalRewardPoints =
        "platform.personal.reward_points.insufficient";
    public const string InvalidPersonalRewardSource =
        "platform.personal.reward_points.source.invalid";
    public const string InvalidPersonalRewardClaim =
        "platform.personal.reward_points.claim.invalid";
    public const string OrganizationRewardRedemptionUnsupported =
        "platform.personal.reward_points.organization_redemption_unsupported";
}
