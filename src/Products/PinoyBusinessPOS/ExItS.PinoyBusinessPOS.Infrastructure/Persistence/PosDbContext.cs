using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

public sealed class PosDbContext : DbContext
{
    public const string SchemaName = "pos";

    public PosDbContext(DbContextOptions<PosDbContext> options)
        : base(options)
    {
    }

    internal DbSet<POSCustomerRecord> Customers => Set<POSCustomerRecord>();
    internal DbSet<CreditEntryRecord> CreditEntries => Set<CreditEntryRecord>();
    internal DbSet<RepaymentRecord> Repayments => Set<RepaymentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);

        modelBuilder.Entity<POSCustomerRecord>(entity =>
        {
            entity.ToTable("customers", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_customers_status",
                    "status IN ('Active', 'Inactive')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(128).IsRequired();
            entity.Property(e => e.MobileNumber).HasColumnName("mobile_number").HasMaxLength(32);
            entity.Property(e => e.NormalizedMobile).HasColumnName("normalized_mobile").HasMaxLength(32);
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(256);
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(512);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedMobile })
                .IsUnique()
                .HasDatabaseName("ux_customers_org_active_mobile")
                .HasFilter($"status = '{nameof(CustomerStatus.Active)}' AND normalized_mobile IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.DisplayName })
                .HasDatabaseName("ix_customers_org_display_name");

            entity.HasIndex(e => e.OrganizationId)
                .HasDatabaseName("ix_customers_organization_id");
        });

        modelBuilder.Entity<CreditEntryRecord>(entity =>
        {
            entity.ToTable("credit_entries", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_credit_entries_status",
                    "status IN ('Active', 'Reversed')");
                tb.HasCheckConstraint(
                    "ck_credit_entries_amount_positive",
                    "amount > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Remarks)
                .HasColumnName("remarks")
                .HasMaxLength(CreditEntry.RemarksMaxLength)
                .IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.ReversedAtUtc).HasColumnName("reversed_at_utc");
            entity.Property(e => e.ReversalReason)
                .HasColumnName("reversal_reason")
                .HasMaxLength(CreditEntry.ReversalReasonMaxLength);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.CustomerId, e.CreatedAtUtc })
                .HasDatabaseName("ix_credit_entries_org_customer_created");

            entity.HasIndex(e => new { e.OrganizationId, e.CustomerId, e.Status })
                .HasDatabaseName("ix_credit_entries_org_customer_status");

            entity.HasOne<POSCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_credit_entries_customers");
        });

        modelBuilder.Entity<RepaymentRecord>(entity =>
        {
            entity.ToTable("repayments", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_repayments_status",
                    "status IN ('Active', 'Reversed')");
                tb.HasCheckConstraint(
                    "ck_repayments_amount_positive",
                    "amount > 0");
                tb.HasCheckConstraint(
                    "ck_repayments_reversal_consistency",
                    "(status = 'Active' AND reversed_at_utc IS NULL AND reversal_reason IS NULL AND reversed_by IS NULL) OR (status = 'Reversed' AND reversed_at_utc IS NOT NULL AND reversal_reason IS NOT NULL AND reversed_by IS NOT NULL)");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Remarks)
                .HasColumnName("remarks")
                .HasMaxLength(Repayment.RemarksMaxLength);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by").IsRequired();
            entity.Property(e => e.ReversedAtUtc).HasColumnName("reversed_at_utc");
            entity.Property(e => e.ReversalReason)
                .HasColumnName("reversal_reason")
                .HasMaxLength(Repayment.ReversalReasonMaxLength);
            entity.Property(e => e.ReversedBy).HasColumnName("reversed_by");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.CustomerId, e.RecordedAtUtc })
                .HasDatabaseName("ix_repayments_org_customer_recorded");

            entity.HasIndex(e => new { e.OrganizationId, e.CustomerId, e.Status })
                .HasDatabaseName("ix_repayments_org_customer_status");

            entity.HasIndex(e => e.OrganizationId)
                .HasDatabaseName("ix_repayments_organization_id");

            entity.HasOne<POSCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_repayments_customers");
        });
    }
}
