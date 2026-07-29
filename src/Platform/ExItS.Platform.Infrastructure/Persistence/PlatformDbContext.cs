using ExItS.Platform.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence;

public sealed class PlatformDbContext : DbContext
{
    public const string SchemaName = "platform";

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
    }
}
