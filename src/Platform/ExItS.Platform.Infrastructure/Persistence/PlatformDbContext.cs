using ExItS.Platform.Infrastructure.Persistence.Access;
using ExItS.Platform.Infrastructure.Persistence.Audit;
using ExItS.Platform.Infrastructure.Persistence.Authorization;
using ExItS.Platform.Infrastructure.Persistence.Catalog;
using ExItS.Platform.Infrastructure.Persistence.Entitlements;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Payments;
using ExItS.Platform.Infrastructure.Persistence.Personal;
using ExItS.Platform.Infrastructure.Persistence.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence;

public sealed class PlatformDbContext : DbContext
{
    public const string SchemaName = "platform";

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
    internal DbSet<TrialDefinitionRecord> TrialDefinitions => Set<TrialDefinitionRecord>();
    internal DbSet<TrialDefinitionFeatureGrantRecord> TrialDefinitionFeatureGrants => Set<TrialDefinitionFeatureGrantRecord>();
    internal DbSet<PlatformOrganizationRecord> Organizations => Set<PlatformOrganizationRecord>();
    internal DbSet<SubscriptionRecord> Subscriptions => Set<SubscriptionRecord>();
    internal DbSet<SaaSPaymentRecord> SaaSPayments => Set<SaaSPaymentRecord>();
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
    internal DbSet<PlatformCredentialTokenRecord> PlatformCredentialTokens => Set<PlatformCredentialTokenRecord>();
    internal DbSet<PlatformExternalLoginRecord> PlatformExternalLogins => Set<PlatformExternalLoginRecord>();
    internal DbSet<OrganizationMembershipRecord> OrganizationMemberships => Set<OrganizationMembershipRecord>();
    internal DbSet<OrganizationInvitationRecord> OrganizationInvitations => Set<OrganizationInvitationRecord>();
    internal DbSet<BusinessCustomerRecord> BusinessCustomers => Set<BusinessCustomerRecord>();
    internal DbSet<CreditCustomerRecord> CreditCustomers => Set<CreditCustomerRecord>();
    internal DbSet<CustomerLinkRequestRecord> CustomerLinkRequests => Set<CustomerLinkRequestRecord>();
    internal DbSet<LinkedCustomerAppUserRecord> LinkedCustomerAppUsers => Set<LinkedCustomerAppUserRecord>();
    internal DbSet<ProductAccessAssignmentRecord> ProductAccessAssignments => Set<ProductAccessAssignmentRecord>();
    internal DbSet<PlatformRoleAssignmentRecord> PlatformRoleAssignments => Set<PlatformRoleAssignmentRecord>();
    internal DbSet<PlatformRoleDefinitionRecord> PlatformRoleDefinitions => Set<PlatformRoleDefinitionRecord>();
    internal DbSet<PlatformCustomRoleAssignmentRecord> PlatformCustomRoleAssignments => Set<PlatformCustomRoleAssignmentRecord>();
    internal DbSet<OrganizationRoleDefinitionRecord> OrganizationRoleDefinitions => Set<OrganizationRoleDefinitionRecord>();
    internal DbSet<OrganizationCustomRoleAssignmentRecord> OrganizationCustomRoleAssignments => Set<OrganizationCustomRoleAssignmentRecord>();
    internal DbSet<AuditRecordRecord> AuditRecords => Set<AuditRecordRecord>();
    internal DbSet<PersonalAccountSettingsRecord> PersonalAccountSettings => Set<PersonalAccountSettingsRecord>();
    internal DbSet<PersonalContactRecord> PersonalContacts => Set<PersonalContactRecord>();
    internal DbSet<PersonalDebtRelationshipRecord> PersonalDebtRelationships => Set<PersonalDebtRelationshipRecord>();
    internal DbSet<PersonalUtangEntryRecord> PersonalUtangEntries => Set<PersonalUtangEntryRecord>();
    internal DbSet<PersonalUtangInvitationRecord> PersonalUtangInvitations => Set<PersonalUtangInvitationRecord>();
    internal DbSet<PersonalReminderRecord> PersonalReminders => Set<PersonalReminderRecord>();
    internal DbSet<PersonalInAppNotificationRecord> PersonalInAppNotifications => Set<PersonalInAppNotificationRecord>();
    internal DbSet<PersonalNotificationDeliveryRecord> PersonalNotificationDeliveries => Set<PersonalNotificationDeliveryRecord>();

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
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
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
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.SuspendedAtUtc).HasColumnName("suspended_at_utc");
            entity.Property(e => e.SuspensionReason).HasColumnName("suspension_reason").HasMaxLength(512);
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
            entity.Property(e => e.AcceptedByUserId).HasColumnName("accepted_by_user_id");
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

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
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
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.HasIndex(e => e.OwnerUserIdentityId);
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
            entity.HasIndex(e => e.RelationshipId);

            entity.HasOne<PersonalDebtRelationshipRecord>()
                .WithMany()
                .HasForeignKey(e => e.RelationshipId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<PlatformUserRecord>()
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserIdentityId)
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

            entity.HasOne<PlatformOrganizationRecord>()
                .WithMany()
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BusinessCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.BusinessCustomerId)
                .OnDelete(DeleteBehavior.Restrict);
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
    }
}
