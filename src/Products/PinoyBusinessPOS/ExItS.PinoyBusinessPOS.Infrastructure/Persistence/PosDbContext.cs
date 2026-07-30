using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Idempotency;
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
    internal DbSet<CreditDueDateChangeRecord> CreditDueDateChanges => Set<CreditDueDateChangeRecord>();
    internal DbSet<RepaymentRecord> Repayments => Set<RepaymentRecord>();
    internal DbSet<PosIdempotencyRecord> IdempotencyRecords => Set<PosIdempotencyRecord>();
    internal DbSet<ProductCategoryRecord> ProductCategories => Set<ProductCategoryRecord>();
    internal DbSet<CatalogProductRecord> CatalogProducts => Set<CatalogProductRecord>();

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
            entity.Property(e => e.CurrentDueDate)
                .HasColumnName("current_due_date")
                .HasColumnType("date");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.CustomerId, e.CreatedAtUtc })
                .HasDatabaseName("ix_credit_entries_org_customer_created");

            entity.HasIndex(e => new { e.OrganizationId, e.CustomerId, e.Status })
                .HasDatabaseName("ix_credit_entries_org_customer_status");

            entity.HasIndex(e => new { e.OrganizationId, e.CurrentDueDate })
                .HasDatabaseName("ix_credit_entries_org_current_due_date");

            entity.HasOne<POSCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_credit_entries_customers");
        });

        modelBuilder.Entity<CreditDueDateChangeRecord>(entity =>
        {
            entity.ToTable("credit_due_date_changes");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.CreditEntryId).HasColumnName("credit_entry_id").IsRequired();
            entity.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();
            entity.Property(e => e.PreviousDueDate)
                .HasColumnName("previous_due_date")
                .HasColumnType("date");
            entity.Property(e => e.NewDueDate)
                .HasColumnName("new_due_date")
                .HasColumnType("date");
            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(CreditDueDateChange.ReasonMaxLength)
                .IsRequired();
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by").IsRequired();
            entity.Property(e => e.ChangedAtUtc).HasColumnName("changed_at_utc");

            entity.HasIndex(e => new { e.OrganizationId, e.CreditEntryId, e.ChangedAtUtc })
                .HasDatabaseName("ix_credit_due_date_changes_org_credit_changed");

            entity.HasIndex(e => new { e.OrganizationId, e.ChangedAtUtc })
                .HasDatabaseName("ix_credit_due_date_changes_org_changed");

            entity.HasOne<CreditEntryRecord>()
                .WithMany()
                .HasForeignKey(e => e.CreditEntryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_credit_due_date_changes_credit_entries");

            entity.HasOne<POSCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_credit_due_date_changes_customers");
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

        modelBuilder.Entity<PosIdempotencyRecord>(entity =>
        {
            entity.ToTable("idempotency_records");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.OperationType).HasColumnName("operation_type").HasMaxLength(128).IsRequired();
            entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128).IsRequired();
            entity.Property(e => e.PayloadHash).HasColumnName("payload_hash").HasMaxLength(128).IsRequired();
            entity.Property(e => e.OperationId).HasColumnName("operation_id");
            entity.Property(e => e.OutcomeCode).HasColumnName("outcome_code").HasMaxLength(64).IsRequired();
            entity.Property(e => e.OutcomeBodyJson).HasColumnName("outcome_body_json");
            entity.Property(e => e.ServerReference).HasColumnName("server_reference").HasMaxLength(128);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.ProductCode, e.OperationType, e.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_idempotency_org_product_type_key");

            entity.HasIndex(e => new { e.OrganizationId, e.CreatedAtUtc })
                .HasDatabaseName("ix_idempotency_org_created");
        });

        modelBuilder.Entity<ProductCategoryRecord>(entity =>
        {
            entity.ToTable("product_categories", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_product_categories_status",
                    "status IN ('Active', 'Inactive')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(ProductCategory.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.NormalizedName)
                .HasColumnName("normalized_name")
                .HasMaxLength(ProductCategory.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            // Only Active category names are unique — an inactive name can be reused.
            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedName })
                .IsUnique()
                .HasDatabaseName("ux_product_categories_org_active_name")
                .HasFilter($"status = '{nameof(ProductCategoryStatus.Active)}'");

            entity.HasIndex(e => new { e.OrganizationId, e.Name })
                .HasDatabaseName("ix_product_categories_org_name");
        });

        modelBuilder.Entity<CatalogProductRecord>(entity =>
        {
            entity.ToTable("products", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_products_status",
                    "status IN ('Active', 'Inactive')");
                tb.HasCheckConstraint(
                    "ck_products_selling_price_non_negative",
                    "selling_price >= 0");
                tb.HasCheckConstraint(
                    "ck_products_unit_of_measure",
                    $"unit_of_measure IN ({string.Join(", ", UnitOfMeasures.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_products_barcode_digits",
                    "barcode IS NULL OR barcode ~ '^[0-9]{8,14}$'");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(CatalogProduct.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(CatalogProduct.DescriptionMaxLength);
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(CatalogProduct.SkuMaxLength);
            entity.Property(e => e.NormalizedSku)
                .HasColumnName("normalized_sku")
                .HasMaxLength(CatalogProduct.SkuMaxLength);
            entity.Property(e => e.Barcode)
                .HasColumnName("barcode")
                .HasMaxLength(CatalogProduct.BarcodeMaxLength);
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.UnitOfMeasure)
                .HasColumnName("unit_of_measure")
                .HasMaxLength(UnitOfMeasures.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.SellingPrice)
                .HasColumnName("selling_price")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            // No status filter: SKUs and barcodes of inactive products stay reserved.
            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedSku })
                .IsUnique()
                .HasDatabaseName("ux_products_org_normalized_sku")
                .HasFilter("normalized_sku IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.Barcode })
                .IsUnique()
                .HasDatabaseName("ux_products_org_barcode")
                .HasFilter("barcode IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.Name })
                .HasDatabaseName("ix_products_org_name");

            entity.HasIndex(e => new { e.OrganizationId, e.CategoryId })
                .HasDatabaseName("ix_products_org_category");

            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_products_org_status");

            // Restrict: deactivating or removing a category must never cascade into products.
            entity.HasOne<ProductCategoryRecord>()
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_products_product_categories");
        });
    }
}
