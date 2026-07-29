using ExItS.Platform.Infrastructure.Persistence.Catalog;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Payments;
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
    }
}
