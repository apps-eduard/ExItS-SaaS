namespace ExItS.Platform.Application.MigrationValidation;

public static class MigrationValidationErrorCodes
{
    public const string MigrationBatchInvalid = "application.migration.batch_invalid";
    public const string DuplicateIdentityMapping = "application.migration.duplicate_identity";
    public const string AmbiguousIdentityMapping = "application.migration.ambiguous_identity";
    public const string OrganizationMappingConflict = "application.migration.organization_conflict";
    public const string MembershipMappingConflict = "application.migration.membership_conflict";
    public const string UnsupportedMigrationContract = "application.migration.unsupported_contract";
    public const string MigrationValidationBlocked = "application.migration.blocked";
    public const string RollbackEvidenceMissing = "application.migration.rollback_evidence_missing";
    public const string SensitiveMigrationDataDetected = "application.migration.sensitive_data";
}

public static class MigrationFindingCodes
{
    public const string DuplicateNormalizedIdentifier = "migration.finding.duplicate_normalized_identifier";
    public const string PlatformUserAlreadyMapped = "migration.finding.platform_user_already_mapped";
    public const string ExternalUserAlreadyMapped = "migration.finding.external_user_already_mapped";
    public const string AmbiguousIdentityMatch = "migration.finding.ambiguous_identity_match";
    public const string MissingPlatformUser = "migration.finding.missing_platform_user";
    public const string MissingPlatformOrganization = "migration.finding.missing_platform_organization";
    public const string DuplicateExternalOrganization = "migration.finding.duplicate_external_organization";
    public const string ExternalOrganizationMappedElsewhere = "migration.finding.external_organization_mapped_elsewhere";
    public const string MembershipConflict = "migration.finding.membership_conflict";
    public const string ProductCodeMismatch = "migration.finding.product_code_mismatch";
    public const string UnsupportedContractVersion = "migration.finding.unsupported_contract_version";
    public const string SourceVersionRegression = "migration.finding.source_version_regression";
    public const string EntitlementSnapshotInvalid = "migration.finding.entitlement_snapshot_invalid";
    public const string NonUtcTimestamp = "migration.finding.non_utc_timestamp";
    public const string SensitiveFieldDetected = "migration.finding.sensitive_field_detected";
    public const string RollbackDataMissing = "migration.finding.rollback_data_missing";
    public const string EmptyIdentifier = "migration.finding.empty_identifier";
    public const string ClinicalRoleProhibited = "migration.finding.clinical_role_prohibited";
    public const string SuspendedMembershipNotActive = "migration.finding.suspended_membership_not_active";
    public const string DuplicateOrganizationMapping = "migration.finding.duplicate_organization_mapping";
    public const string DuplicateMembershipMapping = "migration.finding.duplicate_membership_mapping";
    public const string InvalidNormalizedIdentifier = "migration.finding.invalid_normalized_identifier";
}
