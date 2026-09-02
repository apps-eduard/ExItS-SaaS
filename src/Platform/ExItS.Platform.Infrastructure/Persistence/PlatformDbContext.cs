using ExItS.Platform.Infrastructure.Persistence.Access;
using ExItS.Platform.Infrastructure.Persistence.Audit;
using ExItS.Platform.Infrastructure.Persistence.Authorization;
using ExItS.Platform.Infrastructure.Persistence.Catalog;
using ExItS.Platform.Infrastructure.Persistence.Entitlements;
using ExItS.Platform.Infrastructure.Persistence.GlobalCatalog;
using ExItS.Platform.Infrastructure.Persistence.Governance;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Payments;
using ExItS.Platform.Infrastructure.Persistence.Personal;
using ExItS.Platform.Infrastructure.Persistence.PrivacyCompliance;
using ExItS.Platform.Infrastructure.Persistence.Settings;
using ExItS.Platform.Infrastructure.Persistence.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence;

public sealed class PlatformDbContext : DbContext
{
    public const string SchemaName = "platform";
    public const string CatalogSchemaName = "catalog";

    private static readonly string[] ActiveLikeStatuses =
    [
        "Trialing",
        "Active",
        "GracePeriod",
        "PastDue",
        "Suspended"
    ];

    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    internal DbSet<ProductRecord> Products => Set<ProductRecord>();
    internal DbSet<FeatureDefinitionRecord> FeatureDefinitions => Set<FeatureDefinitionRecord>();
    internal DbSet<PlanRecord> Plans => Set<PlanRecord>();
    internal DbSet<PlanVersionRecord> PlanVersions => Set<PlanVersionRecord>();
    internal DbSet<PlanVersionFeatureGrantRecord> PlanVersionFeatureGrants => Set<PlanVersionFeatureGrantRecord>();
    internal DbSet<PlanVersionBusinessTypeGrantRecord> PlanVersionBusinessTypeGrants => Set<PlanVersionBusinessTypeGrantRecord>();
    internal DbSet<TrialDefinitionRecord> TrialDefinitions => Set<TrialDefinitionRecord>();
    internal DbSet<TrialDefinitionFeatureGrantRecord> TrialDefinitionFeatureGrants => Set<TrialDefinitionFeatureGrantRecord>();
    internal DbSet<BusinessTypeRecord> BusinessTypes => Set<BusinessTypeRecord>();
    internal DbSet<GlobalCategoryRecord> GlobalCategories => Set<GlobalCategoryRecord>();
    internal DbSet<GlobalCategoryBusinessTypeRecord> GlobalCategoryBusinessTypes => Set<GlobalCategoryBusinessTypeRecord>();
    internal DbSet<GlobalProductRecord> GlobalProducts => Set<GlobalProductRecord>();
    internal DbSet<GlobalProductBusinessTypeRecord> GlobalProductBusinessTypes => Set<GlobalProductBusinessTypeRecord>();
    internal DbSet<GlobalProductImageRecord> GlobalProductImages => Set<GlobalProductImageRecord>();
    internal DbSet<CatalogTemplateRecord> CatalogTemplates => Set<CatalogTemplateRecord>();
    internal DbSet<CatalogTemplateProductRecord> CatalogTemplateProducts => Set<CatalogTemplateProductRecord>();
    internal DbSet<CatalogImportJobRecord> CatalogImportJobs => Set<CatalogImportJobRecord>();
    internal DbSet<CatalogImportItemRecord> CatalogImportItems => Set<CatalogImportItemRecord>();
    internal DbSet<PlatformOrganizationRecord> Organizations => Set<PlatformOrganizationRecord>();
    internal DbSet<OrganizationBusinessTypeActivationRecord> OrganizationBusinessTypeActivations =>
        Set<OrganizationBusinessTypeActivationRecord>();
    internal DbSet<OrganizationBranchRecord> OrganizationBranches => Set<OrganizationBranchRecord>();
    internal DbSet<BranchOperatingHoursRecord> BranchOperatingHours => Set<BranchOperatingHoursRecord>();
    internal DbSet<BranchDeliveryPolicyRecord> BranchDeliveryPolicies => Set<BranchDeliveryPolicyRecord>();
    internal DbSet<BranchDeliveryServiceAreaRecord> BranchDeliveryServiceAreas => Set<BranchDeliveryServiceAreaRecord>();
    internal DbSet<PosDeviceRecord> PosDevices => Set<PosDeviceRecord>();
    internal DbSet<PosDeviceRegistrationTokenRecord> PosDeviceRegistrationTokens => Set<PosDeviceRegistrationTokenRecord>();
    internal DbSet<SubscriptionRecord> Subscriptions => Set<SubscriptionRecord>();
    internal DbSet<SaaSPaymentRecord> SaaSPayments => Set<SaaSPaymentRecord>();
    internal DbSet<ProviderPaymentRecord> ProviderPayments => Set<ProviderPaymentRecord>();
    internal DbSet<FeatureOverrideRecord> FeatureOverrides => Set<FeatureOverrideRecord>();
    internal DbSet<EntitlementSnapshotRecord> EntitlementSnapshots => Set<EntitlementSnapshotRecord>();
    internal DbSet<EntitlementSnapshotGrantRecord> EntitlementSnapshotGrants => Set<EntitlementSnapshotGrantRecord>();
    internal DbSet<PlatformUserRecord> PlatformUsers => Set<PlatformUserRecord>();
    internal DbSet<PlatformUserCredentialRecord> PlatformUserCredentials => Set<PlatformUserCredentialRecord>();
    internal DbSet<PlatformAuthSessionRecord> PlatformAuthSessions => Set<PlatformAuthSessionRecord>();
    internal DbSet<AccountProfileRecord> AccountProfiles => Set<AccountProfileRecord>();
    internal DbSet<OrganizationContextPreferenceRecord> OrganizationContextPreferences =>
        Set<OrganizationContextPreferenceRecord>();
    internal DbSet<PlatformAccessTokenRecord> PlatformAccessTokens => Set<PlatformAccessTokenRecord>();
    internal DbSet<PlatformDeviceRecoveryCredentialRecord> PlatformDeviceRecoveryCredentials =>
        Set<PlatformDeviceRecoveryCredentialRecord>();
    internal DbSet<PlatformCredentialTokenRecord> PlatformCredentialTokens => Set<PlatformCredentialTokenRecord>();
    internal DbSet<GovernanceStepUpGrantRecord> GovernanceStepUpGrants => Set<GovernanceStepUpGrantRecord>();
    internal DbSet<PlatformExternalLoginRecord> PlatformExternalLogins => Set<PlatformExternalLoginRecord>();
    internal DbSet<OrganizationMembershipRecord> OrganizationMemberships => Set<OrganizationMembershipRecord>();
    internal DbSet<OrganizationMembershipBranchAssignmentRecord> OrganizationMembershipBranchAssignments =>
        Set<OrganizationMembershipBranchAssignmentRecord>();
    internal DbSet<OrganizationInvitationRecord> OrganizationInvitations => Set<OrganizationInvitationRecord>();
    internal DbSet<OrganizationOwnershipTransferRecord> OrganizationOwnershipTransfers =>
        Set<OrganizationOwnershipTransferRecord>();
    internal DbSet<BusinessCustomerRecord> BusinessCustomers => Set<BusinessCustomerRecord>();
    internal DbSet<CreditCustomerRecord> CreditCustomers => Set<CreditCustomerRecord>();
    internal DbSet<CustomerLinkRequestRecord> CustomerLinkRequests => Set<CustomerLinkRequestRecord>();
    internal DbSet<LinkedCustomerAppUserRecord> LinkedCustomerAppUsers => Set<LinkedCustomerAppUserRecord>();
    internal DbSet<PersonalOrganizationConnectionBlockRecord> PersonalOrganizationConnectionBlocks =>
        Set<PersonalOrganizationConnectionBlockRecord>();
    internal DbSet<OrganizationInAppNotificationRecord> OrganizationInAppNotifications =>
        Set<OrganizationInAppNotificationRecord>();
    internal DbSet<BusinessCreditOpeningBalanceRecord> BusinessCreditOpeningBalances => Set<BusinessCreditOpeningBalanceRecord>();
    internal DbSet<ProductLocalRoleGrantRecord> ProductLocalRoleGrants => Set<ProductLocalRoleGrantRecord>();
    internal DbSet<ProductAccessAssignmentRecord> ProductAccessAssignments => Set<ProductAccessAssignmentRecord>();
    internal DbSet<PlatformRoleAssignmentRecord> PlatformRoleAssignments => Set<PlatformRoleAssignmentRecord>();
    internal DbSet<PlatformRoleDefinitionRecord> PlatformRoleDefinitions => Set<PlatformRoleDefinitionRecord>();
    internal DbSet<PlatformCustomRoleAssignmentRecord> PlatformCustomRoleAssignments => Set<PlatformCustomRoleAssignmentRecord>();
    internal DbSet<OrganizationRoleDefinitionRecord> OrganizationRoleDefinitions => Set<OrganizationRoleDefinitionRecord>();
    internal DbSet<OrganizationCustomRoleAssignmentRecord> OrganizationCustomRoleAssignments => Set<OrganizationCustomRoleAssignmentRecord>();
    internal DbSet<AuditRecordRecord> AuditRecords => Set<AuditRecordRecord>();
    internal DbSet<OrganizationSalesDocumentCapabilityRecord> OrganizationSalesDocumentCapabilities =>
        Set<OrganizationSalesDocumentCapabilityRecord>();
    internal DbSet<OrganizationComplianceProfileRecord> OrganizationComplianceProfiles =>
        Set<OrganizationComplianceProfileRecord>();
    internal DbSet<BranchComplianceProfileRecord> BranchComplianceProfiles =>
        Set<BranchComplianceProfileRecord>();
    internal DbSet<ComplianceRegistrationRecordEntity> ComplianceRegistrationRecords =>
        Set<ComplianceRegistrationRecordEntity>();
    internal DbSet<OrganizationSalesDocumentAcknowledgmentRecord> OrganizationSalesDocumentAcknowledgments =>
        Set<OrganizationSalesDocumentAcknowledgmentRecord>();
    internal DbSet<PersonalAccountSettingsRecord> PersonalAccountSettings => Set<PersonalAccountSettingsRecord>();
    internal DbSet<PlatformSettingsRecord> PlatformSettings => Set<PlatformSettingsRecord>();
    internal DbSet<PersonalContactRecord> PersonalContacts => Set<PersonalContactRecord>();
    internal DbSet<PersonalConnectionRequestRecord> PersonalConnectionRequests => Set<PersonalConnectionRequestRecord>();
    internal DbSet<PersonalDebtRelationshipRecord> PersonalDebtRelationships => Set<PersonalDebtRelationshipRecord>();
    internal DbSet<PersonalUtangEntryRecord> PersonalUtangEntries => Set<PersonalUtangEntryRecord>();
    internal DbSet<PersonalUtangInvitationRecord> PersonalUtangInvitations => Set<PersonalUtangInvitationRecord>();
    internal DbSet<PersonalReminderRecord> PersonalReminders => Set<PersonalReminderRecord>();
    internal DbSet<PersonalInAppNotificationRecord> PersonalInAppNotifications => Set<PersonalInAppNotificationRecord>();
    internal DbSet<PersonalNotificationDeliveryRecord> PersonalNotificationDeliveries => Set<PersonalNotificationDeliveryRecord>();
    internal DbSet<PersonalUtangMigrationBatchRecord> PersonalUtangMigrationBatches => Set<PersonalUtangMigrationBatchRecord>();
    internal DbSet<PersonalUtangMigrationItemRecord> PersonalUtangMigrationItems => Set<PersonalUtangMigrationItemRecord>();
    internal DbSet<PersonalFeatureDefinitionRecord> PersonalFeatureDefinitions => Set<PersonalFeatureDefinitionRecord>();
    internal DbSet<PersonalFeatureEntitlementRecord> PersonalFeatureEntitlements => Set<PersonalFeatureEntitlementRecord>();
    internal DbSet<PersonalRewardBalanceRecord> PersonalRewardBalances => Set<PersonalRewardBalanceRecord>();
    internal DbSet<PersonalRewardTransactionRecord> PersonalRewardTransactions => Set<PersonalRewardTransactionRecord>();
    internal DbSet<PersonalRewardClaimRecord> PersonalRewardClaims => Set<PersonalRewardClaimRecord>();
    internal DbSet<PersonalTodoRecord> PersonalTodos => Set<PersonalTodoRecord>();
    internal DbSet<ComplianceRequirementRecord> ComplianceRequirements => Set<ComplianceRequirementRecord>();
    internal DbSet<ComplianceEvidenceRecord> ComplianceEvidence => Set<ComplianceEvidenceRecord>();
    internal DbSet<ProcessingSystemRecordEntity> ProcessingSystems => Set<ProcessingSystemRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<ProductRecord>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<FeatureDefinitionRecord>(entity =>
        {
            entity.ToTable("feature_definitions");
            entity.HasKey(e => new { e.ProductCode, e.FeatureCode });
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.FeatureCode).HasColumnName("feature_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
            entity.Property(e => e.ValueType).HasColumnName("value_type").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<PlanRecord>(entity =>
        {
            entity.ToTable("plans");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => new { e.ProductCode, e.Code }).IsUnique();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.MaxBranches).HasColumnName("max_branches").HasDefaultValue(1);
            entity.Property(e => e.MaxActiveStaff).HasColumnName("max_active_staff").HasDefaultValue(3);
            entity.Property(e => e.MaxActivePosDevices).HasColumnName("max_active_pos_devices").HasDefaultValue(1);
            entity.Property(e => e.MaxActiveBusinessTypes).HasColumnName("max_active_business_types").HasDefaultValue(1);
            entity.Property(e => e.CustomerCreditEnabled).HasColumnName("customer_credit_enabled").HasDefaultValue(false);
            entity.Property(e => e.AdvancedReportsEnabled).HasColumnName("advanced_reports_enabled").HasDefaultValue(false);
            entity.Property(e => e.ExportEnabled).HasColumnName("export_enabled").HasDefaultValue(false);
            entity.Property(e => e.TrialAllowed).HasColumnName("trial_allowed").HasDefaultValue(true);
            entity.Property(e => e.DefaultTrialDays).HasColumnName("default_trial_days").HasDefaultValue(14);
            entity.Property(e => e.SortOrder).HasColumnName("sort_order").HasDefaultValue(100);
            entity.Property(e => e.MonthlyPrice).HasColumnName("monthly_price").HasColumnType("numeric(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.AnnualPrice).HasColumnName("annual_price").HasColumnType("numeric(18,2)").HasDefaultValue(0m);
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).HasDefaultValue("PHP");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<PlanVersionRecord>(entity =>
        {
            entity.ToTable("plan_versions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.VersionNumber).HasColumnName("version_number");
            entity.HasIndex(e => new { e.PlanId, e.VersionNumber }).IsUnique();
            entity.Property(e => e.EffectiveFromUtc).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveToUtc).HasColumnName("effective_to");
            entity.Property(e => e.BillingPeriod).HasColumnName("billing_period").HasMaxLength(32).IsRequired();
            entity.Property(e => e.TrialEligible).HasColumnName("trial_eligible");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.PublishedAtUtc).HasColumnName("published_at_utc");
            entity.HasOne(e => e.Plan)
                .WithMany()
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlanVersionFeatureGrantRecord>(entity =>
        {
            entity.ToTable("plan_version_feature_grants");
            entity.HasKey(e => new { e.PlanVersionId, e.FeatureCode });
            entity.Property(e => e.PlanVersionId).HasColumnName("plan_version_id");
            entity.Property(e => e.FeatureCode).HasColumnName("feature_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.NumericLimit).HasColumnName("numeric_limit");
            entity.HasOne(e => e.PlanVersion)
                .WithMany(v => v.FeatureGrants)
                .HasForeignKey(e => e.PlanVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlanVersionBusinessTypeGrantRecord>(entity =>
        {
            entity.ToTable("plan_version_business_type_grants");
            entity.HasKey(e => new { e.PlanVersionId, e.BusinessTypeId });
            entity.Property(e => e.PlanVersionId).HasColumnName("plan_version_id");
            entity.Property(e => e.BusinessTypeId).HasColumnName("business_type_id");
            entity.HasIndex(e => e.BusinessTypeId)
                .HasDatabaseName("ix_plan_version_business_type_grants_business_type_id");
            entity.HasOne(e => e.PlanVersion)
                .WithMany(v => v.BusinessTypeGrants)
                .HasForeignKey(e => e.PlanVersionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<BusinessTypeRecord>()
                .WithMany()
                .HasForeignKey(e => e.BusinessTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TrialDefinitionRecord>(entity =>
        {
            entity.ToTable("trial_definitions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
            entity.Property(e => e.DurationTicks).HasColumnName("duration_ticks");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<TrialDefinitionFeatureGrantRecord>(entity =>
        {
            entity.ToTable("trial_definition_feature_grants");
            entity.HasKey(e => new { e.TrialDefinitionId, e.FeatureCode, e.GrantKind });
            entity.Property(e => e.TrialDefinitionId).HasColumnName("trial_definition_id");
            entity.Property(e => e.FeatureCode).HasColumnName("feature_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.GrantKind).HasColumnName("grant_kind").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.NumericLimit).HasColumnName("numeric_limit");
            entity.HasOne(e => e.TrialDefinition)
                .WithMany(t => t.FeatureGrants)
                .HasForeignKey(e => e.TrialDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlatformOrganizationRecord>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(256).IsRequired();
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.Property(e => e.PublicOrganizationId)
                .HasColumnName("public_organization_id")
                .HasMaxLength(16);
            entity.HasIndex(e => e.PublicOrganizationId)
                .IsUnique()
                .HasFilter("public_organization_id IS NOT NULL")
                .HasDatabaseName("ux_organizations_public_organization_id");
            entity.Property(e => e.PrimaryBusinessTypeId).HasColumnName("primary_business_type_id");
            entity.HasIndex(e => e.PrimaryBusinessTypeId).HasDatabaseName("ix_organizations_primary_business_type_id");
            entity.HasOne<BusinessTypeRecord>()
                .WithMany()
                .HasForeignKey(e => e.PrimaryBusinessTypeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.LegalName).HasColumnName("legal_name").HasMaxLength(100);
            entity.Property(e => e.ContactEmail).HasColumnName("contact_email").HasMaxLength(320);
            entity.Property(e => e.ContactPhone).HasColumnName("contact_phone").HasMaxLength(32);
            entity.Property(e => e.AddressLine1).HasColumnName("address_line1").HasMaxLength(200);
            entity.Property(e => e.AddressLine2).HasColumnName("address_line2").HasMaxLength(200);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.Region).HasColumnName("region").HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasColumnName("postal_code").HasMaxLength(32);
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2);
            entity.Property(e => e.TimeZoneId).HasColumnName("time_zone_id").HasMaxLength(100);
            entity.Property(e => e.Locale).HasColumnName("locale").HasMaxLength(16);
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
            entity.Property(e => e.BrandDisplayName).HasColumnName("brand_display_name").HasMaxLength(100);
            entity.Property(e => e.LogoUrl).HasColumnName("logo_url").HasMaxLength(2048);
            entity.Property(e => e.PrimaryColor).HasColumnName("primary_color").HasMaxLength(7);
            entity.Property(e => e.AccentColor).HasColumnName("accent_color").HasMaxLength(7);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<OrganizationSalesDocumentCapabilityRecord>(entity =>
        {
            entity.ToTable("organization_sales_document_capabilities");
            entity.HasKey(e => e.OrganizationId);
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.ComplianceEligibilityStatus)
                .HasColumnName("compliance_eligibility_status")
                .HasMaxLength(64)
                .HasDefaultValue("NotRequested")
                .IsRequired();
            entity.Property(e => e.TaxDocumentIssuanceEnabled)
                .HasColumnName("tax_document_issuance_enabled")
                .HasDefaultValue(false);
            entity.Property(e => e.TaxConfigurationEnabled)
                .HasColumnName("tax_configuration_enabled")
                .HasDefaultValue(false);
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.UpdatedByActorReference)
                .HasColumnName("updated_by_actor_reference")
                .HasMaxLength(256);
            entity.HasOne<PlatformOrganizationRecord>()
                .WithOne()
                .HasForeignKey<OrganizationSalesDocumentCapabilityRecord>(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationComplianceProfileRecord>(entity =>
        {
            entity.ToTable("organization_compliance_profiles");
            entity.HasKey(e => e.OrganizationId);
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.RegisteredTaxpayerName)
                .HasColumnName("registered_taxpayer_name")
                .HasMaxLength(200);
            entity.Property(e => e.TinNormalized)
                .HasColumnName("tin_normalized")
                .HasMaxLength(9);
            entity.Property(e => e.SetupStatus)
                .HasColumnName("setup_status")
                .HasMaxLength(64)
                .HasDefaultValue("NotConfigured")
                .IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.UpdatedByActorReference)
                .HasColumnName("updated_by_actor_reference")
                .HasMaxLength(256);
            entity.HasOne<PlatformOrganizationRecord>()
                .WithOne()
                .HasForeignKey<OrganizationComplianceProfileRecord>(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BranchComplianceProfileRecord>(entity =>
        {
            entity.ToTable("branch_compliance_profiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.OrganizationBranchId).HasColumnName("organization_branch_id");
            entity.Property(e => e.BirBranchCode)
                .HasColumnName("bir_branch_code")
                .HasMaxLength(10);
            entity.Property(e => e.SetupStatus)
                .HasColumnName("setup_status")
                .HasMaxLength(64)
                .HasDefaultValue("NotConfigured")
                .IsRequired();
            entity.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasMaxLength(1000);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.UpdatedByActorReference)
                .HasColumnName("updated_by_actor_reference")
                .HasMaxLength(256);
            entity.HasIndex(e => e.OrganizationBranchId)
                .IsUnique()
                .HasDatabaseName("UX_branch_compliance_profiles_branch");
            entity.HasIndex(e => e.OrganizationId)
                .HasDatabaseName("IX_branch_compliance_profiles_org");
            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<OrganizationBranchRecord>()
                .WithOne()
                .HasForeignKey<BranchComplianceProfileRecord>(e => e.OrganizationBranchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ComplianceRegistrationRecordEntity>(entity =>
        {
            entity.ToTable("compliance_registration_records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.OrganizationBranchId).HasColumnName("organization_branch_id");
            entity.Property(e => e.RegistrationType)
                .HasColumnName("registration_type")
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(e => e.ReferenceNumber)
                .HasColumnName("reference_number")
                .HasMaxLength(128);
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(e => e.EvidenceReference)
                .HasColumnName("evidence_reference")
                .HasMaxLength(256);
            entity.Property(e => e.DocumentType)
                .HasColumnName("document_type")
                .HasMaxLength(64);
            entity.Property(e => e.IssuedAt).HasColumnName("issued_at");
            entity.Property(e => e.EffectiveAt).HasColumnName("effective_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(e => e.RecordedBy)
                .HasColumnName("recorded_by")
                .HasMaxLength(256)
                .IsRequired();
            entity.Property(e => e.ReviewedAtUtc).HasColumnName("reviewed_at_utc");
            entity.Property(e => e.ReviewedBy)
                .HasColumnName("reviewed_by")
                .HasMaxLength(256);
            entity.Property(e => e.ReviewNotes)
                .HasColumnName("review_notes")
                .HasMaxLength(1000);
            entity.HasIndex(e => e.OrganizationId)
                .HasDatabaseName("IX_compliance_registration_records_org");
            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<OrganizationBranchRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationBranchId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<OrganizationSalesDocumentAcknowledgmentRecord>(entity =>
        {
            entity.ToTable("organization_sales_document_acknowledgments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Version).HasColumnName("version").HasMaxLength(128).IsRequired();
            entity.Property(e => e.AcknowledgedAtUtc).HasColumnName("acknowledged_at_utc");
            entity.Property(e => e.ContentKey).HasColumnName("content_key").HasMaxLength(128);
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_sales_document_ack_user");
            entity.HasIndex(e => new { e.OrganizationId, e.UserId, e.Version })
                .IsUnique()
                .HasDatabaseName("UX_sales_document_ack_org_user_version");
            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationBusinessTypeActivationRecord>(entity =>
        {
            entity.ToTable("organization_business_type_activations");
            entity.HasKey(e => new { e.OrganizationId, e.BusinessTypeId });
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessTypeId).HasColumnName("business_type_id");
            entity.Property(e => e.ActivatedAtUtc).HasColumnName("activated_at_utc");
            entity.HasIndex(e => e.BusinessTypeId)
                .HasDatabaseName("ix_organization_business_type_activations_business_type_id");
            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<BusinessTypeRecord>()
                .WithMany()
                .HasForeignKey(e => e.BusinessTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationBranchRecord>(entity =>
        {
            entity.ToTable("organization_branches", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_organization_branches_latitude",
                    "latitude IS NULL OR (latitude >= -90 AND latitude <= 90)");
                tb.HasCheckConstraint(
                    "ck_organization_branches_longitude",
                    "longitude IS NULL OR (longitude >= -180 AND longitude <= 180)");
                tb.HasCheckConstraint(
                    "ck_organization_branches_lat_long_pair",
                    "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL)");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.AddressLine1).HasColumnName("address_line1").HasMaxLength(200);
            entity.Property(e => e.AddressLine2).HasColumnName("address_line2").HasMaxLength(200);
            entity.Property(e => e.City).HasColumnName("city").HasMaxLength(100);
            entity.Property(e => e.Region).HasColumnName("region").HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasColumnName("postal_code").HasMaxLength(32);
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2);
            entity.Property(e => e.Latitude).HasColumnName("latitude").HasPrecision(10, 7);
            entity.Property(e => e.Longitude).HasColumnName("longitude").HasPrecision(10, 7);
            entity.Property(e => e.PickupEnabled).HasColumnName("pickup_enabled").HasDefaultValue(false);
            entity.Property(e => e.DeliveryEnabled).HasColumnName("delivery_enabled").HasDefaultValue(false);
            entity.Property(e => e.CustomerOrderingEnabled).HasColumnName("customer_ordering_enabled").HasDefaultValue(false);
            entity.Property(e => e.ContactPhone).HasColumnName("contact_phone").HasMaxLength(32);
            entity.Property(e => e.TimeZoneId).HasColumnName("time_zone_id").HasMaxLength(64);
            entity.Property(e => e.OnlineOrdersPaused).HasColumnName("online_orders_paused").HasDefaultValue(false);
            entity.Property(e => e.OnlineOrdersPauseReason).HasColumnName("online_orders_pause_reason").HasMaxLength(32);
            entity.Property(e => e.IsPrimary).HasColumnName("is_primary");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(e => e.SuspendedAtUtc).HasColumnName("suspended_at_utc");
            entity.Property(e => e.SuspendedByUserId).HasColumnName("suspended_by_user_id");
            entity.Property(e => e.SuspensionReason).HasColumnName("suspension_reason").HasMaxLength(500);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(e => new { e.OrganizationId, e.Code }).IsUnique();
            // One primary branch per organization (partial unique index).
            entity.HasIndex(e => e.OrganizationId)
                .IsUnique()
                .HasFilter("is_primary = TRUE")
                .HasDatabaseName("ux_organization_branches_one_primary");
            // Alternate key backs composite tenant FKs; do not also declare a redundant unique HasIndex.
            entity.HasAlternateKey(e => new { e.Id, e.OrganizationId })
                .HasName("AK_organization_branches_id_organization_id");
            entity.HasIndex(e => e.Status);
            entity.HasOne<PlatformOrganizationRecord>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BranchOperatingHoursRecord>(entity =>
        {
            entity.ToTable("branch_operating_hours");
            entity.HasKey(e => new { e.BranchId, e.DayOfWeek });
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.IsClosed).HasColumnName("is_closed");
            entity.Property(e => e.IsOpen24Hours).HasColumnName("is_open_24_hours");
            entity.Property(e => e.OpenTime).HasColumnName("open_time");
            entity.Property(e => e.CloseTime).HasColumnName("close_time");
            entity.HasIndex(e => e.OrganizationId);
            entity.HasOne<OrganizationBranchRecord>()
                .WithMany()
                .HasForeignKey(e => new { e.BranchId, e.OrganizationId })
                .HasPrincipalKey(b => new { b.Id, b.OrganizationId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BranchDeliveryPolicyRecord>(entity =>
        {
            entity.ToTable("branch_delivery_policies", tb =>
            {
                tb.HasCheckConstraint("ck_branch_delivery_policies_min_order_nonneg", "minimum_order_amount >= 0");
                tb.HasCheckConstraint("ck_branch_delivery_policies_base_fee_nonneg", "base_delivery_fee >= 0");
                tb.HasCheckConstraint("ck_branch_delivery_policies_included_nonneg", "included_distance_km >= 0");
                tb.HasCheckConstraint("ck_branch_delivery_policies_per_km_nonneg", "additional_fee_per_km >= 0");
                tb.HasCheckConstraint("ck_branch_delivery_policies_max_positive", "maximum_delivery_distance_km > 0");
                tb.HasCheckConstraint(
                    "ck_branch_delivery_policies_free_threshold_nonneg",
                    "free_delivery_threshold IS NULL OR free_delivery_threshold >= 0");
            });
            entity.HasKey(e => e.BranchId);
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.MinimumOrderAmount).HasColumnName("minimum_order_amount").HasPrecision(18, 2);
            entity.Property(e => e.BaseDeliveryFee).HasColumnName("base_delivery_fee").HasPrecision(18, 2);
            entity.Property(e => e.IncludedDistanceKm).HasColumnName("included_distance_km").HasPrecision(18, 3);
            entity.Property(e => e.AdditionalFeePerKm).HasColumnName("additional_fee_per_km").HasPrecision(18, 2);
            entity.Property(e => e.MaximumDeliveryDistanceKm).HasColumnName("maximum_delivery_distance_km").HasPrecision(18, 3);
            entity.Property(e => e.FreeDeliveryThreshold).HasColumnName("free_delivery_threshold").HasPrecision(18, 2);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(e => e.OrganizationId);
            // Composite tenant FK: policy org must match the branch's organization.
            entity.HasOne<OrganizationBranchRecord>()
                .WithMany()
                .HasForeignKey(e => new { e.BranchId, e.OrganizationId })
                .HasPrincipalKey(b => new { b.Id, b.OrganizationId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BranchDeliveryServiceAreaRecord>(entity =>
        {
            entity.ToTable("branch_delivery_service_areas");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
            entity.Property(e => e.RegionOrProvinceName).HasColumnName("region_or_province_name").HasMaxLength(100);
            entity.Property(e => e.CityMunicipalityName).HasColumnName("city_municipality_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NormalizedCityMunicipalityName)
                .HasColumnName("normalized_city_municipality_name")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(e => e.ExternalAreaCode).HasColumnName("external_area_code").HasMaxLength(64);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => new { e.BranchId, e.ExternalAreaCode })
                .IsUnique()
                .HasFilter("is_active = TRUE AND external_area_code IS NOT NULL")
                .HasDatabaseName("ux_branch_delivery_service_areas_active_psgc");
            entity.HasIndex(e => new { e.BranchId, e.NormalizedCityMunicipalityName })
                .HasDatabaseName("ix_branch_delivery_service_areas_branch_city");
            entity.HasOne<OrganizationBranchRecord>()
                .WithMany()
                .HasForeignKey(e => new { e.BranchId, e.OrganizationId })
                .HasPrincipalKey(b => new { b.Id, b.OrganizationId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PosDeviceRecord>(entity =>
        {
            entity.ToTable("pos_devices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.InstallationDeviceId).HasColumnName("installation_device_id").HasMaxLength(128).IsRequired();
            entity.Property(e => e.FriendlyName).HasColumnName("friendly_name").HasMaxLength(128).IsRequired();
            entity.Property(e => e.Platform).HasColumnName("platform").HasMaxLength(64);
            entity.Property(e => e.Model).HasColumnName("model").HasMaxLength(128);
            entity.Property(e => e.AppVersion).HasColumnName("app_version").HasMaxLength(64);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(e => e.RegisteredAtUtc).HasColumnName("registered_at_utc");
            entity.Property(e => e.LastSeenAtUtc).HasColumnName("last_seen_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RevokedByUserId).HasColumnName("revoked_by_user_id");
            entity.HasIndex(e => new { e.OrganizationId, e.InstallationDeviceId }).IsUnique();
            entity.HasIndex(e => e.BranchId);
            entity.HasIndex(e => e.Status);
            entity.HasOne<PlatformOrganizationRecord>().WithMany().HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<OrganizationBranchRecord>().WithMany().HasForeignKey(e => e.BranchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUserRecord>().WithMany().HasForeignKey(e => e.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PosDeviceRegistrationTokenRecord>(entity =>
        {
            entity.ToTable("pos_device_registration_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.RedeemedAtUtc).HasColumnName("redeemed_at_utc");
            entity.Property(e => e.RedeemedByInstallationDeviceId)
                .HasColumnName("redeemed_by_installation_device_id")
                .HasMaxLength(128);
            entity.Property(e => e.RedeemedPosDeviceId).HasColumnName("redeemed_pos_device_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("ux_pos_device_registration_tokens_token_hash");
            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_pos_device_registration_tokens_org_status");
            entity.HasIndex(e => e.ExpiresAtUtc).HasDatabaseName("ix_pos_device_registration_tokens_expires");
            entity.HasOne<PlatformOrganizationRecord>().WithMany().HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUserRecord>().WithMany().HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PosDeviceRecord>().WithMany().HasForeignKey(e => e.RedeemedPosDeviceId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SubscriptionRecord>(entity =>
        {
            entity.ToTable("subscriptions", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_subscriptions_trial_range",
                    "trial_start_utc IS NULL OR trial_end_utc IS NULL OR trial_end_utc > trial_start_utc");
                tb.HasCheckConstraint(
                    "ck_subscriptions_paid_range",
                    "paid_period_start_utc IS NULL OR paid_period_end_utc IS NULL OR paid_period_end_utc > paid_period_start_utc");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.PlanVersionId).HasColumnName("plan_version_id");
            entity.Property(e => e.TrialDefinitionId).HasColumnName("trial_definition_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.TrialStartUtc).HasColumnName("trial_start_utc");
            entity.Property(e => e.TrialEndUtc).HasColumnName("trial_end_utc");
            entity.Property(e => e.PaidPeriodStartUtc).HasColumnName("paid_period_start_utc");
            entity.Property(e => e.PaidPeriodEndUtc).HasColumnName("paid_period_end_utc");
            entity.Property(e => e.GracePeriodEndUtc).HasColumnName("grace_period_end_utc");
            entity.Property(e => e.SuspendedAtUtc).HasColumnName("suspended_at_utc");
            entity.Property(e => e.CancelledAtUtc).HasColumnName("cancelled_at_utc");
            entity.Property(e => e.PastDueAtUtc).HasColumnName("past_due_at_utc");
            entity.Property(e => e.ExpiredAtUtc).HasColumnName("expired_at_utc");
            entity.Property(e => e.BillingCycle).HasColumnName("billing_cycle").HasMaxLength(16);
            entity.Property(e => e.AgreedPrice).HasColumnName("agreed_price").HasColumnType("numeric(18,2)");
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
            entity.Property(e => e.PriceEffectiveFromUtc).HasColumnName("price_effective_from_utc");
            entity.Property(e => e.PendingPlanId).HasColumnName("pending_plan_id");
            entity.Property(e => e.PendingPlanEffectiveAtUtc).HasColumnName("pending_plan_effective_at_utc");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.AggregateVersion).HasColumnName("aggregate_version");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.ProductCode })
                .IsUnique()
                .HasDatabaseName("ux_subscriptions_one_active_like")
                .HasFilter($"status IN ({string.Join(", ", ActiveLikeStatuses.Select(s => $"'{s}'"))})");

            entity.HasOne<Organizations.PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Catalog.PlanRecord>()
                .WithMany()
                .HasForeignKey(e => e.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Catalog.PlanVersionRecord>()
                .WithMany()
                .HasForeignKey(e => e.PlanVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Catalog.TrialDefinitionRecord>()
                .WithMany()
                .HasForeignKey(e => e.TrialDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaaSPaymentRecord>(entity =>
        {
            entity.ToTable("saas_payments", tb =>
            {
                tb.HasCheckConstraint("ck_saas_payments_positive_amount", "amount > 0");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,4)");
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(e => e.Method).HasColumnName("method").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.ExternalReference).HasColumnName("external_reference").HasMaxLength(512).IsRequired();
            entity.Property(e => e.NormalizedReference).HasColumnName("normalized_reference").HasMaxLength(512).IsRequired();
            entity.Property(e => e.PaidAtUtc).HasColumnName("paid_at_utc");
            entity.Property(e => e.ConfirmedAtUtc).HasColumnName("confirmed_at_utc");
            entity.Property(e => e.ConfirmedBy).HasColumnName("confirmed_by").HasMaxLength(256);
            entity.Property(e => e.RejectedAtUtc).HasColumnName("rejected_at_utc");
            entity.Property(e => e.RejectedBy).HasColumnName("rejected_by").HasMaxLength(256);
            entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1024);
            entity.Property(e => e.VoidedAtUtc).HasColumnName("voided_at_utc");
            entity.Property(e => e.VoidedBy).HasColumnName("voided_by").HasMaxLength(256);
            entity.Property(e => e.VoidReason).HasColumnName("void_reason").HasMaxLength(1024);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.AggregateVersion).HasColumnName("aggregate_version");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.Method, e.NormalizedReference, e.OrganizationId })
                .IsUnique()
                .HasDatabaseName("ux_saas_payments_reference")
                .HasFilter("status NOT IN ('Rejected', 'Voided')");

            entity.HasOne<Organizations.PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Subscriptions.SubscriptionRecord>()
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProviderPaymentRecord>(entity =>
        {
            entity.ToTable("provider_payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)");
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
            entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(64).IsRequired();
            entity.Property(e => e.ProviderReference).HasColumnName("provider_reference").HasMaxLength(128).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.IsTest).HasColumnName("is_test");
            entity.Property(e => e.FailureCode).HasColumnName("failure_code").HasMaxLength(64);
            entity.Property(e => e.FailureMessage).HasColumnName("failure_message").HasMaxLength(512);
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.Property(e => e.Purpose).HasColumnName("purpose").HasMaxLength(128);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");

            entity.HasOne<Subscriptions.SubscriptionRecord>()
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FeatureOverrideRecord>(entity =>
        {
            entity.ToTable("feature_overrides", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_feature_overrides_expiry_range",
                    "expires_at_utc IS NULL OR expires_at_utc > effective_from_utc");
                tb.HasCheckConstraint(
                    "ck_feature_overrides_numeric_limit",
                    "numeric_limit IS NULL OR numeric_limit >= 0");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.FeatureCode).HasColumnName("feature_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.NumericLimit).HasColumnName("numeric_limit");
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1024).IsRequired();
            entity.Property(e => e.EffectiveFromUtc).HasColumnName("effective_from_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RevokedByUserId).HasColumnName("revoked_by_user_id");
            entity.Property(e => e.RevocationReason).HasColumnName("revocation_reason").HasMaxLength(1024);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.ProductCode, e.FeatureCode });

            entity.HasOne<Organizations.PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EntitlementSnapshotRecord>(entity =>
        {
            entity.ToTable("entitlement_snapshots", tb =>
            {
                tb.HasCheckConstraint("ck_entitlement_snapshots_version_positive", "snapshot_version > 0");
                tb.HasCheckConstraint("ck_entitlement_snapshots_schema_positive", "schema_version > 0");
                tb.HasCheckConstraint("ck_entitlement_snapshots_refresh_range", "refresh_by_utc >= generated_at_utc");
                tb.HasCheckConstraint(
                    "ck_entitlement_snapshots_expiry_range",
                    "expires_at_utc IS NULL OR expires_at_utc >= effective_at_utc");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(e => e.PlanCode).HasColumnName("plan_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.PlanVersionNumber).HasColumnName("plan_version_number");
            entity.Property(e => e.SnapshotVersion).HasColumnName("snapshot_version");
            entity.Property(e => e.SchemaVersion).HasColumnName("schema_version");
            entity.Property(e => e.SubscriptionStatus).HasColumnName("subscription_status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.InGracePeriod).HasColumnName("in_grace_period");
            entity.Property(e => e.GeneratedAtUtc).HasColumnName("generated_at_utc");
            entity.Property(e => e.EffectiveAtUtc).HasColumnName("effective_at_utc");
            entity.Property(e => e.RefreshByUtc).HasColumnName("refresh_by_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.SourceAggregateVersion).HasColumnName("source_aggregate_version");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");

            entity.HasIndex(e => new { e.OrganizationId, e.ProductCode, e.SnapshotVersion })
                .IsUnique()
                .HasDatabaseName("ux_entitlement_snapshots_org_product_version");

            entity.HasOne<Organizations.PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<Subscriptions.SubscriptionRecord>()
                .WithMany()
                .HasForeignKey(e => e.SubscriptionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EntitlementSnapshotGrantRecord>(entity =>
        {
            entity.ToTable("entitlement_snapshot_grants");
            entity.HasKey(e => new { e.SnapshotId, e.FeatureCode });
            entity.Property(e => e.SnapshotId).HasColumnName("snapshot_id");
            entity.Property(e => e.FeatureCode).HasColumnName("feature_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Enabled).HasColumnName("enabled");
            entity.Property(e => e.NumericLimit).HasColumnName("numeric_limit");
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(32).IsRequired();
            entity.Property(e => e.EffectiveAtUtc).HasColumnName("effective_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");

            entity.HasOne(e => e.Snapshot)
                .WithMany(s => s.Grants)
                .HasForeignKey(e => e.SnapshotId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlatformUserRecord>(entity =>
        {
            entity.ToTable("platform_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(64).IsRequired();
            entity.Property(e => e.NormalizedUsername).HasColumnName("normalized_username").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.NormalizedUsername).IsUnique().HasDatabaseName("ux_platform_users_normalized_username");
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320).IsRequired();
            entity.HasIndex(e => e.NormalizedEmail).IsUnique().HasDatabaseName("ux_platform_users_normalized_email");
            entity.Property(e => e.NormalizedContactEmail)
                .HasColumnName("normalized_contact_email")
                .HasMaxLength(320);
            entity.HasIndex(e => e.NormalizedContactEmail)
                .HasDatabaseName("ix_platform_users_normalized_contact_email");
            entity.Property(e => e.HomeOrganizationId).HasColumnName("home_organization_id");
            entity.HasIndex(e => e.HomeOrganizationId)
                .HasDatabaseName("ix_platform_users_home_organization_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.SuspendedAtUtc).HasColumnName("suspended_at_utc");
            entity.Property(e => e.SuspensionReason).HasColumnName("suspension_reason").HasMaxLength(512);
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(32);
            entity.Property(e => e.EmployeeCode).HasColumnName("employee_code").HasMaxLength(64);
            entity.Property(e => e.StaffNumber).HasColumnName("staff_number").HasMaxLength(16);
            entity.HasIndex(e => e.StaffNumber)
                .IsUnique()
                .HasFilter("staff_number IS NOT NULL")
                .HasDatabaseName("ux_platform_users_staff_number");
            entity.Property(e => e.PublicUserId).HasColumnName("public_user_id").HasMaxLength(16);
            entity.HasIndex(e => e.PublicUserId)
                .IsUnique()
                .HasFilter("public_user_id IS NOT NULL")
                .HasDatabaseName("ux_platform_users_public_user_id");
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id");
            entity.Property(e => e.LinkedPersonalUserId).HasColumnName("linked_personal_user_id");
            entity.HasIndex(e => e.LinkedPersonalUserId)
                .HasDatabaseName("ix_platform_users_linked_personal_user_id");
            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.LinkedPersonalUserId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_platform_users_linked_personal_user");
            entity.ToTable(t => t.HasCheckConstraint(
                "ck_platform_users_linked_personal_staff_only",
                "linked_personal_user_id IS NULL OR (home_organization_id IS NOT NULL AND linked_personal_user_id <> id)"));
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<PlatformUserCredentialRecord>(entity =>
        {
            entity.ToTable("platform_user_credentials");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
            entity.Property(e => e.PasswordHashAlgorithm).HasColumnName("password_hash_algorithm").HasMaxLength(64).IsRequired();
            entity.Property(e => e.SecurityStamp).HasColumnName("security_stamp").HasMaxLength(64).IsRequired();
            entity.Property(e => e.PasswordChangedAtUtc).HasColumnName("password_changed_at_utc");
            entity.Property(e => e.EmailVerifiedAtUtc).HasColumnName("email_verified_at_utc");
            entity.Property(e => e.PendingRecoveryNormalizedEmail).HasColumnName("pending_recovery_normalized_email").HasMaxLength(320);
            entity.Property(e => e.RecoveryNormalizedEmail).HasColumnName("recovery_normalized_email").HasMaxLength(320);
            entity.Property(e => e.RecoveryEmailVerifiedAtUtc).HasColumnName("recovery_email_verified_at_utc");
            entity.Property(e => e.RecoveryEmailPromptSkippedAtUtc).HasColumnName("recovery_email_prompt_skipped_at_utc");
            entity.HasIndex(e => e.RecoveryNormalizedEmail)
                .IsUnique()
                .HasFilter("recovery_normalized_email IS NOT NULL");
            entity.Property(e => e.FailedAccessCount).HasColumnName("failed_access_count");
            entity.Property(e => e.LockoutEndUtc).HasColumnName("lockout_end_utc");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithOne()
                .HasForeignKey<PlatformUserCredentialRecord>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AccountProfileRecord>(entity =>
        {
            entity.ToTable("account_profiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserIdentityId).HasColumnName("user_identity_id");
            entity.Property(e => e.AccountClass).HasColumnName("account_class").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(e => new { e.UserIdentityId, e.AccountClass }).IsUnique();
            entity.HasIndex(e => e.UserIdentityId);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserIdentityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationContextPreferenceRecord>(entity =>
        {
            entity.ToTable("organization_context_preferences");
            entity.HasKey(e => e.UserIdentityId);
            entity.Property(e => e.UserIdentityId).HasColumnName("user_identity_id");
            entity.Property(e => e.LastActiveOrganizationId).HasColumnName("last_active_organization_id");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(e => e.LastActiveOrganizationId);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserIdentityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.LastActiveOrganizationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PlatformAuthSessionRecord>(entity =>
        {
            entity.ToTable("platform_auth_sessions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AccountProfileId).HasColumnName("account_profile_id");
            entity.Property(e => e.AccountClass).HasColumnName("account_class").HasMaxLength(32).IsRequired();
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AccountProfileId);
            entity.Property(e => e.SecurityStampAtIssue).HasColumnName("security_stamp_at_issue").HasMaxLength(64).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.AbsoluteExpiresAtUtc).HasColumnName("absolute_expires_at_utc");
            entity.Property(e => e.LastActivityAtUtc).HasColumnName("last_activity_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
            entity.Property(e => e.UserAgentHash).HasColumnName("user_agent_hash").HasMaxLength(128);
            entity.Property(e => e.SelectedOrganizationId).HasColumnName("selected_organization_id");
            entity.HasIndex(e => e.SelectedOrganizationId);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<AccountProfileRecord>()
                .WithMany()
                .HasForeignKey(e => e.AccountProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.SelectedOrganizationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PlatformAccessTokenRecord>(entity =>
        {
            entity.ToTable("platform_access_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.SecurityStampAtIssue).HasColumnName("security_stamp_at_issue").HasMaxLength(64).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.HasIndex(e => e.OrganizationId);
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PlatformDeviceRecoveryCredentialRecord>(entity =>
        {
            entity.ToTable("platform_device_recovery_credentials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.InstallationDeviceId)
                .HasColumnName("installation_device_id")
                .HasMaxLength(128)
                .IsRequired();
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.InstallationDeviceId, e.RevokedAtUtc });
            entity.Property(e => e.SecurityStampAtIssue)
                .HasColumnName("security_stamp_at_issue")
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.LastUsedAtUtc).HasColumnName("last_used_at_utc");
            entity.Property(e => e.IdleExpiresAtUtc).HasColumnName("idle_expires_at_utc");
            entity.Property(e => e.AbsoluteExpiresAtUtc).HasColumnName("absolute_expires_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RotationVersion).HasColumnName("rotation_version");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlatformCredentialTokenRecord>(entity =>
        {
            entity.ToTable("platform_credential_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Purpose).HasColumnName("purpose").HasMaxLength(32).IsRequired();
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Purpose });
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.ConsumedAtUtc).HasColumnName("consumed_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GovernanceStepUpGrantRecord>(entity =>
        {
            entity.ToTable("governance_step_up_grants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.ActionCode).HasColumnName("action_code").HasMaxLength(128).IsRequired();
            entity.Property(e => e.TargetType).HasColumnName("target_type").HasMaxLength(64).IsRequired();
            entity.Property(e => e.TargetId).HasColumnName("target_id");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.OrganizationId, e.ActionCode });
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.ConsumedAtUtc).HasColumnName("consumed_at_utc");
            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlatformExternalLoginRecord>(entity =>
        {
            entity.ToTable("platform_external_logins");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(32).IsRequired();
            entity.Property(e => e.ProviderSubject).HasColumnName("provider_subject").HasMaxLength(256).IsRequired();
            entity.Property(e => e.ProviderEmail).HasColumnName("provider_email").HasMaxLength(320);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(e => new { e.Provider, e.ProviderSubject }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationMembershipRecord>(entity =>
        {
            entity.ToTable("organization_memberships");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.BranchAccessScope)
                .HasColumnName("branch_access_scope")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.SuspendedAtUtc).HasColumnName("suspended_at_utc");
            entity.Property(e => e.RemovedAtUtc).HasColumnName("removed_at_utc");
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(512);
            entity.Property(e => e.ActorReference).HasColumnName("actor_reference").HasMaxLength(128);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.UserId, e.OrganizationId })
                .IsUnique()
                .HasFilter("status IN ('Active', 'Suspended')")
                .HasDatabaseName("ux_organization_memberships_current");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationMembershipBranchAssignmentRecord>(entity =>
        {
            entity.ToTable("organization_membership_branch_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.MembershipId).HasColumnName("membership_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ActorReference).HasColumnName("actor_reference").HasMaxLength(128);

            entity.HasIndex(e => new { e.MembershipId, e.BranchId })
                .IsUnique()
                .HasDatabaseName("ux_org_membership_branch_assignments_membership_branch");

            entity.HasIndex(e => e.OrganizationId)
                .HasDatabaseName("ix_org_membership_branch_assignments_organization_id");

            entity.HasOne<OrganizationMembershipRecord>()
                .WithMany()
                .HasForeignKey(e => e.MembershipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<OrganizationBranchRecord>()
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationInvitationRecord>(entity =>
        {
            entity.ToTable("organization_invitations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320).IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
            entity.Property(e => e.InvitedByUserId).HasColumnName("invited_by_user_id");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.AcceptedAtUtc).HasColumnName("accepted_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.DeclinedAtUtc).HasColumnName("declined_at_utc");
            entity.Property(e => e.AcceptedByUserId).HasColumnName("accepted_by_user_id");
            entity.Property(e => e.InviteeDisplayName).HasColumnName("invitee_display_name").HasMaxLength(256);
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(100);
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(100);
            entity.Property(e => e.Branch).HasColumnName("branch").HasMaxLength(128);
            entity.Property(e => e.ProductRole).HasColumnName("product_role").HasMaxLength(64);
            entity.Property(e => e.TargetPersonalUserId).HasColumnName("target_personal_user_id");
            entity.Property(e => e.TargetPublicUserId).HasColumnName("target_public_user_id").HasMaxLength(32);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("ux_organization_invitations_token_hash");
            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedEmail })
                .IsUnique()
                .HasFilter("status = 'Pending'")
                .HasDatabaseName("ux_organization_invitations_pending_email");
            entity.HasIndex(e => e.TargetPersonalUserId)
                .HasDatabaseName("ix_organization_invitations_target_personal_user_id");
            entity.HasIndex(e => new { e.OrganizationId, e.TargetPersonalUserId })
                .IsUnique()
                .HasFilter("status = 'Pending' AND target_personal_user_id IS NOT NULL")
                .HasDatabaseName("ux_organization_invitations_pending_target_user");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationOwnershipTransferRecord>(entity =>
        {
            entity.ToTable("organization_ownership_transfers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.FromOwnerUserId).HasColumnName("from_owner_user_id");
            entity.Property(e => e.ToUserId).HasColumnName("to_user_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.AcceptedAtUtc).HasColumnName("accepted_at_utc");
            entity.Property(e => e.DeclinedAtUtc).HasColumnName("declined_at_utc");
            entity.Property(e => e.CancelledAtUtc).HasColumnName("cancelled_at_utc");
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => e.OrganizationId)
                .IsUnique()
                .HasFilter("status = 'Pending'")
                .HasDatabaseName("ux_organization_ownership_transfers_pending_org");

            entity.HasIndex(e => new { e.ToUserId, e.Status })
                .HasDatabaseName("ix_organization_ownership_transfers_to_user_status");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.FromOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductAccessAssignmentRecord>(entity =>
        {
            entity.ToTable("product_access_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.MembershipId).HasColumnName("membership_id");
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.GrantedAtUtc).HasColumnName("granted_at_utc");
            entity.Property(e => e.GrantedByActor).HasColumnName("granted_by_actor").HasMaxLength(128).IsRequired();
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RevokedByActor).HasColumnName("revoked_by_actor").HasMaxLength(128);
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(512);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.UserId, e.OrganizationId, e.ProductCode })
                .IsUnique()
                .HasFilter("status = 'Active'")
                .HasDatabaseName("ux_product_access_assignments_active");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<OrganizationMembershipRecord>()
                .WithMany()
                .HasForeignKey(e => e.MembershipId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<ProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductCode)
                .HasPrincipalKey(p => p.Code)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlatformRoleAssignmentRecord>(entity =>
        {
            entity.ToTable("platform_role_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PlatformUserId).HasColumnName("platform_user_id");
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(64).IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.GrantedByActor).HasColumnName("granted_by_actor").HasMaxLength(128).IsRequired();
            entity.Property(e => e.GrantedAtUtc).HasColumnName("granted_at_utc");
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(512);
            entity.Property(e => e.RevokedByActor).HasColumnName("revoked_by_actor").HasMaxLength(128);
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(512);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => e.OrganizationId).HasDatabaseName("ix_platform_role_assignments_organization_id");

            // Two filtered unique indexes are required (rather than one) because Postgres treats
            // NULLs as distinct in a unique index: this enforces one active assignment per
            // (user, role) for platform-wide grants (organization_id IS NULL) and, separately, one
            // active assignment per (user, role, organization) for organization-scoped grants.
            entity.HasIndex(e => new { e.PlatformUserId, e.Role, e.OrganizationId })
                .IsUnique()
                .HasFilter("status = 'Active' AND organization_id IS NOT NULL")
                .HasDatabaseName("ux_platform_role_assignments_org_scoped_active");

            entity.HasIndex(e => new { e.PlatformUserId, e.Role })
                .IsUnique()
                .HasFilter("status = 'Active' AND organization_id IS NULL")
                .HasDatabaseName("ux_platform_role_assignments_platform_wide_active");

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.PlatformUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlatformRoleDefinitionRecord>(entity =>
        {
            entity.ToTable("platform_role_definitions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(512);
            entity.Property(e => e.Kind).HasColumnName("kind").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PermissionsJson).HasColumnName("permissions_json").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_platform_role_definitions_code");
        });

        modelBuilder.Entity<PlatformCustomRoleAssignmentRecord>(entity =>
        {
            entity.ToTable("platform_custom_role_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PlatformUserId).HasColumnName("platform_user_id");
            entity.Property(e => e.RoleDefinitionId).HasColumnName("role_definition_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.GrantedByActor).HasColumnName("granted_by_actor").HasMaxLength(128).IsRequired();
            entity.Property(e => e.GrantedAtUtc).HasColumnName("granted_at_utc");
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(512);
            entity.Property(e => e.RevokedByActor).HasColumnName("revoked_by_actor").HasMaxLength(128);
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(512);
            entity.HasIndex(e => new { e.PlatformUserId, e.RoleDefinitionId })
                .IsUnique()
                .HasFilter("status = 'Active'")
                .HasDatabaseName("ux_platform_custom_role_assignments_active");
            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.PlatformUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformRoleDefinitionRecord>()
                .WithMany()
                .HasForeignKey(e => e.RoleDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationRoleDefinitionRecord>(entity =>
        {
            entity.ToTable("organization_role_definitions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(512);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PermissionsJson).HasColumnName("permissions_json").HasColumnType("jsonb").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
            entity.HasIndex(e => new { e.OrganizationId, e.Code })
                .IsUnique()
                .HasDatabaseName("ux_organization_role_definitions_org_code");
            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrganizationCustomRoleAssignmentRecord>(entity =>
        {
            entity.ToTable("organization_custom_role_assignments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.PlatformUserId).HasColumnName("platform_user_id");
            entity.Property(e => e.RoleDefinitionId).HasColumnName("role_definition_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.GrantedByActor).HasColumnName("granted_by_actor").HasMaxLength(128).IsRequired();
            entity.Property(e => e.GrantedAtUtc).HasColumnName("granted_at_utc");
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(512);
            entity.Property(e => e.RevokedByActor).HasColumnName("revoked_by_actor").HasMaxLength(128);
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RevokeReason).HasColumnName("revoke_reason").HasMaxLength(512);
            entity.HasIndex(e => new { e.OrganizationId, e.PlatformUserId, e.RoleDefinitionId })
                .IsUnique()
                .HasFilter("status = 'Active'")
                .HasDatabaseName("ux_organization_custom_role_assignments_active");
            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.PlatformUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<OrganizationRoleDefinitionRecord>()
                .WithMany()
                .HasForeignKey(e => e.RoleDefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AuditRecordRecord>(entity =>
        {
            // Append-only log: no unique constraints, no FKs to mutable aggregates, and the
            // application layer exposes no update/delete method (see AuditRecord.Rehydrate/Create).
            entity.ToTable("audit_records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(e => e.ActorIdentifier).HasColumnName("actor_identifier").HasMaxLength(256).IsRequired();
            entity.Property(e => e.ActorType).HasColumnName("actor_type").HasMaxLength(32).IsRequired();
            entity.Property(e => e.ActionCode).HasColumnName("action_code").HasMaxLength(128).IsRequired();
            entity.Property(e => e.TargetType).HasColumnName("target_type").HasMaxLength(64).IsRequired();
            entity.Property(e => e.TargetId).HasColumnName("target_id").HasMaxLength(128).IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64);
            entity.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(128);
            entity.Property(e => e.Outcome).HasColumnName("outcome").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(512);
            entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(2000);

            entity.HasIndex(e => e.OccurredAtUtc).HasDatabaseName("ix_audit_records_occurred_at_utc");
            entity.HasIndex(e => e.ActorIdentifier).HasDatabaseName("ix_audit_records_actor_identifier");
            entity.HasIndex(e => e.ActionCode).HasDatabaseName("ix_audit_records_action_code");
            entity.HasIndex(e => e.OrganizationId).HasDatabaseName("ix_audit_records_organization_id");
            entity.HasIndex(e => e.Outcome).HasDatabaseName("ix_audit_records_outcome");
        });

        modelBuilder.Entity<PersonalAccountSettingsRecord>(entity =>
        {
            entity.ToTable("personal_account_settings");
            entity.HasKey(e => e.UserIdentityId);
            entity.Property(e => e.UserIdentityId).HasColumnName("user_identity_id");
            entity.Property(e => e.EmailNotificationsEnabled).HasColumnName("email_notifications_enabled");
            entity.Property(e => e.PushNotificationsEnabled).HasColumnName("push_notifications_enabled");
            entity.Property(e => e.InAppNotificationsEnabled).HasColumnName("in_app_notifications_enabled");
            entity.Property(e => e.ReminderNotificationsEnabled).HasColumnName("reminder_notifications_enabled");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserIdentityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlatformSettingsRecord>(entity =>
        {
            entity.ToTable("platform_settings", tb => tb.HasCheckConstraint("ck_platform_settings_singleton", "id = 1"));
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PlatformDisplayName).HasColumnName("platform_display_name").HasMaxLength(200);
            entity.Property(e => e.SupportEmail).HasColumnName("support_email").HasMaxLength(320);
            entity.Property(e => e.BrandingLogoUrl).HasColumnName("branding_logo_url").HasMaxLength(2048);
            entity.Property(e => e.BrandingPrimaryColor).HasColumnName("branding_primary_color").HasMaxLength(7);
            entity.Property(e => e.BrandingAccentColor).HasColumnName("branding_accent_color").HasMaxLength(7);
            entity.Property(e => e.EmailProviderMode).HasColumnName("email_provider_mode").HasMaxLength(32).IsRequired();
            entity.Property(e => e.SmtpHost).HasColumnName("smtp_host").HasMaxLength(255);
            entity.Property(e => e.SmtpPort).HasColumnName("smtp_port");
            entity.Property(e => e.SmtpUsername).HasColumnName("smtp_username").HasMaxLength(255);
            entity.Property(e => e.ProtectedSmtpPassword).HasColumnName("protected_smtp_password").HasMaxLength(4096);
            entity.Property(e => e.SmtpPasswordConfigured).HasColumnName("smtp_password_configured");
            entity.Property(e => e.FromDisplayName).HasColumnName("from_display_name").HasMaxLength(200);
            entity.Property(e => e.FromAddress).HasColumnName("from_address").HasMaxLength(320);
            entity.Property(e => e.SmtpSecurityMode).HasColumnName("smtp_security_mode").HasMaxLength(32).IsRequired();
            entity.Property(e => e.AdminPublicBaseUrl).HasColumnName("admin_public_base_url").HasMaxLength(2048);
            entity.Property(e => e.DefaultTimeZoneId).HasColumnName("default_time_zone_id").HasMaxLength(128);
            entity.Property(e => e.DefaultLocale).HasColumnName("default_locale").HasMaxLength(32);
            entity.Property(e => e.DefaultCurrencyCode).HasColumnName("default_currency_code").HasMaxLength(3);
            entity.Property(e => e.DefaultCountryCode).HasColumnName("default_country_code").HasMaxLength(2);
            entity.Property(e => e.DateFormat).HasColumnName("date_format").HasMaxLength(64);
            entity.Property(e => e.TimeFormat).HasColumnName("time_format").HasMaxLength(64);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.UpdatedByActorId).HasColumnName("updated_by_actor_id").HasMaxLength(320);
            entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<PersonalContactRecord>(entity =>
        {
            entity.ToTable("personal_contacts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OwnerUserIdentityId).HasColumnName("owner_user_identity_id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(100).IsRequired();
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(32);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(320);
            entity.Property(e => e.LinkedUserIdentityId).HasColumnName("linked_user_identity_id");
            entity.Property(e => e.ResolvedUserIdentityId).HasColumnName("resolved_user_identity_id");
            entity.Property(e => e.ResolvedPublicUserId).HasColumnName("resolved_public_user_id").HasMaxLength(32);
            entity.Property(e => e.ConnectedAtUtc).HasColumnName("connected_at_utc");
            entity.Property(e => e.BlockedAtUtc).HasColumnName("blocked_at_utc");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(e => e.OwnerUserIdentityId);
            entity.HasIndex(e => new { e.OwnerUserIdentityId, e.Email })
                .IsUnique()
                .HasFilter("email IS NOT NULL AND status = 'Active'")
                .HasDatabaseName("ux_personal_contacts_owner_active_email");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserIdentityId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.LinkedUserIdentityId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.ResolvedUserIdentityId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PersonalConnectionRequestRecord>(entity =>
        {
            entity.ToTable("personal_connection_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RequesterUserIdentityId).HasColumnName("requester_user_identity_id");
            entity.Property(e => e.TargetUserIdentityId).HasColumnName("target_user_identity_id");
            entity.Property(e => e.RequesterContactId).HasColumnName("requester_contact_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.AcceptedAtUtc).HasColumnName("accepted_at_utc");
            entity.Property(e => e.DeclinedAtUtc).HasColumnName("declined_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RespondedByUserIdentityId).HasColumnName("responded_by_user_identity_id");
            entity.HasIndex(e => new { e.RequesterUserIdentityId, e.TargetUserIdentityId, e.Status })
                .HasDatabaseName("ix_personal_connection_requests_requester_target_status");
            // Pending unordered pair uniqueness enforced by ux_personal_connection_requests_pending_user_pair (see migration).
            entity.HasIndex(e => e.RequesterContactId);

            entity.HasOne<PersonalContactRecord>()
                .WithMany()
                .HasForeignKey(e => e.RequesterContactId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.RequesterUserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.TargetUserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PersonalDebtRelationshipRecord>(entity =>
        {
            entity.ToTable("personal_debt_relationships", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_personal_debt_relationships_creditor_side",
                    "(creditor_user_identity_id IS NOT NULL AND creditor_contact_id IS NULL) OR (creditor_user_identity_id IS NULL AND creditor_contact_id IS NOT NULL)");
                tb.HasCheckConstraint(
                    "ck_personal_debt_relationships_debtor_side",
                    "(debtor_user_identity_id IS NOT NULL AND debtor_contact_id IS NULL) OR (debtor_user_identity_id IS NULL AND debtor_contact_id IS NOT NULL)");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreditorUserIdentityId).HasColumnName("creditor_user_identity_id");
            entity.Property(e => e.CreditorContactId).HasColumnName("creditor_contact_id");
            entity.Property(e => e.DebtorUserIdentityId).HasColumnName("debtor_user_identity_id");
            entity.Property(e => e.DebtorContactId).HasColumnName("debtor_contact_id");
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsFixedLength().IsRequired();
            entity.Property(e => e.CurrentBalance).HasColumnName("current_balance").HasColumnType("decimal(18,4)");
            entity.Property(e => e.DueDateUtc).HasColumnName("due_date_utc");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.DestinationOrganizationId).HasColumnName("destination_organization_id");
            entity.Property(e => e.DestinationCreditCustomerId).HasColumnName("destination_credit_customer_id");
            entity.Property(e => e.MigrationBatchId).HasColumnName("migration_batch_id");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.AggregateVersion).HasColumnName("aggregate_version").IsConcurrencyToken();
            entity.HasIndex(e => e.CreditorUserIdentityId);
            entity.HasIndex(e => e.DebtorUserIdentityId);
            entity.HasIndex(e => e.CreditorContactId);
            entity.HasIndex(e => e.DebtorContactId);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.CreditorUserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.DebtorUserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PersonalContactRecord>()
                .WithMany()
                .HasForeignKey(e => e.CreditorContactId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PersonalContactRecord>()
                .WithMany()
                .HasForeignKey(e => e.DebtorContactId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PersonalUtangEntryRecord>(entity =>
        {
            entity.ToTable("personal_utang_entries", tb =>
            {
                tb.HasCheckConstraint("ck_personal_utang_entries_positive_amount", "amount > 0");
            });
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RelationshipId).HasColumnName("relationship_id");
            entity.Property(e => e.EntryType).HasColumnName("entry_type").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,4)");
            entity.Property(e => e.SignedDelta).HasColumnName("signed_delta").HasColumnType("decimal(18,4)");
            entity.Property(e => e.BalanceAfter).HasColumnName("balance_after").HasColumnType("decimal(18,4)");
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(512);
            entity.Property(e => e.DueDateUtc).HasColumnName("due_date_utc");
            entity.Property(e => e.CreatedByUserIdentityId).HasColumnName("created_by_user_identity_id");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.ResolvedByUserIdentityId).HasColumnName("resolved_by_user_identity_id");
            entity.Property(e => e.ResolvedAtUtc).HasColumnName("resolved_at_utc");
            entity.Property(e => e.DisputeReason).HasColumnName("dispute_reason").HasMaxLength(256);
            entity.Property(e => e.Intent)
                .HasColumnName("intent")
                .HasMaxLength(32)
                .IsRequired()
                .HasDefaultValue("Regular");
            entity.Property(e => e.SettlementBalanceSnapshot)
                .HasColumnName("settlement_balance_snapshot")
                .HasColumnType("decimal(18,4)");
            entity.HasIndex(e => e.RelationshipId);
            entity.HasIndex(e => new { e.RelationshipId, e.Status });

            entity.HasOne<PersonalDebtRelationshipRecord>()
                .WithMany()
                .HasForeignKey(e => e.RelationshipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.ResolvedByUserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PersonalUtangInvitationRecord>(entity =>
        {
            entity.ToTable("personal_utang_invitations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DebtRelationshipId).HasColumnName("debt_relationship_id");
            entity.Property(e => e.InviteeContactId).HasColumnName("invitee_contact_id");
            entity.Property(e => e.InvitedByUserIdentityId).HasColumnName("invited_by_user_identity_id");
            entity.Property(e => e.InviteTargetNormalizedEmail)
                .HasColumnName("invite_target_normalized_email")
                .HasMaxLength(320);
            entity.Property(e => e.InviteTargetPhone).HasColumnName("invite_target_phone").HasMaxLength(32);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.AcceptedAtUtc).HasColumnName("accepted_at_utc");
            entity.Property(e => e.DeclinedAtUtc).HasColumnName("declined_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.AcceptedByUserIdentityId).HasColumnName("accepted_by_user_identity_id");
            entity.HasIndex(e => e.TokenHash).IsUnique();
            entity.HasIndex(e => e.InvitedByUserIdentityId);
            entity.HasIndex(e => e.InviteTargetNormalizedEmail);
            entity.HasIndex(e => new { e.DebtRelationshipId, e.InviteeContactId, e.Status })
                .HasDatabaseName("ix_personal_utang_invitations_relationship_contact_status");

            entity.HasOne<PersonalDebtRelationshipRecord>()
                .WithMany()
                .HasForeignKey(e => e.DebtRelationshipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PersonalContactRecord>()
                .WithMany()
                .HasForeignKey(e => e.InviteeContactId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.InvitedByUserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.AcceptedByUserIdentityId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PersonalReminderRecord>(entity =>
        {
            entity.ToTable("personal_reminders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DebtRelationshipId).HasColumnName("debt_relationship_id");
            entity.Property(e => e.CreatedByUserIdentityId).HasColumnName("created_by_user_identity_id");
            entity.Property(e => e.ScheduleType).HasColumnName("schedule_type").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Message).HasColumnName("message").HasMaxLength(280);
            entity.Property(e => e.ScheduledForUtc).HasColumnName("scheduled_for_utc");
            entity.Property(e => e.NextDeliveryAtUtc).HasColumnName("next_delivery_at_utc");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.DeliveredAtUtc).HasColumnName("delivered_at_utc");
            entity.Property(e => e.DeliveryAttemptCount).HasColumnName("delivery_attempt_count");
            entity.HasIndex(e => e.DebtRelationshipId);
            entity.HasIndex(e => e.NextDeliveryAtUtc);

            entity.HasOne<PersonalDebtRelationshipRecord>()
                .WithMany()
                .HasForeignKey(e => e.DebtRelationshipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PersonalInAppNotificationRecord>(entity =>
        {
            entity.ToTable("personal_in_app_notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RecipientUserIdentityId).HasColumnName("recipient_user_identity_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(120).IsRequired();
            entity.Property(e => e.Preview).HasColumnName("preview").HasMaxLength(200).IsRequired();
            entity.Property(e => e.RelatedType).HasColumnName("related_type").HasMaxLength(64).IsRequired();
            entity.Property(e => e.RelatedId).HasColumnName("related_id").HasMaxLength(64);
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ReadAtUtc).HasColumnName("read_at_utc");
            entity.HasIndex(e => e.RecipientUserIdentityId);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.RecipientUserIdentityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalNotificationDeliveryRecord>(entity =>
        {
            entity.ToTable("personal_notification_deliveries");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReminderId).HasColumnName("reminder_id");
            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.RecipientUserIdentityId).HasColumnName("recipient_user_identity_id");
            entity.Property(e => e.Channel).HasColumnName("channel").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PreviewText).HasColumnName("preview_text").HasMaxLength(200).IsRequired();
            entity.Property(e => e.AttemptedAtUtc).HasColumnName("attempted_at_utc");
            entity.Property(e => e.DeliveredAtUtc).HasColumnName("delivered_at_utc");
            entity.Property(e => e.FailureReason).HasColumnName("failure_reason").HasMaxLength(256);
            entity.HasIndex(e => e.ReminderId);
            entity.HasIndex(e => e.RecipientUserIdentityId);

            entity.HasOne<PersonalReminderRecord>()
                .WithMany()
                .HasForeignKey(e => e.ReminderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<PersonalInAppNotificationRecord>()
                .WithMany()
                .HasForeignKey(e => e.NotificationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.RecipientUserIdentityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BusinessCustomerRecord>(entity =>
        {
            entity.ToTable("business_customers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(128).IsRequired();
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(32);
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(512);
            entity.Property(e => e.OwningProductCode).HasColumnName("owning_product_code").HasMaxLength(64);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.LinkedUserIdentityId).HasColumnName("linked_user_identity_id");
            entity.Property(e => e.AllowDeliveryBeyondNormalDistance)
                .HasColumnName("allow_delivery_beyond_normal_distance")
                .IsRequired()
                .HasDefaultValue(false);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
            entity.HasIndex(e => e.OrganizationId).HasDatabaseName("ix_business_customers_organization_id");
            entity.HasIndex(e => new { e.OrganizationId, e.OwningProductCode })
                .HasDatabaseName("ix_business_customers_org_product");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CreditCustomerRecord>(entity =>
        {
            entity.ToTable("credit_customers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessCustomerId).HasColumnName("business_customer_id");
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
            entity.HasIndex(e => e.OrganizationId).HasDatabaseName("ix_credit_customers_organization_id");
            entity.HasIndex(e => e.BusinessCustomerId)
                .IsUnique()
                .HasFilter("status = 'Active'")
                .HasDatabaseName("ux_credit_customers_active_business_customer");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BusinessCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.BusinessCustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CustomerLinkRequestRecord>(entity =>
        {
            entity.ToTable("customer_link_requests");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessCustomerId).HasColumnName("business_customer_id");
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320).IsRequired();
            entity.Property(e => e.TargetUserIdentityId).HasColumnName("target_user_identity_id");
            entity.Property(e => e.TargetPublicUserId).HasColumnName("target_public_user_id").HasMaxLength(32);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
            entity.Property(e => e.InvitedByUserId).HasColumnName("invited_by_user_id");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.AcceptedAtUtc).HasColumnName("accepted_at_utc");
            entity.Property(e => e.DeclinedAtUtc).HasColumnName("declined_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.AcceptedByUserId).HasColumnName("accepted_by_user_id");
            entity.Property(e => e.ReminderCount).HasColumnName("reminder_count").HasDefaultValue(0);
            entity.Property(e => e.LastRemindedAtUtc).HasColumnName("last_reminded_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
            entity.HasIndex(e => e.TokenHash).IsUnique().HasDatabaseName("ux_customer_link_requests_token_hash");
            entity.HasIndex(e => e.BusinessCustomerId)
                .IsUnique()
                .HasFilter("status = 'Pending'")
                .HasDatabaseName("ux_customer_link_requests_pending_customer");
            entity.HasIndex(e => e.TargetUserIdentityId)
                .HasDatabaseName("ix_customer_link_requests_target_user_identity_id");
            entity.HasIndex(e => new { e.OrganizationId, e.TargetUserIdentityId })
                .IsUnique()
                .HasFilter("status = 'Pending' AND target_user_identity_id IS NOT NULL")
                .HasDatabaseName("ux_customer_link_requests_pending_org_target");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BusinessCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.BusinessCustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PersonalOrganizationConnectionBlockRecord>(entity =>
        {
            entity.ToTable("personal_organization_connection_blocks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PersonalUserIdentityId).HasColumnName("personal_user_identity_id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.BlockedAtUtc).HasColumnName("blocked_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.UnblockedAtUtc).HasColumnName("unblocked_at_utc");
            entity.Property(e => e.SourceCustomerLinkRequestId).HasColumnName("source_customer_link_request_id");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
            entity.HasIndex(e => new { e.PersonalUserIdentityId, e.OrganizationId })
                .IsUnique()
                .HasDatabaseName("ux_personal_org_connection_blocks_pair");
            entity.HasIndex(e => e.PersonalUserIdentityId)
                .HasFilter("status = 'Active'")
                .HasDatabaseName("ix_personal_org_connection_blocks_personal_active");

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.PersonalUserIdentityId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrganizationInAppNotificationRecord>(entity =>
        {
            entity.ToTable("organization_in_app_notifications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.RecipientUserIdentityId).HasColumnName("recipient_user_identity_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(120).IsRequired();
            entity.Property(e => e.Preview).HasColumnName("preview").HasMaxLength(200).IsRequired();
            entity.Property(e => e.RelatedType).HasColumnName("related_type").HasMaxLength(64).IsRequired();
            entity.Property(e => e.RelatedId).HasColumnName("related_id").HasMaxLength(64);
            entity.Property(e => e.IsRead).HasColumnName("is_read");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ReadAtUtc).HasColumnName("read_at_utc");
            entity.HasIndex(e => new { e.OrganizationId, e.RecipientUserIdentityId })
                .HasDatabaseName("ix_organization_in_app_notifications_org_recipient");
            entity.HasIndex(e => new { e.RecipientUserIdentityId, e.RelatedType, e.RelatedId })
                .HasDatabaseName("ix_organization_in_app_notifications_recipient_related");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.RecipientUserIdentityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LinkedCustomerAppUserRecord>(entity =>
        {
            entity.ToTable("linked_customer_app_users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessCustomerId).HasColumnName("business_customer_id");
            entity.Property(e => e.UserIdentityId).HasColumnName("user_identity_id");
            entity.Property(e => e.SourceLinkRequestId).HasColumnName("source_link_request_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.LinkedAtUtc).HasColumnName("linked_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();
            entity.HasIndex(e => e.OrganizationId).HasDatabaseName("ix_linked_customer_app_users_organization_id");
            entity.HasIndex(e => e.BusinessCustomerId)
                .IsUnique()
                .HasFilter("status = 'Active'")
                .HasDatabaseName("ux_linked_customer_app_users_active_customer");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BusinessCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.BusinessCustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CustomerLinkRequestRecord>()
                .WithMany()
                .HasForeignKey(e => e.SourceLinkRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BusinessCreditOpeningBalanceRecord>(entity =>
        {
            entity.ToTable("business_credit_opening_balances");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.CreditCustomerId).HasColumnName("credit_customer_id");
            entity.Property(e => e.BusinessCustomerId).HasColumnName("business_customer_id");
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(18,4)");
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
            entity.Property(e => e.EffectiveDateUtc).HasColumnName("effective_date_utc");
            entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(64).IsRequired();
            entity.Property(e => e.SourceRecordId).HasColumnName("source_record_id");
            entity.Property(e => e.MigrationBatchId).HasColumnName("migration_batch_id");
            entity.Property(e => e.ImportedByUserId).HasColumnName("imported_by_user_id");
            entity.Property(e => e.ImportedAtUtc).HasColumnName("imported_at_utc");
            entity.Property(e => e.DestinationProduct).HasColumnName("destination_product").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.OrganizationId).HasDatabaseName("ix_business_credit_opening_balances_org");
            entity.HasIndex(e => new { e.OrganizationId, e.SourceType, e.SourceRecordId })
                .IsUnique()
                .HasDatabaseName("ux_business_credit_opening_balances_org_source");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CreditCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.CreditCustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BusinessCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.BusinessCustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductLocalRoleGrantRecord>(entity =>
        {
            entity.ToTable("product_local_role_grants");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.UserIdentityId).HasColumnName("user_identity_id");
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.RoleCode).HasColumnName("role_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.GrantedAtUtc).HasColumnName("granted_at_utc");
            entity.Property(e => e.GrantedByUserIdentityId).HasColumnName("granted_by_user_identity_id");
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(64).IsRequired();
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RevokedByUserIdentityId).HasColumnName("revoked_by_user_identity_id");
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(512);
            entity.HasIndex(e => new { e.OrganizationId, e.UserIdentityId, e.ProductCode })
                .IsUnique()
                .HasDatabaseName("ux_product_local_role_grants_active_org_user_product")
                .HasFilter("status = 'Active'");

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.UserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PersonalUtangMigrationBatchRecord>(entity =>
        {
            entity.ToTable("personal_utang_migration_batches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OwnerUserIdentityId).HasColumnName("owner_user_identity_id");
            entity.Property(e => e.DestinationOrganizationId).HasColumnName("destination_organization_id");
            entity.Property(e => e.DestinationProductCode).HasColumnName("destination_product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.EffectiveMigrationDateUtc).HasColumnName("effective_migration_date_utc");
            entity.Property(e => e.IncludeContact).HasColumnName("include_contact");
            entity.Property(e => e.IncludeOpeningBalance).HasColumnName("include_opening_balance");
            entity.Property(e => e.IncludeSelectedHistory).HasColumnName("include_selected_history");
            entity.Property(e => e.IncludeDueDatesAndNotes).HasColumnName("include_due_dates_and_notes");
            entity.Property(e => e.SourceDisposition).HasColumnName("source_disposition").HasMaxLength(32).IsRequired();
            entity.Property(e => e.LinkedParticipantConsentAcknowledged).HasColumnName("linked_participant_consent_acknowledged");
            entity.Property(e => e.ConfirmationToken).HasColumnName("confirmation_token");
            entity.Property(e => e.PreviewedAtUtc).HasColumnName("previewed_at_utc");
            entity.Property(e => e.ExecutedAtUtc).HasColumnName("executed_at_utc");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(e => new { e.OwnerUserIdentityId, e.IdempotencyKey })
                .IsUnique()
                .HasFilter("idempotency_key IS NOT NULL")
                .HasDatabaseName("ux_personal_utang_migration_batches_owner_idempotency");
            entity.HasIndex(e => e.DestinationOrganizationId)
                .HasDatabaseName("ix_personal_utang_migration_batches_destination_org");

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserIdentityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.DestinationOrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PersonalUtangMigrationItemRecord>(entity =>
        {
            entity.ToTable("personal_utang_migration_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatchId).HasColumnName("batch_id");
            entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(64).IsRequired();
            entity.Property(e => e.SourceRecordId).HasColumnName("source_record_id");
            entity.Property(e => e.DestinationType).HasColumnName("destination_type").HasMaxLength(64);
            entity.Property(e => e.DestinationRecordId).HasColumnName("destination_record_id");
            entity.Property(e => e.OpeningBalanceAmount).HasColumnName("opening_balance_amount").HasColumnType("decimal(18,4)");
            entity.Property(e => e.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3);
            entity.Property(e => e.NotesSnapshot).HasColumnName("notes_snapshot").HasMaxLength(512);
            entity.Property(e => e.DueDateUtc).HasColumnName("due_date_utc");
            entity.Property(e => e.HistoryEntryIdsCsv).HasColumnName("history_entry_ids_csv").HasMaxLength(4000);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.BlockedReason).HasColumnName("blocked_reason").HasMaxLength(512);
            entity.HasIndex(e => e.BatchId).HasDatabaseName("ix_personal_utang_migration_items_batch_id");
            entity.HasIndex(e => new { e.SourceType, e.SourceRecordId, e.Status })
                .HasDatabaseName("ix_personal_utang_migration_items_source_status");

            entity.HasOne<PersonalUtangMigrationBatchRecord>()
                .WithMany()
                .HasForeignKey(e => e.BatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalFeatureDefinitionRecord>(entity =>
        {
            entity.ToTable("personal_feature_definitions");
            entity.HasKey(e => e.FeatureCode);
            entity.Property(e => e.FeatureCode).HasColumnName("feature_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.RewardPointsPrice).HasColumnName("reward_points_price");
            entity.Property(e => e.DefaultEntitlementDurationDays).HasColumnName("default_entitlement_duration_days");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.ToTable(t => t.HasCheckConstraint(
                "ck_personal_feature_definitions_duration_days",
                "default_entitlement_duration_days IS NULL OR (default_entitlement_duration_days >= 1 AND default_entitlement_duration_days <= 3650)"));
            entity.HasIndex(e => e.IsActive).HasDatabaseName("ix_personal_feature_definitions_is_active");
        });

        modelBuilder.Entity<PersonalFeatureEntitlementRecord>(entity =>
        {
            entity.ToTable("personal_feature_entitlements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PersonalUserId).HasColumnName("personal_user_id");
            entity.Property(e => e.FeatureCode).HasColumnName("feature_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.StartsAtUtc).HasColumnName("starts_at_utc");
            entity.Property(e => e.EndsAtUtc).HasColumnName("ends_at_utc");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.GrantSource).HasColumnName("grant_source").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RevocationReason).HasColumnName("revocation_reason").HasMaxLength(512);

            entity.HasIndex(e => new { e.PersonalUserId, e.FeatureCode, e.Status })
                .HasDatabaseName("ix_personal_feature_entitlements_user_feature_status");
            entity.HasIndex(e => new { e.PersonalUserId, e.FeatureCode, e.StartsAtUtc, e.EndsAtUtc })
                .HasDatabaseName("ix_personal_feature_entitlements_user_feature_window");

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.PersonalUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalRewardBalanceRecord>(entity =>
        {
            entity.ToTable("personal_reward_balances");
            entity.HasKey(e => e.PersonalUserId);
            entity.Property(e => e.PersonalUserId).HasColumnName("personal_user_id");
            entity.Property(e => e.AvailablePoints).HasColumnName("available_points");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.PersonalUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalRewardTransactionRecord>(entity =>
        {
            entity.ToTable("personal_reward_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PersonalUserId).HasColumnName("personal_user_id");
            entity.Property(e => e.TransactionType).HasColumnName("transaction_type").HasMaxLength(16).IsRequired();
            entity.Property(e => e.Points).HasColumnName("points");
            entity.Property(e => e.SignedDelta).HasColumnName("signed_delta");
            entity.Property(e => e.BalanceAfter).HasColumnName("balance_after");
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(512);
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id").HasMaxLength(128);
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");

            entity.HasIndex(e => new { e.PersonalUserId, e.CreatedAtUtc, e.Id })
                .HasDatabaseName("ix_personal_reward_transactions_user_created");
            entity.HasIndex(e => new { e.PersonalUserId, e.IdempotencyKey })
                .IsUnique()
                .HasFilter("idempotency_key IS NOT NULL")
                .HasDatabaseName("ux_personal_reward_transactions_user_idempotency");

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.PersonalUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalRewardClaimRecord>(entity =>
        {
            entity.ToTable("personal_reward_claims");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PersonalUserId).HasColumnName("personal_user_id");
            entity.Property(e => e.ClaimType).HasColumnName("claim_type").HasMaxLength(32).IsRequired();
            entity.Property(e => e.ClaimKey).HasColumnName("claim_key").HasMaxLength(128).IsRequired();
            entity.Property(e => e.PointsAwarded).HasColumnName("points_awarded");
            entity.Property(e => e.RewardTransactionId).HasColumnName("reward_transaction_id");
            entity.Property(e => e.ClaimedAtUtc).HasColumnName("claimed_at_utc");

            entity.HasIndex(e => new { e.PersonalUserId, e.ClaimType, e.ClaimKey })
                .IsUnique()
                .HasDatabaseName("ux_personal_reward_claims_user_type_key");
            entity.HasIndex(e => e.RewardTransactionId)
                .IsUnique()
                .HasDatabaseName("ux_personal_reward_claims_transaction");

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.PersonalUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalTodoRecord>(entity =>
        {
            entity.ToTable("personal_todos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OwnerUserIdentityId).HasColumnName("owner_user_identity_id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(2000);
            entity.Property(e => e.DueAtUtc).HasColumnName("due_at_utc");
            entity.Property(e => e.ReminderAtUtc).HasColumnName("reminder_at_utc");
            entity.Property(e => e.ReminderNotifiedAtUtc).HasColumnName("reminder_notified_at_utc");
            entity.Property(e => e.Priority).HasColumnName("priority").HasMaxLength(16).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(16).IsRequired();
            entity.Property(e => e.RelatedEntityType).HasColumnName("related_entity_type").HasMaxLength(64);
            entity.Property(e => e.RelatedEntityId).HasColumnName("related_entity_id");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => e.OwnerUserIdentityId)
                .HasDatabaseName("ix_personal_todos_owner_user_identity_id");
            entity.HasIndex(e => new { e.OwnerUserIdentityId, e.Status })
                .HasDatabaseName("ix_personal_todos_owner_status");
            entity.HasIndex(e => new { e.OwnerUserIdentityId, e.DueAtUtc })
                .HasDatabaseName("ix_personal_todos_owner_due");
            entity.HasIndex(e => new { e.Status, e.ReminderAtUtc })
                .HasDatabaseName("ix_personal_todos_status_reminder")
                .HasFilter("reminder_at_utc IS NOT NULL AND reminder_notified_at_utc IS NULL");

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.OwnerUserIdentityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BusinessTypeRecord>(entity =>
        {
            entity.ToTable("business_types", CatalogSchemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.NormalizedName).HasColumnName("normalized_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.IconReference).HasColumnName("icon_reference").HasMaxLength(512);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => e.Code).IsUnique().HasDatabaseName("ux_business_types_code");
            entity.HasIndex(e => e.NormalizedName).IsUnique().HasDatabaseName("ux_business_types_normalized_name");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_business_types_status");
            entity.HasIndex(e => e.SortOrder).HasDatabaseName("ix_business_types_sort_order");
        });

        modelBuilder.Entity<GlobalCategoryRecord>(entity =>
        {
            entity.ToTable("global_categories", CatalogSchemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.NormalizedName).HasColumnName("normalized_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.IconReference).HasColumnName("icon_reference").HasMaxLength(512);
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => e.NormalizedName)
                .IsUnique()
                .HasFilter("parent_id IS NULL")
                .HasDatabaseName("ux_global_categories_normalized_name_root");
            entity.HasIndex(e => new { e.NormalizedName, e.ParentId })
                .IsUnique()
                .HasFilter("parent_id IS NOT NULL")
                .HasDatabaseName("ux_global_categories_normalized_name_parent");
            entity.HasIndex(e => e.ParentId).HasDatabaseName("ix_global_categories_parent_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_global_categories_status");

            entity.HasOne<GlobalCategoryRecord>()
                .WithMany()
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.BusinessTypes)
                .WithOne()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GlobalCategoryBusinessTypeRecord>(entity =>
        {
            entity.ToTable("global_category_business_types", CatalogSchemaName);
            entity.HasKey(e => new { e.CategoryId, e.BusinessTypeId });
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.BusinessTypeId).HasColumnName("business_type_id");
            entity.HasIndex(e => e.BusinessTypeId).HasDatabaseName("ix_global_category_business_types_type");

            entity.HasOne<BusinessTypeRecord>()
                .WithMany()
                .HasForeignKey(e => e.BusinessTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GlobalProductRecord>(entity =>
        {
            entity.ToTable("global_products", CatalogSchemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(64);
            entity.Property(e => e.Barcode).HasColumnName("barcode").HasMaxLength(64);
            entity.Property(e => e.Brand).HasColumnName("brand").HasMaxLength(120);
            entity.Property(e => e.GlobalCategoryId).HasColumnName("global_category_id");
            entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(32).IsRequired();
            entity.Property(e => e.SellingMode).HasColumnName("selling_mode").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CostPrice).HasColumnName("cost_price").HasColumnType("decimal(18,2)");
            entity.Property(e => e.SellingPrice).HasColumnName("selling_price").HasColumnType("decimal(18,2)");
            entity.Property(e => e.ImageReference).HasColumnName("image_reference").HasMaxLength(512);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.SearchTags).HasColumnName("search_tags").HasColumnType("text[]");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasCheckConstraint(
                "ck_global_products_selling_mode",
                "selling_mode IN ('PerItem', 'ByWeight')");
            entity.HasCheckConstraint(
                "ck_global_products_selling_mode_unit",
                "selling_mode <> 'ByWeight' OR unit = 'Kilogram'");

            entity.HasIndex(e => e.Barcode)
                .IsUnique()
                .HasFilter("barcode IS NOT NULL")
                .HasDatabaseName("ux_global_products_barcode");
            entity.HasIndex(e => e.Sku)
                .IsUnique()
                .HasFilter("sku IS NOT NULL")
                .HasDatabaseName("ux_global_products_sku");
            entity.HasIndex(e => e.GlobalCategoryId).HasDatabaseName("ix_global_products_category_id");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_global_products_status");
            entity.HasIndex(e => e.Name).HasDatabaseName("ix_global_products_name");
            entity.HasIndex(e => e.Brand).HasDatabaseName("ix_global_products_brand");

            entity.HasOne<GlobalCategoryRecord>()
                .WithMany()
                .HasForeignKey(e => e.GlobalCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.BusinessTypes)
                .WithOne()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GlobalProductBusinessTypeRecord>(entity =>
        {
            entity.ToTable("global_product_business_types", CatalogSchemaName);
            entity.HasKey(e => new { e.ProductId, e.BusinessTypeId });
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.BusinessTypeId).HasColumnName("business_type_id");
            entity.HasIndex(e => e.BusinessTypeId).HasDatabaseName("ix_global_product_business_types_type");

            entity.HasOne<BusinessTypeRecord>()
                .WithMany()
                .HasForeignKey(e => e.BusinessTypeId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GlobalProductImageRecord>(entity =>
        {
            entity.ToTable("global_product_images", CatalogSchemaName, tb =>
            {
                tb.HasCheckConstraint("ck_global_product_images_version_positive", "version >= 1");
                tb.HasCheckConstraint(
                    "ck_global_product_images_dimensions_positive",
                    "thumb_width > 0 AND thumb_height > 0 AND medium_width > 0 AND medium_height > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GlobalProductId).HasColumnName("global_product_id").IsRequired();
            entity.Property(e => e.StorageKey).HasColumnName("storage_key").IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").IsRequired();
            entity.Property(e => e.ThumbWidth).HasColumnName("thumb_width").IsRequired();
            entity.Property(e => e.ThumbHeight).HasColumnName("thumb_height").IsRequired();
            entity.Property(e => e.MediumWidth).HasColumnName("medium_width").IsRequired();
            entity.Property(e => e.MediumHeight).HasColumnName("medium_height").IsRequired();
            entity.Property(e => e.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasIndex(e => e.GlobalProductId)
                .IsUnique()
                .HasDatabaseName("ux_global_product_images_product");

            entity.HasOne<GlobalProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.GlobalProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_global_product_images_global_products");
        });

        modelBuilder.Entity<CatalogTemplateRecord>(entity =>
        {
            entity.ToTable("catalog_templates", CatalogSchemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(120).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(e => e.IconReference).HasColumnName("icon_reference").HasMaxLength(512);
            entity.Property(e => e.PrimaryBusinessTypeId).HasColumnName("primary_business_type_id").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.DefaultBatchSize).HasColumnName("default_batch_size");
            entity.Property(e => e.SelectionMode).HasColumnName("selection_mode").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PublishedAtUtc).HasColumnName("published_at_utc");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => e.Slug).IsUnique().HasDatabaseName("ux_catalog_templates_slug");
            entity.HasIndex(e => e.Status).HasDatabaseName("ix_catalog_templates_status");
            entity.HasIndex(e => e.PrimaryBusinessTypeId)
                .HasDatabaseName("ix_catalog_templates_primary_business_type_id");

            entity.HasOne<BusinessTypeRecord>()
                .WithMany()
                .HasForeignKey(e => e.PrimaryBusinessTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Products)
                .WithOne()
                .HasForeignKey(e => e.CatalogTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CatalogTemplateProductRecord>(entity =>
        {
            entity.ToTable("catalog_template_products", CatalogSchemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CatalogTemplateId).HasColumnName("catalog_template_id");
            entity.Property(e => e.GlobalProductId).HasColumnName("global_product_id");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.IsFeatured).HasColumnName("is_featured");
            entity.Property(e => e.IsFirstBatch).HasColumnName("is_first_batch");

            entity.HasIndex(e => new { e.CatalogTemplateId, e.GlobalProductId })
                .IsUnique()
                .HasDatabaseName("ux_catalog_template_products_template_product");
            entity.HasIndex(e => new { e.CatalogTemplateId, e.SortOrder })
                .HasDatabaseName("ix_catalog_template_products_template_sort");
            entity.HasIndex(e => e.GlobalProductId).HasDatabaseName("ix_catalog_template_products_product_id");

            entity.HasOne<GlobalProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.GlobalProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CatalogImportJobRecord>(entity =>
        {
            entity.ToTable("catalog_import_jobs", CatalogSchemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(260).IsRequired();
            entity.Property(e => e.FileFormat).HasColumnName("file_format").HasMaxLength(16).IsRequired();
            entity.Property(e => e.ContentType).HasColumnName("content_type").HasMaxLength(128);
            entity.Property(e => e.FileSizeBytes).HasColumnName("file_size_bytes");
            entity.Property(e => e.FileSha256).HasColumnName("file_sha256").HasMaxLength(64).IsRequired();
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by").HasMaxLength(128).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.TotalCount).HasColumnName("total_count");
            entity.Property(e => e.ProcessedCount).HasColumnName("processed_count");
            entity.Property(e => e.ImportedCount).HasColumnName("imported_count");
            entity.Property(e => e.SkippedCount).HasColumnName("skipped_count");
            entity.Property(e => e.FailedCount).HasColumnName("failed_count");
            entity.Property(e => e.CurrentStage).HasColumnName("current_stage").HasMaxLength(64);
            entity.Property(e => e.ErrorSummary).HasColumnName("error_summary").HasMaxLength(1000);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.LastHeartbeatAtUtc).HasColumnName("last_heartbeat_at_utc");
            entity.Property(e => e.TargetTemplateId).HasColumnName("target_template_id");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => e.Status).HasDatabaseName("ix_catalog_import_jobs_status");
            entity.HasIndex(e => e.CreatedAtUtc).HasDatabaseName("ix_catalog_import_jobs_created_at");
            entity.HasIndex(e => e.IdempotencyKey)
                .IsUnique()
                .HasFilter("idempotency_key IS NOT NULL")
                .HasDatabaseName("ux_catalog_import_jobs_idempotency_key");
            entity.HasIndex(e => e.TargetTemplateId)
                .HasDatabaseName("ix_catalog_import_jobs_target_template_id");

            entity.HasOne<CatalogTemplateRecord>()
                .WithMany()
                .HasForeignKey(e => e.TargetTemplateId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(e => e.Items)
                .WithOne()
                .HasForeignKey(e => e.CatalogImportJobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CatalogImportItemRecord>(entity =>
        {
            entity.ToTable("catalog_import_items", CatalogSchemaName);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CatalogImportJobId).HasColumnName("catalog_import_job_id");
            entity.Property(e => e.RowNumber).HasColumnName("row_number");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(64);
            entity.Property(e => e.Barcode).HasColumnName("barcode").HasMaxLength(64);
            entity.Property(e => e.GlobalCategoryId).HasColumnName("global_category_id");
            entity.Property(e => e.CategoryName).HasColumnName("category_name").HasMaxLength(200);
            entity.Property(e => e.Unit).HasColumnName("unit").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CostPrice).HasColumnName("cost_price").HasColumnType("decimal(18,2)");
            entity.Property(e => e.SellingPrice).HasColumnName("selling_price").HasColumnType("decimal(18,2)");
            entity.Property(e => e.ImageReference).HasColumnName("image_reference").HasMaxLength(512);
            entity.Property(e => e.SearchTagsRaw).HasColumnName("search_tags_raw").HasMaxLength(1000);
            entity.Property(e => e.BusinessTypesRaw).HasColumnName("business_types_raw").HasMaxLength(512);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.ErrorCode).HasColumnName("error_code").HasMaxLength(128);
            entity.Property(e => e.ErrorMessage).HasColumnName("error_message").HasMaxLength(1000);
            entity.Property(e => e.CreatedGlobalProductId).HasColumnName("created_global_product_id");
            entity.Property(e => e.AttemptCount).HasColumnName("attempt_count");
            entity.Property(e => e.ProcessedAtUtc).HasColumnName("processed_at_utc");

            entity.HasIndex(e => new { e.CatalogImportJobId, e.RowNumber })
                .HasDatabaseName("ix_catalog_import_items_job_row");
            entity.HasIndex(e => new { e.CatalogImportJobId, e.Status })
                .HasDatabaseName("ix_catalog_import_items_job_status");
        });

        modelBuilder.Entity<ComplianceRequirementRecord>(entity =>
        {
            entity.ToTable("privacy_compliance_requirements");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(64).IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(4000).IsRequired();
            entity.Property(e => e.RequirementLevel).HasColumnName("requirement_level").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.OwnerRole).HasColumnName("owner_role").HasMaxLength(120).IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").HasMaxLength(32).IsRequired();
            entity.Property(e => e.EffectiveDate).HasColumnName("effective_date");
            entity.Property(e => e.LastReviewedDate).HasColumnName("last_reviewed_date");
            entity.Property(e => e.NextReviewDate).HasColumnName("next_review_date");
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(4000);
            entity.Property(e => e.SourceReference).HasColumnName("source_reference").HasMaxLength(500);
            entity.Property(e => e.RequiresDpoLegalVerification).HasColumnName("requires_dpo_legal_verification");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });

        modelBuilder.Entity<ComplianceEvidenceRecord>(entity =>
        {
            entity.ToTable("privacy_compliance_evidence");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RequirementId).HasColumnName("requirement_id");
            entity.Property(e => e.Kind).HasColumnName("kind").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Label).HasColumnName("label").HasMaxLength(200).IsRequired();
            entity.Property(e => e.ReferencePath).HasColumnName("reference_path").HasMaxLength(500).IsRequired();
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(1000);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(e => e.RequirementId).HasDatabaseName("ix_privacy_compliance_evidence_requirement_id");
            entity.HasIndex(e => new { e.RequirementId, e.ReferencePath })
                .IsUnique()
                .HasDatabaseName("ux_privacy_compliance_evidence_requirement_path");
        });

        modelBuilder.Entity<ProcessingSystemRecordEntity>(entity =>
        {
            entity.ToTable("privacy_compliance_processing_systems");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.SystemName).HasColumnName("system_name").HasMaxLength(200).IsRequired();
            entity.Property(e => e.Purpose).HasColumnName("purpose").HasMaxLength(2000).IsRequired();
            entity.Property(e => e.DataSubjects).HasColumnName("data_subjects").HasMaxLength(1000).IsRequired();
            entity.Property(e => e.PersonalDataCategories).HasColumnName("personal_data_categories").HasMaxLength(2000).IsRequired();
            entity.Property(e => e.SensitiveDataCategories).HasColumnName("sensitive_data_categories").HasMaxLength(2000);
            entity.Property(e => e.StorageLocation).HasColumnName("storage_location").HasMaxLength(500).IsRequired();
            entity.Property(e => e.RecipientsProcessors).HasColumnName("recipients_processors").HasMaxLength(1000);
            entity.Property(e => e.RetentionSummary).HasColumnName("retention_summary").HasMaxLength(1000);
            entity.Property(e => e.SecurityControls).HasColumnName("security_controls").HasMaxLength(2000);
            entity.Property(e => e.Owner).HasColumnName("owner").HasMaxLength(120).IsRequired();
            entity.Property(e => e.PiaStatus).HasColumnName("pia_status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        });
    }
}
