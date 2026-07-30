using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Expenses;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Idempotency;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Suppliers;
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
    internal DbSet<InventoryAccountRecord> InventoryAccounts => Set<InventoryAccountRecord>();
    internal DbSet<StockMovementRecord> StockMovements => Set<StockMovementRecord>();
    internal DbSet<ExpenseCategoryRecord> ExpenseCategories => Set<ExpenseCategoryRecord>();
    internal DbSet<ExpenseRecord> Expenses => Set<ExpenseRecord>();
    internal DbSet<ExpenseNumberSequenceRecord> ExpenseNumberSequences => Set<ExpenseNumberSequenceRecord>();
    internal DbSet<SupplierRecord> Suppliers => Set<SupplierRecord>();
    internal DbSet<SupplierCodeSequenceRecord> SupplierCodeSequences => Set<SupplierCodeSequenceRecord>();

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

            entity.HasIndex(e => new { e.OrganizationId, e.UpdatedAtUtc })
                .HasDatabaseName("ix_customers_org_updated");

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
            entity.Property(e => e.SourceSaleId).HasColumnName("source_sale_id");
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

            entity.HasIndex(e => e.SourceSaleId)
                .IsUnique()
                .HasDatabaseName("ux_credit_entries_source_sale_id")
                .HasFilter("source_sale_id IS NOT NULL");

            entity.HasOne<POSCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_credit_entries_customers");

            // FK to sales only (not the reverse) avoids circular insert failures when checkout
            // writes sale.linked_credit_entry_id and credit.source_sale_id in one transaction.
            entity.HasOne<SaleRecord>()
                .WithMany()
                .HasForeignKey(e => e.SourceSaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_credit_entries_source_sale");
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
                // Cash: tender + change, no GCash/customer/credit. ManualGCash: no tender/change/customer/credit.
                // Utang: no tender/change/GCash; customer + linked credit required; total > 0.
                tb.HasCheckConstraint(
                    "ck_sales_tender_consistency",
                    "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND total > 0)");
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
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.LinkedCreditEntryId).HasColumnName("linked_credit_entry_id");
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

            entity.HasIndex(e => e.LinkedCreditEntryId)
                .IsUnique()
                .HasDatabaseName("ux_sales_linked_credit_entry_id")
                .HasFilter("linked_credit_entry_id IS NOT NULL");

            entity.HasIndex(e => e.CustomerId)
                .HasDatabaseName("ix_sales_customer_id");

            // No FK from linked_credit_entry_id → credit_entries (circular with source_sale_id).
            // Application + unique filter index enforce one-to-one linkage.
            entity.HasOne<POSCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sales_customers");
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

            entity.HasIndex(e => new { e.OrganizationId, e.ProductId })
                .HasDatabaseName("ix_sale_lines_org_product");

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

        modelBuilder.Entity<InventoryAccountRecord>(entity =>
        {
            entity.ToTable("inventory_accounts", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_inventory_accounts_on_hand_non_negative",
                    "on_hand_quantity >= 0");
                tb.HasCheckConstraint(
                    "ck_inventory_accounts_reorder_level_non_negative",
                    "reorder_level IS NULL OR reorder_level >= 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.IsTracked).HasColumnName("is_tracked").IsRequired();
            entity.Property(e => e.ReorderLevel).HasColumnName("reorder_level").HasPrecision(18, 3);
            entity.Property(e => e.OnHandQuantity)
                .HasColumnName("on_hand_quantity")
                .HasPrecision(18, 3)
                .HasDefaultValue(0m)
                .IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.ProductId })
                .IsUnique()
                .HasDatabaseName("ux_inventory_accounts_org_product");

            entity.HasIndex(e => new { e.OrganizationId, e.IsTracked })
                .HasDatabaseName("ix_inventory_accounts_org_tracked");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_inventory_accounts_products");
        });

        modelBuilder.Entity<StockMovementRecord>(entity =>
        {
            entity.ToTable("stock_movements", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_stock_movements_quantity_effect_nonzero",
                    "quantity_effect <> 0");
                tb.HasCheckConstraint(
                    "ck_stock_movements_movement_type",
                    $"movement_type IN ({string.Join(", ", StockMovementTypes.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_stock_movements_source_type",
                    $"source_type IN ({string.Join(", ", StockMovementSourceTypes.Codes.Select(c => $"'{c}'"))})");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.InventoryAccountId).HasColumnName("inventory_account_id").IsRequired();
            entity.Property(e => e.MovementType)
                .HasColumnName("movement_type")
                .HasMaxLength(StockMovementTypes.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.QuantityEffect)
                .HasColumnName("quantity_effect")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(StockMovement.ReasonMaxLength)
                .IsRequired();
            entity.Property(e => e.SourceType)
                .HasColumnName("source_type")
                .HasMaxLength(StockMovementSourceTypes.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.SourceId).HasColumnName("source_id");
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by").IsRequired();

            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.RecordedAtUtc })
                .HasDatabaseName("ix_stock_movements_org_product_recorded");

            entity.HasIndex(e => new { e.OrganizationId, e.RecordedAtUtc })
                .HasDatabaseName("ix_stock_movements_org_recorded");

            // One unique index covers SaleDeduction and SaleVoidRestoration (movement_type is part of the key).
            // EF cannot model two filtered uniques on the identical column set.
            entity.HasIndex(e => new { e.OrganizationId, e.SourceId, e.ProductId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_stock_movements_sale_source")
                .HasFilter(
                    $"source_type = '{nameof(StockMovementSourceType.Sale)}' AND source_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_stock_movements_opening_stock")
                .HasFilter($"movement_type = '{nameof(StockMovementType.OpeningStock)}'");

            entity.HasOne<InventoryAccountRecord>()
                .WithMany()
                .HasForeignKey(e => e.InventoryAccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_stock_movements_inventory_accounts");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_stock_movements_products");
        });

        modelBuilder.Entity<ExpenseCategoryRecord>(entity =>
        {
            entity.ToTable("expense_categories", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_expense_categories_status",
                    "status IN ('Active', 'Inactive')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(ExpenseCategory.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.NormalizedName)
                .HasColumnName("normalized_name")
                .HasMaxLength(ExpenseCategory.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedName })
                .IsUnique()
                .HasDatabaseName("ux_expense_categories_org_active_name")
                .HasFilter($"status = '{nameof(ExpenseCategoryStatus.Active)}'");

            entity.HasIndex(e => new { e.OrganizationId, e.Name })
                .HasDatabaseName("ix_expense_categories_org_name");
        });

        modelBuilder.Entity<ExpenseRecord>(entity =>
        {
            entity.ToTable("expenses", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_expenses_status",
                    "status IN ('Recorded', 'Voided')");
                tb.HasCheckConstraint(
                    "ck_expenses_payment_method",
                    $"payment_method IN ({string.Join(", ", ExpensePaymentMethods.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_expenses_amount_positive",
                    "amount > 0");
                tb.HasCheckConstraint(
                    "ck_expenses_void_consistency",
                    "(status = 'Recorded' AND voided_at_utc IS NULL AND voided_by IS NULL AND void_reason IS NULL) OR (status = 'Voided' AND voided_at_utc IS NOT NULL AND voided_by IS NOT NULL AND void_reason IS NOT NULL)");
                // Cash: no GCash reference. ManualGCash: optional reference.
                tb.HasCheckConstraint(
                    "ck_expenses_tender_consistency",
                    "(payment_method = 'Cash' AND gcash_reference IS NULL) OR (payment_method = 'ManualGCash')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ExpenseNumber)
                .HasColumnName("expense_number")
                .HasMaxLength(ExpenseNumbers.MaxLength)
                .IsRequired();
            entity.Property(e => e.CategoryId).HasColumnName("category_id").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PaymentMethod)
                .HasColumnName("payment_method")
                .HasMaxLength(ExpensePaymentMethods.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(Expense.DescriptionMaxLength)
                .IsRequired();
            entity.Property(e => e.Payee)
                .HasColumnName("payee")
                .HasMaxLength(Expense.PayeeMaxLength);
            entity.Property(e => e.GcashReference)
                .HasColumnName("gcash_reference")
                .HasMaxLength(Expense.GCashReferenceMaxLength);
            entity.Property(e => e.ExpenseDate).HasColumnName("expense_date").HasColumnType("date").IsRequired();
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by").IsRequired();
            entity.Property(e => e.VoidedAtUtc).HasColumnName("voided_at_utc");
            entity.Property(e => e.VoidedBy).HasColumnName("voided_by");
            entity.Property(e => e.VoidReason)
                .HasColumnName("void_reason")
                .HasMaxLength(Expense.VoidReasonMaxLength);
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.ExpenseNumber })
                .IsUnique()
                .HasDatabaseName("ux_expenses_org_expense_number");

            entity.HasIndex(e => new { e.OrganizationId, e.ExpenseDate })
                .HasDatabaseName("ix_expenses_org_expense_date");

            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_expenses_org_status");

            entity.HasIndex(e => new { e.OrganizationId, e.PaymentMethod })
                .HasDatabaseName("ix_expenses_org_payment_method");

            entity.HasIndex(e => new { e.OrganizationId, e.CategoryId })
                .HasDatabaseName("ix_expenses_org_category_id");

            entity.HasOne<ExpenseCategoryRecord>()
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_expenses_expense_categories");
        });

        modelBuilder.Entity<ExpenseNumberSequenceRecord>(entity =>
        {
            entity.ToTable("expense_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_expense_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_expense_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date").HasColumnType("date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
        });

        modelBuilder.Entity<SupplierRecord>(entity =>
        {
            entity.ToTable("suppliers", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_suppliers_status",
                    "status IN ('Active', 'Inactive')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.SupplierCode)
                .HasColumnName("supplier_code")
                .HasMaxLength(SupplierCodes.MaxLength)
                .IsRequired();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(Supplier.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.NormalizedName)
                .HasColumnName("normalized_name")
                .HasMaxLength(Supplier.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.ContactPerson)
                .HasColumnName("contact_person")
                .HasMaxLength(Supplier.ContactPersonMaxLength);
            entity.Property(e => e.MobileNumber)
                .HasColumnName("mobile_number")
                .HasMaxLength(Supplier.MobileMaxLength);
            entity.Property(e => e.NormalizedMobile)
                .HasColumnName("normalized_mobile")
                .HasMaxLength(Supplier.MobileMaxLength);
            entity.Property(e => e.TelephoneNumber)
                .HasColumnName("telephone_number")
                .HasMaxLength(Supplier.TelephoneMaxLength);
            entity.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(Supplier.EmailMaxLength);
            entity.Property(e => e.NormalizedEmail)
                .HasColumnName("normalized_email")
                .HasMaxLength(Supplier.EmailMaxLength);
            entity.Property(e => e.AddressLine1)
                .HasColumnName("address_line1")
                .HasMaxLength(Supplier.AddressLineMaxLength);
            entity.Property(e => e.AddressLine2)
                .HasColumnName("address_line2")
                .HasMaxLength(Supplier.AddressLineMaxLength);
            entity.Property(e => e.CityMunicipality)
                .HasColumnName("city_municipality")
                .HasMaxLength(Supplier.CityMaxLength);
            entity.Property(e => e.Province)
                .HasColumnName("province")
                .HasMaxLength(Supplier.ProvinceMaxLength);
            entity.Property(e => e.PostalCode)
                .HasColumnName("postal_code")
                .HasMaxLength(Supplier.PostalCodeMaxLength);
            entity.Property(e => e.TaxOrRegistrationNumber)
                .HasColumnName("tax_or_registration_number")
                .HasMaxLength(Supplier.TaxMaxLength);
            entity.Property(e => e.NormalizedTaxOrRegistrationNumber)
                .HasColumnName("normalized_tax_or_registration_number")
                .HasMaxLength(Supplier.TaxMaxLength);
            entity.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasMaxLength(Supplier.NotesMaxLength);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.SupplierCode })
                .IsUnique()
                .HasDatabaseName("ux_suppliers_org_supplier_code");

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedName })
                .IsUnique()
                .HasDatabaseName("ux_suppliers_org_active_name")
                .HasFilter($"status = '{nameof(SupplierStatus.Active)}'");

            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_suppliers_org_status");

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedName })
                .HasDatabaseName("ix_suppliers_org_normalized_name");

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedEmail })
                .HasDatabaseName("ix_suppliers_org_normalized_email")
                .HasFilter("normalized_email IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedMobile })
                .HasDatabaseName("ix_suppliers_org_normalized_mobile")
                .HasFilter("normalized_mobile IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedTaxOrRegistrationNumber })
                .HasDatabaseName("ix_suppliers_org_normalized_tax")
                .HasFilter("normalized_tax_or_registration_number IS NOT NULL");
        });

        modelBuilder.Entity<SupplierCodeSequenceRecord>(entity =>
        {
            entity.ToTable("supplier_code_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_supplier_code_sequences_next_value_positive",
                    "next_value > 0");
            });

            entity.HasKey(e => e.OrganizationId)
                .HasName("pk_supplier_code_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.NextValue).HasColumnName("next_value").IsRequired();
        });
    }
}
