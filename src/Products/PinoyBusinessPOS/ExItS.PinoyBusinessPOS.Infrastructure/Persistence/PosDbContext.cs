using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CashierShifts;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Expenses;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Idempotency;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.OperationalSetup;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Registers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Suppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Purchasing;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;
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
    internal DbSet<PaymentAttemptRecord> PaymentAttempts => Set<PaymentAttemptRecord>();
    internal DbSet<PosIdempotencyRecord> IdempotencyRecords => Set<PosIdempotencyRecord>();
    internal DbSet<ProductCategoryRecord> ProductCategories => Set<ProductCategoryRecord>();
    internal DbSet<CatalogProductRecord> CatalogProducts => Set<CatalogProductRecord>();
    internal DbSet<CatalogImportJobRecord> CatalogImportJobs => Set<CatalogImportJobRecord>();
    internal DbSet<CatalogImportItemResultRecord> CatalogImportItems => Set<CatalogImportItemResultRecord>();
    internal DbSet<SaleRecord> Sales => Set<SaleRecord>();
    internal DbSet<SaleLineRecord> SaleLines => Set<SaleLineRecord>();
    internal DbSet<SaleNumberSequenceRecord> SaleNumberSequences => Set<SaleNumberSequenceRecord>();
    internal DbSet<SaleReturnRecord> SaleReturns => Set<SaleReturnRecord>();
    internal DbSet<SaleReturnLineRecord> SaleReturnLines => Set<SaleReturnLineRecord>();
    internal DbSet<SaleReturnNumberSequenceRecord> SaleReturnNumberSequences => Set<SaleReturnNumberSequenceRecord>();
    internal DbSet<InventoryAccountRecord> InventoryAccounts => Set<InventoryAccountRecord>();
    internal DbSet<StockMovementRecord> StockMovements => Set<StockMovementRecord>();
    internal DbSet<InventoryReorderChangeRecord> InventoryReorderChanges => Set<InventoryReorderChangeRecord>();
    internal DbSet<StockCountRecord> StockCounts => Set<StockCountRecord>();
    internal DbSet<StockCountLineRecord> StockCountLines => Set<StockCountLineRecord>();
    internal DbSet<StockCountNumberSequenceRecord> StockCountNumberSequences => Set<StockCountNumberSequenceRecord>();
    internal DbSet<ExpenseCategoryRecord> ExpenseCategories => Set<ExpenseCategoryRecord>();
    internal DbSet<ExpenseRecord> Expenses => Set<ExpenseRecord>();
    internal DbSet<ExpenseNumberSequenceRecord> ExpenseNumberSequences => Set<ExpenseNumberSequenceRecord>();
    internal DbSet<SupplierRecord> Suppliers => Set<SupplierRecord>();
    internal DbSet<SupplierCodeSequenceRecord> SupplierCodeSequences => Set<SupplierCodeSequenceRecord>();
    internal DbSet<PurchaseOrderRecord> PurchaseOrders => Set<PurchaseOrderRecord>();
    internal DbSet<PurchaseOrderLineRecord> PurchaseOrderLines => Set<PurchaseOrderLineRecord>();
    internal DbSet<PurchaseOrderNumberSequenceRecord> PurchaseOrderNumberSequences => Set<PurchaseOrderNumberSequenceRecord>();
    internal DbSet<GoodsReceiptRecord> GoodsReceipts => Set<GoodsReceiptRecord>();
    internal DbSet<GoodsReceiptLineRecord> GoodsReceiptLines => Set<GoodsReceiptLineRecord>();
    internal DbSet<GoodsReceiptNumberSequenceRecord> GoodsReceiptNumberSequences => Set<GoodsReceiptNumberSequenceRecord>();
    internal DbSet<CashierShiftRecord> CashierShifts => Set<CashierShiftRecord>();
    internal DbSet<CashierShiftMovementRecord> CashierShiftMovements => Set<CashierShiftMovementRecord>();
    internal DbSet<CashierShiftNumberSequenceRecord> CashierShiftNumberSequences => Set<CashierShiftNumberSequenceRecord>();
    internal DbSet<Permissions.PosRoleAssignmentRecord> PosRoleAssignments => Set<Permissions.PosRoleAssignmentRecord>();
    internal DbSet<RegisterRecord> Registers => Set<RegisterRecord>();
    internal DbSet<RegisterCodeSequenceRecord> RegisterCodeSequences => Set<RegisterCodeSequenceRecord>();
    internal DbSet<OperationalSetupRecord> OperationalSetups => Set<OperationalSetupRecord>();

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
            entity.Property(e => e.PlatformBusinessCustomerId)
                .HasColumnName("platform_business_customer_id");
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

            entity.HasIndex(e => new { e.OrganizationId, e.PlatformBusinessCustomerId })
                .IsUnique()
                .HasDatabaseName("ux_customers_org_platform_business_customer")
                .HasFilter("platform_business_customer_id IS NOT NULL");

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

        modelBuilder.Entity<PaymentAttemptRecord>(entity =>
        {
            entity.ToTable("payment_attempts", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_payment_attempts_method",
                    "method IN ('Cash', 'Card', 'GCash', 'ManualGCashTransfer')");
                tb.HasCheckConstraint(
                    "ck_payment_attempts_provider",
                    "provider IN ('None', 'Fake', 'Manual')");
                tb.HasCheckConstraint(
                    "ck_payment_attempts_status",
                    "status IN ('Created', 'Pending', 'RequiresCustomerAction', 'Processing', 'Paid', 'Failed', 'Cancelled', 'Expired', 'Refunded', 'PendingManualVerification')");
                tb.HasCheckConstraint(
                    "ck_payment_attempts_amount_positive",
                    "amount > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.SaleId).HasColumnName("sale_id").IsRequired();
            entity.Property(e => e.Method).HasColumnName("method").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(32).IsRequired();
            entity.Property(e => e.ProviderReference)
                .HasColumnName("provider_reference")
                .HasMaxLength(PaymentAttempt.ProviderReferenceMaxLength);
            entity.Property(e => e.ExternalReference)
                .HasColumnName("external_reference")
                .HasMaxLength(PaymentAttempt.ExternalReferenceMaxLength);
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CheckoutUrl)
                .HasColumnName("checkout_url")
                .HasMaxLength(PaymentAttempt.UrlMaxLength);
            entity.Property(e => e.DeepLink)
                .HasColumnName("deep_link")
                .HasMaxLength(PaymentAttempt.UrlMaxLength);
            entity.Property(e => e.QrPayload)
                .HasColumnName("qr_payload")
                .HasMaxLength(PaymentAttempt.QrPayloadMaxLength);
            entity.Property(e => e.CardBrand).HasColumnName("card_brand").HasMaxLength(32);
            entity.Property(e => e.CardLastFour).HasColumnName("card_last_four").HasMaxLength(4);
            entity.Property(e => e.FailureCode)
                .HasColumnName("failure_code")
                .HasMaxLength(PaymentAttempt.FailureCodeMaxLength);
            entity.Property(e => e.FailureMessage)
                .HasColumnName("failure_message")
                .HasMaxLength(PaymentAttempt.FailureMessageMaxLength);
            entity.Property(e => e.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasMaxLength(PaymentAttempt.IdempotencyKeyMaxLength)
                .IsRequired();
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.VerifiedBy).HasColumnName("verified_by");
            entity.Property(e => e.VerificationReason)
                .HasColumnName("verification_reason")
                .HasMaxLength(PaymentAttempt.FailureMessageMaxLength);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.ProviderEventSequence).HasColumnName("provider_event_sequence");

            entity.HasIndex(e => new { e.OrganizationId, e.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_payment_attempts_org_idempotency");
            entity.HasIndex(e => new { e.Provider, e.ProviderReference })
                .IsUnique()
                .HasFilter("provider_reference IS NOT NULL")
                .HasDatabaseName("ux_payment_attempts_provider_reference");
            entity.HasIndex(e => new { e.OrganizationId, e.ExternalReference })
                .IsUnique()
                .HasFilter("external_reference IS NOT NULL")
                .HasDatabaseName("ux_payment_attempts_org_external_reference");
            entity.HasIndex(e => new { e.OrganizationId, e.SaleId, e.Status })
                .HasDatabaseName("ix_payment_attempts_org_sale_status");
            entity.HasIndex(e => e.OrganizationId)
                .HasDatabaseName("ix_payment_attempts_organization_id");

            entity.HasOne<SaleRecord>()
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_payment_attempts_sales");
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
            entity.Property(e => e.SourceGlobalCategoryId).HasColumnName("source_global_category_id");
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

            entity.HasIndex(e => new { e.OrganizationId, e.SourceGlobalCategoryId })
                .HasDatabaseName("ix_product_categories_org_source_global")
                .HasFilter("source_global_category_id IS NOT NULL");
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
                    "ck_products_selling_mode",
                    "selling_mode IN ('PerItem','ByWeight')");
                tb.HasCheckConstraint(
                    "ck_products_selling_mode_unit",
                    "selling_mode <> 'ByWeight' OR unit_of_measure = 'Kilogram'");
                tb.HasCheckConstraint(
                    "ck_products_barcode_digits",
                    "barcode IS NULL OR barcode ~ '^[0-9]{8,14}$'");
                tb.HasCheckConstraint(
                    "ck_products_catalog_source",
                    "catalog_source IN ('Manual', 'Template', 'GlobalSearch', 'BulkImport')");
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
            entity.Property(e => e.SellingMode)
                .HasColumnName("selling_mode")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(e => e.SellingPrice)
                .HasColumnName("selling_price")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PlatformGlobalProductId).HasColumnName("platform_global_product_id");
            entity.Property(e => e.PlatformTemplateId).HasColumnName("platform_template_id");
            entity.Property(e => e.CatalogSource)
                .HasColumnName("catalog_source")
                .HasMaxLength(32)
                .IsRequired()
                .HasDefaultValue("Manual");
            entity.Property(e => e.CatalogImportedAt).HasColumnName("catalog_imported_at");
            entity.Property(e => e.CatalogSnapshotVersion).HasColumnName("catalog_snapshot_version");
            entity.Property(e => e.SourceGlobalCategoryId).HasColumnName("source_global_category_id");
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

            entity.HasIndex(e => new { e.OrganizationId, e.PlatformGlobalProductId })
                .IsUnique()
                .HasDatabaseName("ux_products_org_platform_global_product")
                .HasFilter("platform_global_product_id IS NOT NULL");

            // Restrict: deactivating or removing a category must never cascade into products.
            entity.HasOne<ProductCategoryRecord>()
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_products_product_categories");
        });

        modelBuilder.Entity<CatalogImportJobRecord>(entity =>
        {
            entity.ToTable("catalog_import_jobs", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_catalog_import_jobs_status",
                    "status IN ('Queued', 'Processing', 'Completed', 'CompletedWithWarnings', 'Failed', 'Cancelled')");
                tb.HasCheckConstraint(
                    "ck_catalog_import_jobs_kind",
                    "job_kind IN ('TemplateBatch', 'SelectedProducts')");
                tb.HasCheckConstraint(
                    "ck_catalog_import_jobs_source",
                    "catalog_source IN ('Manual', 'Template', 'GlobalSearch', 'BulkImport')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.JobKind).HasColumnName("job_kind").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PlatformTemplateId).HasColumnName("platform_template_id");
            entity.Property(e => e.BatchNumber).HasColumnName("batch_number");
            entity.Property(e => e.CatalogSource).HasColumnName("catalog_source").HasMaxLength(32).IsRequired();
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by").HasMaxLength(128).IsRequired();
            entity.Property(e => e.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasMaxLength(CatalogImportRules.IdempotencyKeyMaxLength);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.TotalCount).HasColumnName("total_count");
            entity.Property(e => e.ProcessedCount).HasColumnName("processed_count");
            entity.Property(e => e.ImportedCount).HasColumnName("imported_count");
            entity.Property(e => e.SkippedCount).HasColumnName("skipped_count");
            entity.Property(e => e.FailedCount).HasColumnName("failed_count");
            entity.Property(e => e.CurrentStage).HasColumnName("current_stage").HasMaxLength(64);
            entity.Property(e => e.ErrorSummary)
                .HasColumnName("error_summary")
                .HasMaxLength(CatalogImportRules.ErrorMessageMaxLength);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.LastHeartbeatAtUtc).HasColumnName("last_heartbeat_at_utc");

            entity.HasIndex(e => new { e.OrganizationId, e.CreatedAtUtc })
                .HasDatabaseName("ix_catalog_import_jobs_org_created");
            entity.HasIndex(e => new { e.Status, e.LastHeartbeatAtUtc })
                .HasDatabaseName("ix_catalog_import_jobs_status_heartbeat");
            entity.HasIndex(e => new { e.OrganizationId, e.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_catalog_import_jobs_org_idempotency")
                .HasFilter("idempotency_key IS NOT NULL");

            entity.HasMany(e => e.Items)
                .WithOne(e => e.Job!)
                .HasForeignKey(e => e.CatalogImportJobId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_catalog_import_items_jobs");
        });

        modelBuilder.Entity<CatalogImportItemResultRecord>(entity =>
        {
            entity.ToTable("catalog_import_items", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_catalog_import_items_status",
                    "status IN ('Pending', 'Imported', 'Skipped', 'Failed')");
                tb.HasCheckConstraint(
                    "ck_catalog_import_items_selling_mode",
                    "selling_mode IN ('PerItem','ByWeight')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CatalogImportJobId).HasColumnName("catalog_import_job_id").IsRequired();
            entity.Property(e => e.PlatformGlobalProductId).HasColumnName("platform_global_product_id").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(CatalogProduct.NameMaxLength).IsRequired();
            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(CatalogProduct.DescriptionMaxLength);
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(CatalogProduct.SkuMaxLength);
            entity.Property(e => e.Barcode).HasColumnName("barcode").HasMaxLength(CatalogProduct.BarcodeMaxLength);
            entity.Property(e => e.UnitOfMeasure)
                .HasColumnName("unit_of_measure")
                .HasMaxLength(UnitOfMeasures.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.SellingMode)
                .HasColumnName("selling_mode")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(e => e.SuggestedPrice).HasColumnName("suggested_price").HasPrecision(18, 2);
            entity.Property(e => e.SourceGlobalCategoryId).HasColumnName("source_global_category_id");
            entity.Property(e => e.SourceCategoryName)
                .HasColumnName("source_category_name")
                .HasMaxLength(ProductCategory.NameMaxLength);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.LocalProductId).HasColumnName("local_product_id");
            entity.Property(e => e.ErrorCode).HasColumnName("error_code").HasMaxLength(128);
            entity.Property(e => e.ErrorMessage)
                .HasColumnName("error_message")
                .HasMaxLength(CatalogImportRules.ErrorMessageMaxLength);
            entity.Property(e => e.ProcessedAtUtc).HasColumnName("processed_at_utc");

            entity.HasIndex(e => new { e.CatalogImportJobId, e.SortOrder })
                .HasDatabaseName("ix_catalog_import_items_job_sort");
            entity.HasIndex(e => new { e.CatalogImportJobId, e.Status })
                .HasDatabaseName("ix_catalog_import_items_job_status");
        });

        modelBuilder.Entity<SaleRecord>(entity =>
        {
            entity.ToTable("sales", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_sales_status",
                    "status IN ('Completed', 'Voided', 'AwaitingPayment')");
                tb.HasCheckConstraint(
                    "ck_sales_payment_method",
                    $"payment_method IN ({string.Join(", ", SalePaymentMethods.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_sales_totals_non_negative",
                    "subtotal >= 0 AND total >= 0 AND tax_amount >= 0");
                // Voided sales must carry the full void audit; Completed/AwaitingPayment carry none of it.
                tb.HasCheckConstraint(
                    "ck_sales_void_consistency",
                    "(status IN ('Completed', 'AwaitingPayment') AND voided_at_utc IS NULL AND voided_by IS NULL AND void_reason IS NULL) OR (status = 'Voided' AND voided_at_utc IS NOT NULL AND voided_by IS NOT NULL AND void_reason IS NOT NULL)");
                // Cash: tender + change. ManualGCash / Card / GCash: no tender/change/customer/credit.
                // Utang: customer + linked credit; total > 0.
                tb.HasCheckConstraint(
                    "ck_sales_tender_consistency",
                    "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method IN ('Card', 'GCash') AND amount_tendered IS NULL AND change_amount IS NULL AND customer_id IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND total > 0)");
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
            entity.Property(e => e.TaxAmount).HasColumnName("tax_amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.AmountTendered).HasColumnName("amount_tendered").HasPrecision(18, 2);
            entity.Property(e => e.ChangeAmount).HasColumnName("change_amount").HasPrecision(18, 2);
            entity.Property(e => e.GcashReference)
                .HasColumnName("gcash_reference")
                .HasMaxLength(Sale.GCashReferenceMaxLength);
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.LinkedCreditEntryId).HasColumnName("linked_credit_entry_id");
            entity.Property(e => e.CashierShiftId).HasColumnName("cashier_shift_id");
            entity.Property(e => e.RegisterId).HasColumnName("register_id");
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

            entity.HasIndex(e => e.CashierShiftId)
                .HasDatabaseName("ix_sales_cashier_shift_id");

            entity.HasIndex(e => e.RegisterId)
                .HasDatabaseName("ix_sales_register_id");

            entity.HasOne<CashierShiftRecord>()
                .WithMany()
                .HasForeignKey(e => e.CashierShiftId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sales_cashier_shifts");

            entity.HasOne<RegisterRecord>()
                .WithMany()
                .HasForeignKey(e => e.RegisterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sales_registers");

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
                tb.HasCheckConstraint(
                    "ck_sale_lines_selling_mode",
                    "selling_mode_snapshot IN ('PerItem','ByWeight')");
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
            entity.Property(e => e.SellingModeSnapshot)
                .HasColumnName("selling_mode_snapshot")
                .HasMaxLength(32)
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

        modelBuilder.Entity<SaleReturnRecord>(entity =>
        {
            entity.ToTable("sale_returns", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_sale_returns_status",
                    $"status IN ('{nameof(SaleReturnStatus.Completed)}')");
                tb.HasCheckConstraint(
                    "ck_sale_returns_refund_method",
                    $"refund_method IN ('{nameof(SalePaymentMethod.Cash)}', '{nameof(SalePaymentMethod.ManualGCash)}', '{nameof(SalePaymentMethod.Utang)}')");
                tb.HasCheckConstraint(
                    "ck_sale_returns_total_refund_positive",
                    "total_refund_amount > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ReturnNumber).HasColumnName("return_number").HasMaxLength(ReturnNumbers.MaxLength).IsRequired();
            entity.Property(e => e.SaleId).HasColumnName("sale_id").IsRequired();
            entity.Property(e => e.CashierShiftId).HasColumnName("cashier_shift_id");
            entity.Property(e => e.SourceRegisterId).HasColumnName("source_register_id");
            entity.Property(e => e.RefundRegisterId).HasColumnName("refund_register_id");
            entity.Property(e => e.RefundMethod).HasColumnName("refund_method").HasMaxLength(SalePaymentMethods.CodeMaxLength).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(SaleReturnStatuses.CodeMaxLength).IsRequired();
            entity.Property(e => e.ReturnDate).HasColumnName("return_date").HasColumnType("date").IsRequired();
            entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(SaleReturn.ReasonMaxLength).IsRequired();
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(SaleReturn.NotesMaxLength);
            entity.Property(e => e.TotalRefundAmount).HasColumnName("total_refund_amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.ReturnNumber })
                .IsUnique()
                .HasDatabaseName("ux_sale_returns_org_return_number");

            entity.HasIndex(e => new { e.OrganizationId, e.SaleId, e.CreatedAtUtc })
                .HasDatabaseName("ix_sale_returns_org_sale_created");

            entity.HasIndex(e => new { e.OrganizationId, e.CashierShiftId })
                .HasDatabaseName("ix_sale_returns_org_shift")
                .HasFilter("cashier_shift_id IS NOT NULL");

            entity.HasIndex(e => e.SourceRegisterId)
                .HasDatabaseName("ix_sale_returns_source_register_id");

            entity.HasIndex(e => e.RefundRegisterId)
                .HasDatabaseName("ix_sale_returns_refund_register_id");

            entity.HasOne<SaleRecord>()
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sale_returns_sales");

            entity.HasOne<CashierShiftRecord>()
                .WithMany()
                .HasForeignKey(e => e.CashierShiftId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sale_returns_cashier_shifts");

            entity.HasOne<RegisterRecord>()
                .WithMany()
                .HasForeignKey(e => e.SourceRegisterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sale_returns_source_registers");

            entity.HasOne<RegisterRecord>()
                .WithMany()
                .HasForeignKey(e => e.RefundRegisterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sale_returns_refund_registers");
        });

        modelBuilder.Entity<SaleReturnLineRecord>(entity =>
        {
            entity.ToTable("sale_return_lines", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_sale_return_lines_quantity_positive",
                    "quantity_returned > 0");
                tb.HasCheckConstraint(
                    "ck_sale_return_lines_refund_positive",
                    "refund_amount > 0");
                tb.HasCheckConstraint(
                    "ck_sale_return_lines_restock_disposition",
                    $"restock_disposition IN ('{nameof(RestockDisposition.ReturnToStock)}', '{nameof(RestockDisposition.DoNotRestock)}')");
                tb.HasCheckConstraint(
                    "ck_sale_return_lines_uom",
                    $"uom_snapshot IN ({string.Join(", ", UnitOfMeasures.Codes.Select(c => $"'{c}'"))})");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SaleReturnId).HasColumnName("sale_return_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.SaleLineId).HasColumnName("sale_line_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.ProductNameSnapshot).HasColumnName("product_name_snapshot").HasMaxLength(SaleReturnLine.NameSnapshotMaxLength).IsRequired();
            entity.Property(e => e.UomSnapshot).HasColumnName("uom_snapshot").HasMaxLength(UnitOfMeasures.CodeMaxLength).IsRequired();
            entity.Property(e => e.QuantityReturned).HasColumnName("quantity_returned").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.UnitPriceSnapshot).HasColumnName("unit_price_snapshot").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.RefundAmount).HasColumnName("refund_amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.RestockDisposition).HasColumnName("restock_disposition").HasMaxLength(RestockDispositions.CodeMaxLength).IsRequired();
            entity.Property(e => e.LineReason).HasColumnName("line_reason").HasMaxLength(SaleReturnLine.LineReasonMaxLength);
            entity.Property(e => e.InventoryMovementId).HasColumnName("inventory_movement_id");

            entity.HasIndex(e => new { e.SaleReturnId, e.SaleLineId })
                .IsUnique()
                .HasDatabaseName("ux_sale_return_lines_return_sale_line");

            entity.HasIndex(e => new { e.OrganizationId, e.SaleLineId })
                .HasDatabaseName("ix_sale_return_lines_org_sale_line");

            entity.HasOne<SaleReturnRecord>()
                .WithMany()
                .HasForeignKey(e => e.SaleReturnId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sale_return_lines_returns");

            entity.HasOne<SaleLineRecord>()
                .WithMany()
                .HasForeignKey(e => e.SaleLineId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sale_return_lines_sale_lines");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_sale_return_lines_products");
        });

        modelBuilder.Entity<SaleReturnNumberSequenceRecord>(entity =>
        {
            entity.ToTable("sale_return_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_sale_return_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_sale_return_number_sequences");
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
                tb.HasCheckConstraint(
                    "ck_inventory_accounts_reorder_quantity_positive",
                    "reorder_quantity IS NULL OR reorder_quantity > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.IsTracked).HasColumnName("is_tracked").IsRequired();
            entity.Property(e => e.ReorderLevel).HasColumnName("reorder_level").HasPrecision(18, 3);
            entity.Property(e => e.ReorderQuantity).HasColumnName("reorder_quantity").HasPrecision(18, 3);
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

            entity.HasIndex(e => new { e.OrganizationId, e.SourceId, e.ProductId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_stock_movements_purchase_receipt_source")
                .HasFilter(
                    $"source_type = '{nameof(StockMovementSourceType.PurchaseReceipt)}' AND source_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.SourceId, e.ProductId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_stock_movements_stock_count_source")
                .HasFilter(
                    $"source_type = '{nameof(StockMovementSourceType.StockCount)}' AND source_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.SourceId, e.ProductId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_stock_movements_sale_return_source")
                .HasFilter(
                    $"source_type = '{nameof(StockMovementSourceType.SaleReturn)}' AND source_id IS NOT NULL");

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

        modelBuilder.Entity<InventoryReorderChangeRecord>(entity =>
        {
            entity.ToTable("inventory_reorder_changes");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.InventoryAccountId).HasColumnName("inventory_account_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.PreviousReorderLevel).HasColumnName("previous_reorder_level").HasPrecision(18, 3);
            entity.Property(e => e.NewReorderLevel).HasColumnName("new_reorder_level").HasPrecision(18, 3);
            entity.Property(e => e.PreviousReorderQuantity).HasColumnName("previous_reorder_quantity").HasPrecision(18, 3);
            entity.Property(e => e.NewReorderQuantity).HasColumnName("new_reorder_quantity").HasPrecision(18, 3);
            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(InventoryReorderChange.ReasonMaxLength)
                .IsRequired();
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by").IsRequired();
            entity.Property(e => e.ChangedAtUtc).HasColumnName("changed_at_utc");

            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.ChangedAtUtc })
                .HasDatabaseName("ix_inventory_reorder_changes_org_product_changed");

            entity.HasOne<InventoryAccountRecord>()
                .WithMany()
                .HasForeignKey(e => e.InventoryAccountId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_inventory_reorder_changes_accounts");
        });

        modelBuilder.Entity<StockCountRecord>(entity =>
        {
            entity.ToTable("stock_counts", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_stock_counts_status",
                    "status IN ('Draft', 'InProgress', 'Completed', 'Cancelled')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.CountNumber).HasColumnName("count_number").HasMaxLength(StockCountNumbers.MaxLength);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CountDate).HasColumnName("count_date").IsRequired();
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(StockCount.NotesMaxLength);
            entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
            entity.Property(e => e.StartedBy).HasColumnName("started_by");
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.CompletedBy).HasColumnName("completed_by");
            entity.Property(e => e.CancelledAtUtc).HasColumnName("cancelled_at_utc");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.CountNumber })
                .IsUnique()
                .HasDatabaseName("ux_stock_counts_org_count_number")
                .HasFilter("count_number IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.Status, e.UpdatedAtUtc })
                .HasDatabaseName("ix_stock_counts_org_status_updated");
        });

        modelBuilder.Entity<StockCountLineRecord>(entity =>
        {
            entity.ToTable("stock_count_lines");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StockCountId).HasColumnName("stock_count_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.SystemOnHandSnapshot).HasColumnName("system_on_hand_snapshot").HasPrecision(18, 3);
            entity.Property(e => e.CountedQuantity).HasColumnName("counted_quantity").HasPrecision(18, 3);

            entity.HasIndex(e => new { e.StockCountId, e.ProductId })
                .IsUnique()
                .HasDatabaseName("ux_stock_count_lines_count_product");

            entity.HasOne<StockCountRecord>()
                .WithMany()
                .HasForeignKey(e => e.StockCountId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_stock_count_lines_counts");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_stock_count_lines_products");
        });

        modelBuilder.Entity<StockCountNumberSequenceRecord>(entity =>
        {
            entity.ToTable("stock_count_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_stock_count_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_stock_count_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date").HasColumnType("date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
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

        modelBuilder.Entity<PurchaseOrderRecord>(entity =>
        {
            entity.ToTable("purchase_orders", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_purchase_orders_status",
                    "status IN ('Draft', 'Ordered', 'PartiallyReceived', 'Received', 'Cancelled')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.PoNumber).HasColumnName("po_number").HasMaxLength(PurchaseOrderNumbers.MaxLength);
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.OrderDate).HasColumnName("order_date").IsRequired();
            entity.Property(e => e.ExpectedDeliveryDate).HasColumnName("expected_delivery_date");
            entity.Property(e => e.SupplierReference)
                .HasColumnName("supplier_reference")
                .HasMaxLength(PurchaseOrder.SupplierReferenceMaxLength);
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(PurchaseOrder.NotesMaxLength);
            entity.Property(e => e.OrderedAtUtc).HasColumnName("ordered_at_utc");
            entity.Property(e => e.OrderedBy).HasColumnName("ordered_by");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.PoNumber })
                .IsUnique()
                .HasDatabaseName("ux_purchase_orders_org_po_number")
                .HasFilter("po_number IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_purchase_orders_org_status");

            entity.HasIndex(e => new { e.OrganizationId, e.SupplierId })
                .HasDatabaseName("ix_purchase_orders_org_supplier");

            entity.HasIndex(e => new { e.OrganizationId, e.OrderDate })
                .HasDatabaseName("ix_purchase_orders_org_order_date");

            entity.HasOne<SupplierRecord>()
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_orders_suppliers");
        });

        modelBuilder.Entity<PurchaseOrderLineRecord>(entity =>
        {
            entity.ToTable("purchase_order_lines", tb =>
            {
                tb.HasCheckConstraint("ck_purchase_order_lines_ordered_qty_positive", "ordered_qty > 0");
                tb.HasCheckConstraint("ck_purchase_order_lines_unit_cost_nonnegative", "unit_purchase_cost >= 0");
                tb.HasCheckConstraint("ck_purchase_order_lines_received_qty_nonnegative", "received_qty >= 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.NameSnapshot)
                .HasColumnName("name_snapshot")
                .HasMaxLength(PurchaseOrderLine.NameSnapshotMaxLength);
            entity.Property(e => e.UomSnapshot).HasColumnName("uom_snapshot").HasMaxLength(UnitOfMeasures.CodeMaxLength);
            entity.Property(e => e.OrderedQty).HasColumnName("ordered_qty").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.UnitPurchaseCost).HasColumnName("unit_purchase_cost").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.LineTotal).HasColumnName("line_total").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.ReceivedQty).HasColumnName("received_qty").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.LineNotes).HasColumnName("line_notes").HasMaxLength(PurchaseOrderLine.LineNotesMaxLength);

            entity.HasIndex(e => new { e.PurchaseOrderId, e.LineNumber })
                .IsUnique()
                .HasDatabaseName("ux_purchase_order_lines_po_line_number");

            entity.HasIndex(e => new { e.PurchaseOrderId, e.ProductId })
                .IsUnique()
                .HasDatabaseName("ux_purchase_order_lines_po_product");

            entity.HasOne<PurchaseOrderRecord>()
                .WithMany()
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_purchase_order_lines_purchase_orders");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_purchase_order_lines_products");
        });

        modelBuilder.Entity<PurchaseOrderNumberSequenceRecord>(entity =>
        {
            entity.ToTable("purchase_order_number_sequences", tb =>
            {
                tb.HasCheckConstraint("ck_po_number_sequences_last_value_positive", "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_purchase_order_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
        });

        modelBuilder.Entity<GoodsReceiptRecord>(entity =>
        {
            entity.ToTable("goods_receipts");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id").IsRequired();
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id").IsRequired();
            entity.Property(e => e.GrnNumber).HasColumnName("grn_number").HasMaxLength(GoodsReceiptNumbers.MaxLength).IsRequired();
            entity.Property(e => e.ReceivedDate).HasColumnName("received_date").IsRequired();
            entity.Property(e => e.DeliveryReference).HasColumnName("delivery_reference").HasMaxLength(GoodsReceipt.DeliveryReferenceMaxLength);
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(GoodsReceipt.NotesMaxLength);
            entity.Property(e => e.ReceivedAtUtc).HasColumnName("received_at_utc");
            entity.Property(e => e.ReceivedBy).HasColumnName("received_by").IsRequired();

            entity.HasIndex(e => new { e.OrganizationId, e.GrnNumber })
                .IsUnique()
                .HasDatabaseName("ux_goods_receipts_org_grn_number");

            entity.HasIndex(e => new { e.OrganizationId, e.PurchaseOrderId })
                .HasDatabaseName("ix_goods_receipts_org_po");

            entity.HasOne<PurchaseOrderRecord>()
                .WithMany()
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_goods_receipts_purchase_orders");

            entity.HasOne<SupplierRecord>()
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_goods_receipts_suppliers");
        });

        modelBuilder.Entity<GoodsReceiptLineRecord>(entity =>
        {
            entity.ToTable("goods_receipt_lines", tb =>
            {
                tb.HasCheckConstraint("ck_goods_receipt_lines_received_qty_positive", "received_qty > 0");
                tb.HasCheckConstraint("ck_goods_receipt_lines_unit_cost_non_negative", "unit_purchase_cost_snapshot >= 0");
                tb.HasCheckConstraint("ck_goods_receipt_lines_line_total_non_negative", "line_total_snapshot >= 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GoodsReceiptId).HasColumnName("goods_receipt_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.PurchaseOrderLineId).HasColumnName("purchase_order_line_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.NameSnapshot).HasColumnName("name_snapshot").HasMaxLength(PurchaseOrderLine.NameSnapshotMaxLength).IsRequired();
            entity.Property(e => e.UomSnapshot).HasColumnName("uom_snapshot").HasMaxLength(UnitOfMeasures.CodeMaxLength).IsRequired();
            entity.Property(e => e.ReceivedQty).HasColumnName("received_qty").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.UnitPurchaseCostSnapshot).HasColumnName("unit_purchase_cost_snapshot").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.LineTotalSnapshot).HasColumnName("line_total_snapshot").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.InventoryMovementId).HasColumnName("inventory_movement_id");

            entity.HasIndex(e => new { e.GoodsReceiptId, e.LineNumber })
                .IsUnique()
                .HasDatabaseName("ux_goods_receipt_lines_grn_line_number");

            entity.HasIndex(e => e.InventoryMovementId)
                .IsUnique()
                .HasDatabaseName("ux_goods_receipt_lines_inventory_movement_id")
                .HasFilter("inventory_movement_id IS NOT NULL");

            entity.HasOne<GoodsReceiptRecord>()
                .WithMany()
                .HasForeignKey(e => e.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_goods_receipt_lines_goods_receipts");

            entity.HasOne<PurchaseOrderLineRecord>()
                .WithMany()
                .HasForeignKey(e => e.PurchaseOrderLineId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_goods_receipt_lines_po_lines");
        });

        modelBuilder.Entity<GoodsReceiptNumberSequenceRecord>(entity =>
        {
            entity.ToTable("grn_number_sequences", tb =>
            {
                tb.HasCheckConstraint("ck_grn_number_sequences_last_value_positive", "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_grn_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
        });

        modelBuilder.Entity<CashierShiftRecord>(entity =>
        {
            entity.ToTable("cashier_shifts", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_cashier_shifts_status",
                    "status IN ('Open', 'Closed', 'Cancelled')");
                tb.HasCheckConstraint(
                    "ck_cashier_shifts_opening_cash_non_negative",
                    "opening_cash_amount >= 0");
                tb.HasCheckConstraint(
                    "ck_cashier_shifts_close_consistency",
                    "(status = 'Open' AND closing_cash_amount IS NULL AND expected_cash_amount_snapshot IS NULL AND cash_variance_amount IS NULL AND closed_at_utc IS NULL AND closed_by IS NULL AND cancelled_at_utc IS NULL AND cancelled_by IS NULL) OR (status = 'Closed' AND closing_cash_amount IS NOT NULL AND expected_cash_amount_snapshot IS NOT NULL AND cash_variance_amount IS NOT NULL AND closed_at_utc IS NOT NULL AND closed_by IS NOT NULL AND cancelled_at_utc IS NULL AND cancelled_by IS NULL) OR (status = 'Cancelled' AND closing_cash_amount IS NULL AND expected_cash_amount_snapshot IS NULL AND cash_variance_amount IS NULL AND closed_at_utc IS NULL AND closed_by IS NULL AND cancelled_at_utc IS NOT NULL AND cancelled_by IS NOT NULL)");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ShiftNumber)
                .HasColumnName("shift_number")
                .HasMaxLength(CashierShiftNumbers.MaxLength)
                .IsRequired();
            entity.Property(e => e.ActorId).HasColumnName("actor_id").IsRequired();
            entity.Property(e => e.RegisterId).HasColumnName("register_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.BusinessDate).HasColumnName("business_date").HasColumnType("date").IsRequired();
            entity.Property(e => e.OpeningCashAmount).HasColumnName("opening_cash_amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.OpenedAtUtc).HasColumnName("opened_at_utc");
            entity.Property(e => e.OpenedBy).HasColumnName("opened_by").IsRequired();
            entity.Property(e => e.ClosingCashAmount).HasColumnName("closing_cash_amount").HasPrecision(18, 2);
            entity.Property(e => e.ExpectedCashAmountSnapshot).HasColumnName("expected_cash_amount_snapshot").HasPrecision(18, 2);
            entity.Property(e => e.CashVarianceAmount).HasColumnName("cash_variance_amount").HasPrecision(18, 2);
            entity.Property(e => e.ClosingNotes)
                .HasColumnName("closing_notes")
                .HasMaxLength(CashierShift.ClosingNotesMaxLength);
            entity.Property(e => e.ClosedAtUtc).HasColumnName("closed_at_utc");
            entity.Property(e => e.ClosedBy).HasColumnName("closed_by");
            entity.Property(e => e.CancelledAtUtc).HasColumnName("cancelled_at_utc");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.ShiftNumber })
                .IsUnique()
                .HasDatabaseName("ux_cashier_shifts_org_shift_number");

            entity.HasIndex(e => new { e.OrganizationId, e.ActorId })
                .IsUnique()
                .HasDatabaseName("ux_cashier_shifts_org_actor_open")
                .HasFilter("status = 'Open'");

            entity.HasIndex(e => new { e.OrganizationId, e.RegisterId })
                .IsUnique()
                .HasDatabaseName("ux_cashier_shifts_org_register_open")
                .HasFilter("status = 'Open' AND register_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.Status, e.OpenedAtUtc })
                .HasDatabaseName("ix_cashier_shifts_org_status_opened");

            entity.HasIndex(e => e.RegisterId)
                .HasDatabaseName("ix_cashier_shifts_register_id");

            entity.HasOne<RegisterRecord>()
                .WithMany()
                .HasForeignKey(e => e.RegisterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_cashier_shifts_registers");
        });

        modelBuilder.Entity<CashierShiftMovementRecord>(entity =>
        {
            entity.ToTable("cashier_shift_movements", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_cashier_shift_movements_type",
                    "movement_type IN ('CashIn', 'CashOut')");
                tb.HasCheckConstraint(
                    "ck_cashier_shift_movements_amount_positive",
                    "amount > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ShiftId).HasColumnName("shift_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.MovementType).HasColumnName("movement_type").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(CashierShiftMovement.ReasonMaxLength)
                .IsRequired();
            entity.Property(e => e.Reference)
                .HasColumnName("reference")
                .HasMaxLength(CashierShiftMovement.ReferenceMaxLength);
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by").IsRequired();

            entity.HasIndex(e => new { e.OrganizationId, e.ShiftId, e.RecordedAtUtc })
                .HasDatabaseName("ix_cashier_shift_movements_org_shift_recorded");

            entity.HasOne<CashierShiftRecord>()
                .WithMany()
                .HasForeignKey(e => e.ShiftId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_cashier_shift_movements_shifts");
        });

        modelBuilder.Entity<CashierShiftNumberSequenceRecord>(entity =>
        {
            entity.ToTable("cashier_shift_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_cashier_shift_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_cashier_shift_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
        });

        modelBuilder.Entity<Permissions.PosRoleAssignmentRecord>(entity =>
        {
            entity.ToTable("pos_role_assignments", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_pos_role_assignments_role",
                    "role IN ('Owner', 'Admin', 'StoreManager', 'Cashier', 'InventoryStaff', 'ReportingUser')");
                tb.HasCheckConstraint(
                    "ck_pos_role_assignments_status",
                    "status IN ('Active', 'Revoked')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ActorId).HasColumnName("actor_id").IsRequired();
            entity.Property(e => e.Role).HasColumnName("role").HasMaxLength(32).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.AssignedAtUtc).HasColumnName("assigned_at_utc");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by").IsRequired();
            entity.Property(e => e.RevokedAtUtc).HasColumnName("revoked_at_utc");
            entity.Property(e => e.RevokedBy).HasColumnName("revoked_by");
            entity.Property(e => e.RevocationReason)
                .HasColumnName("revocation_reason")
                .HasMaxLength(Domain.Permissions.PosRoleAssignment.RevocationReasonMaxLength);
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.ActorId })
                .IsUnique()
                .HasDatabaseName("ux_pos_role_assignments_org_actor_active")
                .HasFilter("status = 'Active'");

            entity.HasIndex(e => new { e.OrganizationId, e.Status, e.AssignedAtUtc })
                .HasDatabaseName("ix_pos_role_assignments_org_status_assigned");

            entity.HasIndex(e => new { e.OrganizationId, e.Role, e.Status })
                .HasDatabaseName("ix_pos_role_assignments_org_role_status");

            entity.HasIndex(e => new { e.OrganizationId, e.ActorId, e.Status })
                .HasDatabaseName("ix_pos_role_assignments_org_actor_status");

            entity.HasIndex(e => new { e.OrganizationId, e.RevokedAtUtc })
                .HasDatabaseName("ix_pos_role_assignments_org_revoked");
        });

        modelBuilder.Entity<RegisterRecord>(entity =>
        {
            entity.ToTable("registers", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_registers_status",
                    "status IN ('Active', 'Inactive')");
                tb.HasCheckConstraint(
                    "ck_registers_code_format",
                    "register_code ~ '^REG-[0-9]{6}$'");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.RegisterCode)
                .HasColumnName("register_code")
                .HasMaxLength(RegisterCodes.MaxLength)
                .IsRequired();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(Register.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.NormalizedName)
                .HasColumnName("normalized_name")
                .HasMaxLength(Register.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(Register.DescriptionMaxLength);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by").IsRequired();
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.RegisterCode })
                .IsUnique()
                .HasDatabaseName("ux_registers_org_register_code");

            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedName })
                .IsUnique()
                .HasDatabaseName("ux_registers_org_normalized_name");

            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_registers_org_status");

            entity.HasIndex(e => e.OrganizationId)
                .HasDatabaseName("ix_registers_organization_id");
        });

        modelBuilder.Entity<RegisterCodeSequenceRecord>(entity =>
        {
            entity.ToTable("register_code_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_register_code_sequences_next_value_positive",
                    "next_value > 0");
            });

            entity.HasKey(e => e.OrganizationId)
                .HasName("pk_register_code_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.NextValue).HasColumnName("next_value").IsRequired();
        });

        modelBuilder.Entity<OperationalSetupRecord>(entity =>
        {
            entity.ToTable("operational_setups", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_operational_setups_tax_pricing_mode",
                    "tax_pricing_mode IN ('TaxExclusive', 'TaxInclusive')");
                tb.HasCheckConstraint(
                    "ck_operational_setups_tax_rate_range",
                    "tax_rate_percent >= 0 AND tax_rate_percent <= 100");
                tb.HasCheckConstraint(
                    "ck_operational_setups_completed_consistency",
                    "(is_completed = FALSE AND completed_at_utc IS NULL) OR (is_completed = TRUE AND completed_at_utc IS NOT NULL)");
            });

            entity.HasKey(e => e.OrganizationId);
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.StoreDisplayName)
                .HasColumnName("store_display_name")
                .HasMaxLength(PosOperationalSetup.StoreDisplayNameMaxLength)
                .IsRequired();
            entity.Property(e => e.CurrencyCode)
                .HasColumnName("currency_code")
                .HasMaxLength(PosOperationalSetup.CurrencyCodeMaxLength)
                .IsRequired();
            entity.Property(e => e.TaxPricingMode)
                .HasColumnName("tax_pricing_mode")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(e => e.TaxRatePercent)
                .HasColumnName("tax_rate_percent")
                .HasPrecision(5, 2)
                .IsRequired();
            entity.Property(e => e.ReceiptHeader)
                .HasColumnName("receipt_header")
                .HasMaxLength(PosOperationalSetup.ReceiptHeaderMaxLength);
            entity.Property(e => e.ReceiptFooter)
                .HasColumnName("receipt_footer")
                .HasMaxLength(PosOperationalSetup.ReceiptFooterMaxLength);
            entity.Property(e => e.BusinessAddress)
                .HasColumnName("business_address")
                .HasMaxLength(PosOperationalSetup.BusinessAddressMaxLength);
            entity.Property(e => e.ContactPhone)
                .HasColumnName("contact_phone")
                .HasMaxLength(PosOperationalSetup.ContactPhoneMaxLength);
            entity.Property(e => e.DefaultRegisterId).HasColumnName("default_register_id");
            entity.Property(e => e.IsCompleted).HasColumnName("is_completed").IsRequired();
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by").IsRequired();
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasOne<RegisterRecord>()
                .WithMany()
                .HasForeignKey(e => e.DefaultRegisterId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_operational_setups_default_register");
        });
    }
}
