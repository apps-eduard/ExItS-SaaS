using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Idempotency;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;
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
    internal DbSet<SaleRecord> Sales => Set<SaleRecord>();
    internal DbSet<SaleLineRecord> SaleLines => Set<SaleLineRecord>();
    internal DbSet<SaleNumberSequenceRecord> SaleNumberSequences => Set<SaleNumberSequenceRecord>();

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

        modelBuilder.Entity<SaleRecord>(entity =>
        {
            entity.ToTable("sales", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_sales_status",
                    "status IN ('Completed', 'Voided')");
                tb.HasCheckConstraint(
                    "ck_sales_payment_method",
                    $"payment_method IN ({string.Join(", ", SalePaymentMethods.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_sales_totals_non_negative",
                    "subtotal >= 0 AND total >= 0");
                // Voided sales must carry the full void audit; completed sales must carry none of it.
                tb.HasCheckConstraint(
                    "ck_sales_void_consistency",
                    "(status = 'Completed' AND voided_at_utc IS NULL AND voided_by IS NULL AND void_reason IS NULL) OR (status = 'Voided' AND voided_at_utc IS NOT NULL AND voided_by IS NOT NULL AND void_reason IS NOT NULL)");
                // Cash sales carry tender and change and no GCash reference; manual GCash sales carry neither.
                tb.HasCheckConstraint(
                    "ck_sales_tender_consistency",
                    "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL)");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.SaleNumber)
                .HasColumnName("sale_number")
                .HasMaxLength(SaleNumbers.MaxLength)
                .IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PaymentMethod)
                .HasColumnName("payment_method")
                .HasMaxLength(SalePaymentMethods.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Total).HasColumnName("total").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.AmountTendered).HasColumnName("amount_tendered").HasPrecision(18, 2);
            entity.Property(e => e.ChangeAmount).HasColumnName("change_amount").HasPrecision(18, 2);
            entity.Property(e => e.GcashReference)
                .HasColumnName("gcash_reference")
                .HasMaxLength(Sale.GCashReferenceMaxLength);
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by").IsRequired();
            entity.Property(e => e.VoidedAtUtc).HasColumnName("voided_at_utc");
            entity.Property(e => e.VoidedBy).HasColumnName("voided_by");
            entity.Property(e => e.VoidReason)
                .HasColumnName("void_reason")
                .HasMaxLength(Sale.VoidReasonMaxLength);
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            // Sale numbers are unique per organization only — two organizations may hold the same one.
            entity.HasIndex(e => new { e.OrganizationId, e.SaleNumber })
                .IsUnique()
                .HasDatabaseName("ux_sales_org_sale_number");

            entity.HasIndex(e => new { e.OrganizationId, e.RecordedAtUtc })
                .HasDatabaseName("ix_sales_org_recorded_at");

            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_sales_org_status");

            entity.HasIndex(e => new { e.OrganizationId, e.PaymentMethod })
                .HasDatabaseName("ix_sales_org_payment_method");
        });

        modelBuilder.Entity<SaleLineRecord>(entity =>
        {
            entity.ToTable("sale_lines", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_sale_lines_quantity_positive",
                    "quantity > 0");
                tb.HasCheckConstraint(
                    "ck_sale_lines_amounts_non_negative",
                    "unit_price >= 0 AND line_total >= 0");
                tb.HasCheckConstraint(
                    "ck_sale_lines_line_number_positive",
                    "line_number > 0");
                tb.HasCheckConstraint(
                    "ck_sale_lines_unit_of_measure",
                    $"unit_of_measure_snapshot IN ({string.Join(", ", UnitOfMeasures.Codes.Select(c => $"'{c}'"))})");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SaleId).HasColumnName("sale_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.NameSnapshot)
                .HasColumnName("name_snapshot")
                .HasMaxLength(SaleLine.NameSnapshotMaxLength)
                .IsRequired();
            entity.Property(e => e.SkuSnapshot)
                .HasColumnName("sku_snapshot")
                .HasMaxLength(SaleLine.SkuSnapshotMaxLength);
            entity.Property(e => e.BarcodeSnapshot)
                .HasColumnName("barcode_snapshot")
                .HasMaxLength(SaleLine.BarcodeSnapshotMaxLength);
            entity.Property(e => e.UnitOfMeasureSnapshot)
                .HasColumnName("unit_of_measure_snapshot")
                .HasMaxLength(UnitOfMeasures.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2).IsRequired();
            // Measured units admit up to three decimal places; countable units stay whole.
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.LineTotal).HasColumnName("line_total").HasPrecision(18, 2).IsRequired();

            entity.HasIndex(e => new { e.SaleId, e.LineNumber })
                .IsUnique()
                .HasDatabaseName("ux_sale_lines_sale_line_number");

            // Cascade guards against orphan lines. Sales are never deleted, so this never fires in
            // normal operation; it only removes the possibility of dangling line rows.
            entity.HasOne<SaleRecord>()
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_sale_lines_sales");

            // Restrict: products are never hard-deleted, so sold history can never lose its product.
            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sale_lines_products");
        });

        modelBuilder.Entity<SaleNumberSequenceRecord>(entity =>
        {
            entity.ToTable("sale_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_sale_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_sale_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date").HasColumnType("date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
        });
    }
}
