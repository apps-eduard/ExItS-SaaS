using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Customers;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

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
}
