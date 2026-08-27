using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Customers;
using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Financing;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence;

public sealed class BnplDbContext : DbContext
{
    public const string SchemaName = "bnpl";
    public const string DatabaseLogicalName = "ExItS_PinoyBuyNowPayLater";

    public BnplDbContext(DbContextOptions<BnplDbContext> options)
        : base(options)
    {
    }

    internal DbSet<BnplCustomerRecord> Customers => Set<BnplCustomerRecord>();
    internal DbSet<BnplFinancingApplicationRecord> FinancingApplications => Set<BnplFinancingApplicationRecord>();
    internal DbSet<BnplFinancingOfferRecord> FinancingOffers => Set<BnplFinancingOfferRecord>();
    internal DbSet<BnplFinancingDecisionRecord> FinancingDecisions => Set<BnplFinancingDecisionRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        ConfigureCustomers(modelBuilder);
        ConfigureFinancing(modelBuilder);
    }

    private static void ConfigureCustomers(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BnplCustomerRecord>(entity =>
        {
            entity.ToTable("customers", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_bnpl_customers_status",
                    "status IN ('Active', 'Inactive')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(128).IsRequired();
            entity.Property(e => e.Mobile).HasColumnName("mobile").HasMaxLength(32);
            entity.Property(e => e.NormalizedMobile).HasColumnName("normalized_mobile").HasMaxLength(32);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(256);
            entity.Property(e => e.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(256);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.LinkedPersonalPublicUserId)
                .HasColumnName("linked_personal_public_user_id")
                .HasMaxLength(12);
            entity.Property(e => e.LinkedCommerceCustomerId)
                .HasColumnName("linked_commerce_customer_id");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.Property(e => e.Xmin).HasColumnName("xmin").IsRowVersion();

            entity.HasIndex(e => e.OrganizationId)
                .HasDatabaseName("ix_bnpl_customers_organization_id");

            entity.HasIndex(e => new { e.OrganizationId, e.DisplayName })
                .HasDatabaseName("ix_bnpl_customers_org_display_name");

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedMobile })
                .HasDatabaseName("ix_bnpl_customers_org_normalized_mobile");

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedEmail })
                .HasDatabaseName("ix_bnpl_customers_org_normalized_email");

            entity.HasIndex(e => new { e.OrganizationId, e.LinkedPersonalPublicUserId })
                .IsUnique()
                .HasDatabaseName("ux_bnpl_customers_org_linked_personal")
                .HasFilter("linked_personal_public_user_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.LinkedCommerceCustomerId })
                .IsUnique()
                .HasDatabaseName("ux_bnpl_customers_org_linked_commerce")
                .HasFilter("linked_commerce_customer_id IS NOT NULL");
        });
    }

    private static void ConfigureFinancing(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BnplFinancingApplicationRecord>(entity =>
        {
            entity.ToTable("financing_applications", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_bnpl_financing_applications_status",
                    "status IN ('Draft', 'PendingEligibility', 'Offered', 'CustomerAccepted', 'ApprovedPendingSale', 'Declined', 'Cancelled')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.BranchId).HasColumnName("branch_id").IsRequired();
            entity.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PurchaseAmount).HasColumnName("purchase_amount").HasPrecision(18, 2);
            entity.Property(e => e.DownPaymentAmount).HasColumnName("down_payment_amount").HasPrecision(18, 2);
            entity.Property(e => e.RequestedFinanceAmount).HasColumnName("requested_finance_amount").HasPrecision(18, 2);
            entity.Property(e => e.PurchaseDescription).HasColumnName("purchase_description").HasMaxLength(512);
            entity.Property(e => e.MerchantProductReference).HasColumnName("merchant_product_reference").HasMaxLength(128);
            entity.Property(e => e.AggregateVersion).HasColumnName("aggregate_version");
            entity.Property(e => e.EligibilityApproved).HasColumnName("eligibility_approved");
            entity.Property(e => e.EligibilityDecidedAtUtc).HasColumnName("eligibility_decided_at_utc");
            entity.Property(e => e.EligibilityDecidedByActorId).HasColumnName("eligibility_decided_by_actor_id");
            entity.Property(e => e.EligibilityNote).HasColumnName("eligibility_note").HasMaxLength(512);
            entity.Property(e => e.CurrentOfferId).HasColumnName("current_offer_id");
            entity.Property(e => e.AcceptedOfferId).HasColumnName("accepted_offer_id");
            entity.Property(e => e.CreatedByActorId).HasColumnName("created_by_actor_id").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.Property(e => e.Xmin).HasColumnName("xmin").IsRowVersion();

            entity.HasIndex(e => new { e.OrganizationId, e.BranchId })
                .HasDatabaseName("ix_bnpl_financing_applications_org_branch");
            entity.HasIndex(e => new { e.OrganizationId, e.CustomerId })
                .HasDatabaseName("ix_bnpl_financing_applications_org_customer");
            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_bnpl_financing_applications_org_status");

            entity.HasOne<BnplCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_bnpl_financing_applications_customer");

            entity.HasMany(e => e.Offers)
                .WithOne(o => o.Application!)
                .HasForeignKey(o => o.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Decisions)
                .WithOne(d => d.Application!)
                .HasForeignKey(d => d.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BnplFinancingOfferRecord>(entity =>
        {
            entity.ToTable("financing_offers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApplicationId).HasColumnName("application_id").IsRequired();
            entity.Property(e => e.Version).HasColumnName("version");
            entity.Property(e => e.PurchaseAmount).HasColumnName("purchase_amount").HasPrecision(18, 2);
            entity.Property(e => e.DownPaymentAmount).HasColumnName("down_payment_amount").HasPrecision(18, 2);
            entity.Property(e => e.FinancedPrincipal).HasColumnName("financed_principal").HasPrecision(18, 2);
            entity.Property(e => e.CreatedByActorId).HasColumnName("created_by_actor_id").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.IsSuperseded).HasColumnName("is_superseded");
            entity.Property(e => e.AcceptedAtUtc).HasColumnName("accepted_at_utc");
            entity.Property(e => e.AcceptedByActorId).HasColumnName("accepted_by_actor_id");

            entity.HasIndex(e => new { e.ApplicationId, e.Version })
                .IsUnique()
                .HasDatabaseName("ux_bnpl_financing_offers_application_version");
        });

        modelBuilder.Entity<BnplFinancingDecisionRecord>(entity =>
        {
            entity.ToTable("financing_decisions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ApplicationId).HasColumnName("application_id").IsRequired();
            entity.Property(e => e.Stage).HasColumnName("stage").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Outcome).HasColumnName("outcome").HasMaxLength(32).IsRequired();
            entity.Property(e => e.ActorId).HasColumnName("actor_id").IsRequired();
            entity.Property(e => e.DecidedAtUtc).HasColumnName("decided_at_utc").IsRequired();
            entity.Property(e => e.Note).HasColumnName("note").HasMaxLength(512);
            entity.Property(e => e.OfferId).HasColumnName("offer_id");

            entity.HasIndex(e => new { e.ApplicationId, e.DecidedAtUtc })
                .HasDatabaseName("ix_bnpl_financing_decisions_application_time");
        });
    }
}
