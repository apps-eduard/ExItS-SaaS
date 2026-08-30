using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Expenses;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Onboarding;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CashierShifts;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Expenses;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Idempotency;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Onboarding;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.OperationalSetup;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Registers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.SupplierPayables;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Suppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Purchasing;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CustomerOrdering;
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
    internal DbSet<WriteOffRecord> WriteOffs => Set<WriteOffRecord>();
    internal DbSet<PaymentAttemptRecord> PaymentAttempts => Set<PaymentAttemptRecord>();
    internal DbSet<PosIdempotencyRecord> IdempotencyRecords => Set<PosIdempotencyRecord>();
    internal DbSet<ProductCategoryRecord> ProductCategories => Set<ProductCategoryRecord>();
    internal DbSet<ProductBrandRecord> ProductBrands => Set<ProductBrandRecord>();
    internal DbSet<CatalogProductRecord> CatalogProducts => Set<CatalogProductRecord>();
    internal DbSet<CatalogProductUnitRecord> CatalogProductUnits => Set<CatalogProductUnitRecord>();
    internal DbSet<CatalogProductImageRecord> CatalogProductImages => Set<CatalogProductImageRecord>();
    internal DbSet<CatalogImportJobRecord> CatalogImportJobs => Set<CatalogImportJobRecord>();
    internal DbSet<CatalogImportItemResultRecord> CatalogImportItems => Set<CatalogImportItemResultRecord>();
    internal DbSet<SaleRecord> Sales => Set<SaleRecord>();
    internal DbSet<SaleLineRecord> SaleLines => Set<SaleLineRecord>();
    internal DbSet<SaleCommercialDiscountAdjustmentRecord> SaleCommercialDiscountAdjustments =>
        Set<SaleCommercialDiscountAdjustmentRecord>();

    internal DbSet<SalePriceOverrideAdjustmentRecord> SalePriceOverrideAdjustments =>
        Set<SalePriceOverrideAdjustmentRecord>();
    internal DbSet<SaleNumberSequenceRecord> SaleNumberSequences => Set<SaleNumberSequenceRecord>();
    internal DbSet<CustomerOrderRecord> CustomerOrders => Set<CustomerOrderRecord>();
    internal DbSet<CustomerOrderLineRecord> CustomerOrderLines => Set<CustomerOrderLineRecord>();
    internal DbSet<CustomerOrderNumberSequenceRecord> CustomerOrderNumberSequences => Set<CustomerOrderNumberSequenceRecord>();
    internal DbSet<SaleReturnRecord> SaleReturns => Set<SaleReturnRecord>();
    internal DbSet<SaleReturnLineRecord> SaleReturnLines => Set<SaleReturnLineRecord>();
    internal DbSet<SaleReturnNumberSequenceRecord> SaleReturnNumberSequences => Set<SaleReturnNumberSequenceRecord>();
    internal DbSet<InventoryAccountRecord> InventoryAccounts => Set<InventoryAccountRecord>();
    internal DbSet<StockMovementRecord> StockMovements => Set<StockMovementRecord>();
    internal DbSet<InventoryReorderChangeRecord> InventoryReorderChanges => Set<InventoryReorderChangeRecord>();
    internal DbSet<StockCountRecord> StockCounts => Set<StockCountRecord>();
    internal DbSet<StockCountLineRecord> StockCountLines => Set<StockCountLineRecord>();
    internal DbSet<StockCountNumberSequenceRecord> StockCountNumberSequences => Set<StockCountNumberSequenceRecord>();
    internal DbSet<InventoryTransferRecord> InventoryTransfers => Set<InventoryTransferRecord>();
    internal DbSet<InventoryTransferLineRecord> InventoryTransferLines => Set<InventoryTransferLineRecord>();
    internal DbSet<InventoryTransferNumberSequenceRecord> InventoryTransferNumberSequences => Set<InventoryTransferNumberSequenceRecord>();
    internal DbSet<DirectPurchaseReceiptRecord> DirectPurchaseReceipts => Set<DirectPurchaseReceiptRecord>();
    internal DbSet<DirectPurchaseReceiptLineRecord> DirectPurchaseReceiptLines => Set<DirectPurchaseReceiptLineRecord>();
    internal DbSet<DirectPurchaseReceiptNumberSequenceRecord> DirectPurchaseReceiptNumberSequences => Set<DirectPurchaseReceiptNumberSequenceRecord>();
    internal DbSet<StockUseRecord> StockUses => Set<StockUseRecord>();
    internal DbSet<StockUseLineRecord> StockUseLines => Set<StockUseLineRecord>();
    internal DbSet<StockUseNumberSequenceRecord> StockUseNumberSequences => Set<StockUseNumberSequenceRecord>();
    internal DbSet<ProductionDefinitionRecord> ProductionDefinitions => Set<ProductionDefinitionRecord>();
    internal DbSet<ProductionComponentRecord> ProductionComponents => Set<ProductionComponentRecord>();
    internal DbSet<ProductionRunRecord> ProductionRuns => Set<ProductionRunRecord>();
    internal DbSet<ProductionRunMaterialRecord> ProductionRunMaterials => Set<ProductionRunMaterialRecord>();
    internal DbSet<ProductionRunNumberSequenceRecord> ProductionRunNumberSequences => Set<ProductionRunNumberSequenceRecord>();
    internal DbSet<WasteLossRecord> WasteLosses => Set<WasteLossRecord>();
    internal DbSet<WasteLossLineRecord> WasteLossLines => Set<WasteLossLineRecord>();
    internal DbSet<WasteLossNumberSequenceRecord> WasteLossNumberSequences => Set<WasteLossNumberSequenceRecord>();
    internal DbSet<InventoryBranchBalanceRecord> InventoryBranchBalances => Set<InventoryBranchBalanceRecord>();
    internal DbSet<InventoryLotRecord> InventoryLots => Set<InventoryLotRecord>();
    internal DbSet<InventoryLotMovementRecord> InventoryLotMovements => Set<InventoryLotMovementRecord>();
    internal DbSet<ExpenseCategoryRecord> ExpenseCategories => Set<ExpenseCategoryRecord>();
    internal DbSet<ExpenseRecord> Expenses => Set<ExpenseRecord>();
    internal DbSet<ExpenseNumberSequenceRecord> ExpenseNumberSequences => Set<ExpenseNumberSequenceRecord>();
    internal DbSet<SupplierPayableRecord> SupplierPayables => Set<SupplierPayableRecord>();
    internal DbSet<SupplierPayablePaymentRecord> SupplierPayablePayments => Set<SupplierPayablePaymentRecord>();
    internal DbSet<SupplierRecord> Suppliers => Set<SupplierRecord>();
    internal DbSet<SupplierCodeSequenceRecord> SupplierCodeSequences => Set<SupplierCodeSequenceRecord>();
    internal DbSet<ConnectedSupplierRelationshipRecord> ConnectedSupplierRelationships => Set<ConnectedSupplierRelationshipRecord>();
    internal DbSet<SupplierProductExposureRecord> SupplierProductExposures => Set<SupplierProductExposureRecord>();
    internal DbSet<ConnectedBuyerProductShareRecord> ConnectedBuyerProductShares => Set<ConnectedBuyerProductShareRecord>();
    internal DbSet<BuyerSupplierProductLinkRecord> BuyerSupplierProductLinks => Set<BuyerSupplierProductLinkRecord>();
    internal DbSet<ConnectedPurchaseOrderRecord> ConnectedPurchaseOrders => Set<ConnectedPurchaseOrderRecord>();
    internal DbSet<ConnectedPurchaseOrderLineRecord> ConnectedPurchaseOrderLines => Set<ConnectedPurchaseOrderLineRecord>();
    internal DbSet<PurchaseOrderRecord> PurchaseOrders => Set<PurchaseOrderRecord>();
    internal DbSet<PurchaseOrderLineRecord> PurchaseOrderLines => Set<PurchaseOrderLineRecord>();
    internal DbSet<PurchaseOrderNumberSequenceRecord> PurchaseOrderNumberSequences => Set<PurchaseOrderNumberSequenceRecord>();
    internal DbSet<GoodsReceiptRecord> GoodsReceipts => Set<GoodsReceiptRecord>();
    internal DbSet<GoodsReceiptLineRecord> GoodsReceiptLines => Set<GoodsReceiptLineRecord>();
    internal DbSet<GoodsReceiptNumberSequenceRecord> GoodsReceiptNumberSequences => Set<GoodsReceiptNumberSequenceRecord>();
    internal DbSet<CashierShiftRecord> CashierShifts => Set<CashierShiftRecord>();
    internal DbSet<CashierShiftMovementRecord> CashierShiftMovements => Set<CashierShiftMovementRecord>();
    internal DbSet<CashierShiftNumberSequenceRecord> CashierShiftNumberSequences => Set<CashierShiftNumberSequenceRecord>();
    internal DbSet<CashierShiftCashCountLineRecord> CashierShiftCashCountLines => Set<CashierShiftCashCountLineRecord>();
    internal DbSet<OrganizationCashDenominationRecord> OrganizationCashDenominations => Set<OrganizationCashDenominationRecord>();
    internal DbSet<Permissions.PosRoleAssignmentRecord> PosRoleAssignments => Set<Permissions.PosRoleAssignmentRecord>();
    internal DbSet<RegisterRecord> Registers => Set<RegisterRecord>();
    internal DbSet<RegisterCodeSequenceRecord> RegisterCodeSequences => Set<RegisterCodeSequenceRecord>();
    internal DbSet<OperationalSetupRecord> OperationalSetups => Set<OperationalSetupRecord>();
    internal DbSet<OrganizationOnboardingProgressRecord> OrganizationOnboardingProgressRows =>
        Set<OrganizationOnboardingProgressRecord>();

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
            entity.Property(e => e.LinkedPersonalPublicUserId)
                .HasColumnName("linked_personal_public_user_id")
                .HasMaxLength(12);
            entity.Property(e => e.LinkedBuyerOrganizationId)
                .HasColumnName("linked_buyer_organization_id");
            entity.Property(e => e.LinkedBuyerPublicOrganizationId)
                .HasColumnName("linked_buyer_public_organization_id")
                .HasMaxLength(9);
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

            entity.HasIndex(e => new { e.OrganizationId, e.LinkedPersonalPublicUserId })
                .IsUnique()
                .HasDatabaseName("ux_customers_org_linked_personal")
                .HasFilter("linked_personal_public_user_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.LinkedBuyerOrganizationId })
                .IsUnique()
                .HasDatabaseName("ux_customers_org_linked_buyer_org")
                .HasFilter("linked_buyer_organization_id IS NOT NULL");

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

        modelBuilder.Entity<WriteOffRecord>(entity =>
        {
            entity.ToTable("write_offs", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_write_offs_status",
                    "status IN ('Active', 'Reversed')");
                tb.HasCheckConstraint(
                    "ck_write_offs_amount_positive",
                    "amount > 0");
                tb.HasCheckConstraint(
                    "ck_write_offs_reversal_consistency",
                    "(status = 'Active' AND reversed_at_utc IS NULL AND reversal_reason IS NULL AND reversed_by IS NULL) OR (status = 'Reversed' AND reversed_at_utc IS NOT NULL AND reversal_reason IS NOT NULL AND reversed_by IS NOT NULL)");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.CustomerId).HasColumnName("customer_id").IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(WriteOff.ReasonMaxLength)
                .IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by").IsRequired();
            entity.Property(e => e.ReversedAtUtc).HasColumnName("reversed_at_utc");
            entity.Property(e => e.ReversalReason)
                .HasColumnName("reversal_reason")
                .HasMaxLength(WriteOff.ReversalReasonMaxLength);
            entity.Property(e => e.ReversedBy).HasColumnName("reversed_by");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.CustomerId, e.RecordedAtUtc })
                .HasDatabaseName("ix_write_offs_org_customer_recorded");

            entity.HasIndex(e => new { e.OrganizationId, e.CustomerId, e.Status })
                .HasDatabaseName("ix_write_offs_org_customer_status");

            entity.HasIndex(e => e.OrganizationId)
                .HasDatabaseName("ix_write_offs_organization_id");

            entity.HasOne<POSCustomerRecord>()
                .WithMany()
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_write_offs_customers");
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
            entity.Property(e => e.ProviderFinalizedBySystem).HasColumnName("provider_finalized_by_system").IsRequired();

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

        modelBuilder.Entity<ProductBrandRecord>(entity =>
        {
            entity.ToTable("product_brands", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_product_brands_status",
                    "status IN ('Active', 'Inactive')");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(ProductBrand.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.NormalizedName)
                .HasColumnName("normalized_name")
                .HasMaxLength(ProductBrand.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            // Only Active brand names are unique — an inactive name can be reused.
            entity.HasIndex(e => new { e.OrganizationId, e.NormalizedName })
                .IsUnique()
                .HasDatabaseName("ux_product_brands_org_active_name")
                .HasFilter($"status = '{nameof(ProductBrandStatus.Active)}'");

            entity.HasIndex(e => new { e.OrganizationId, e.Name })
                .HasDatabaseName("ix_product_brands_org_name");
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
                tb.HasCheckConstraint(
                    "ck_products_expiration_warning_days",
                    "expiration_warning_days IS NULL OR (expiration_warning_days >= 1 AND expiration_warning_days <= 365)");
                tb.HasCheckConstraint(
                    "ck_products_expiration_warning_requires_tracking",
                    "tracks_expiration OR expiration_warning_days IS NULL");
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
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
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
            entity.Property(e => e.CanExposeToConnectedBuyers)
                .HasColumnName("can_expose_to_connected_buyers")
                .HasDefaultValue(true)
                .IsRequired();
            entity.Property(e => e.IsBlockedFromConnectedBuyers)
                .HasColumnName("is_blocked_from_connected_buyers")
                .HasDefaultValue(false)
                .IsRequired();
            entity.Property(e => e.DefaultConnectedPoPrice)
                .HasColumnName("default_connected_po_price")
                .HasPrecision(18, 2);
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PlatformGlobalProductId).HasColumnName("platform_global_product_id");
            entity.Property(e => e.PlatformTemplateId).HasColumnName("platform_template_id");
            entity.Property(e => e.PlatformBarcode).HasColumnName("platform_barcode").HasMaxLength(14);
            entity.Property(e => e.PlatformImageVersion).HasColumnName("platform_image_version");
            entity.Property(e => e.CatalogSource)
                .HasColumnName("catalog_source")
                .HasMaxLength(32)
                .IsRequired()
                .HasDefaultValue("Manual");
            entity.Property(e => e.CatalogImportedAt).HasColumnName("catalog_imported_at");
            entity.Property(e => e.CatalogSnapshotVersion).HasColumnName("catalog_snapshot_version");
            entity.Property(e => e.SourceGlobalCategoryId).HasColumnName("source_global_category_id");
            entity.Property(e => e.TracksExpiration)
                .HasColumnName("tracks_expiration")
                .IsRequired()
                .HasDefaultValue(false);
            entity.Property(e => e.ExpirationWarningDays).HasColumnName("expiration_warning_days");
            entity.Property(e => e.CanBePurchased)
                .HasColumnName("can_be_purchased")
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.CanBeSold)
                .HasColumnName("can_be_sold")
                .IsRequired()
                .HasDefaultValue(true);
            entity.Property(e => e.CanBeUsedAsIngredient)
                .HasColumnName("can_be_used_as_ingredient")
                .IsRequired()
                .HasDefaultValue(false);
            entity.Property(e => e.IsProduced)
                .HasColumnName("is_produced")
                .IsRequired()
                .HasDefaultValue(false);
            entity.Property(e => e.UsagePreset)
                .HasColumnName("usage_preset")
                .HasMaxLength(64)
                .HasDefaultValue("BuyAndSell");
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            // Alternate key for composite tenant FKs (e.g. customer_order_lines).
            entity.HasAlternateKey(e => new { e.Id, e.OrganizationId })
                .HasName("AK_products_id_organization_id");

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

            entity.HasIndex(e => new { e.OrganizationId, e.BrandId })
                .HasDatabaseName("ix_products_org_brand");

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

            // Restrict: deactivating or removing a brand must never cascade into products.
            entity.HasOne<ProductBrandRecord>()
                .WithMany()
                .HasForeignKey(e => e.BrandId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_products_product_brands");
        });

        modelBuilder.Entity<CatalogProductUnitRecord>(entity =>
        {
            entity.ToTable("product_units", tb =>
            {
                tb.HasCheckConstraint("ck_product_units_kind", "kind IN (0, 1)");
                tb.HasCheckConstraint("ck_product_units_multiplier_positive", "multiplier_to_base > 0");
                tb.HasCheckConstraint(
                    "ck_product_units_selling_price_non_negative",
                    "selling_price IS NULL OR selling_price >= 0");
                tb.HasCheckConstraint("ck_product_units_sort_order_non_negative", "sort_order >= 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.Kind).HasColumnName("kind").IsRequired();
            entity.Property(e => e.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(CatalogProductUnit.DisplayNameMaxLength)
                .IsRequired();
            entity.Property(e => e.ShortLabel)
                .HasColumnName("short_label")
                .HasMaxLength(CatalogProductUnit.ShortLabelMaxLength)
                .IsRequired();
            entity.Property(e => e.MultiplierToBase)
                .HasColumnName("multiplier_to_base")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.SellingPrice)
                .HasColumnName("selling_price")
                .HasPrecision(18, 2);
            entity.Property(e => e.AllowsCustomQuantity)
                .HasColumnName("allows_custom_quantity")
                .IsRequired();
            entity.Property(e => e.IsActive).HasColumnName("is_active").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.ProductId })
                .HasDatabaseName("ix_product_units_org_product");

            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.Kind })
                .HasDatabaseName("ix_product_units_org_product_kind_active")
                .HasFilter("is_active");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_product_units_products");
        });

        modelBuilder.Entity<CatalogProductImageRecord>(entity =>
        {
            entity.ToTable("product_images", tb =>
            {
                tb.HasCheckConstraint("ck_product_images_version_positive", "version >= 1");
                tb.HasCheckConstraint(
                    "ck_product_images_dimensions_positive",
                    "thumb_width > 0 AND thumb_height > 0 AND medium_width > 0 AND medium_height > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.StorageKey).HasColumnName("storage_key").IsRequired();
            entity.Property(e => e.Version).HasColumnName("version").IsRequired();
            entity.Property(e => e.ThumbWidth).HasColumnName("thumb_width").IsRequired();
            entity.Property(e => e.ThumbHeight).HasColumnName("thumb_height").IsRequired();
            entity.Property(e => e.MediumWidth).HasColumnName("medium_width").IsRequired();
            entity.Property(e => e.MediumHeight).HasColumnName("medium_height").IsRequired();
            entity.Property(e => e.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasIndex(e => new { e.OrganizationId, e.ProductId })
                .IsUnique()
                .HasDatabaseName("ux_product_images_org_product");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => new { e.ProductId, e.OrganizationId })
                .HasPrincipalKey(p => new { p.Id, p.OrganizationId })
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_product_images_products");
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
                tb.HasCheckConstraint(
                    "ck_sales_discount_totals_non_negative",
                    "gross_subtotal >= 0 AND line_discount_total >= 0 AND sale_discount_total >= 0 AND discount_total >= 0");
                // Discount reconciliation: gross minus every discount is exactly the net subtotal.
                tb.HasCheckConstraint(
                    "ck_sales_discount_reconciliation",
                    "discount_total = line_discount_total + sale_discount_total AND gross_subtotal - discount_total = subtotal");
                // Voided sales must carry the full void audit; Completed/AwaitingPayment carry none of it.
                tb.HasCheckConstraint(
                    "ck_sales_void_consistency",
                    "(status IN ('Completed', 'AwaitingPayment') AND voided_at_utc IS NULL AND voided_by IS NULL AND void_reason IS NULL) OR (status = 'Voided' AND voided_at_utc IS NOT NULL AND voided_by IS NOT NULL AND void_reason IS NOT NULL)");
                // Cash: tender + change; optional customer; never credit link.
                // ManualGCash / Card / GCash: no tender/change/credit; optional customer.
                // Utang: customer + linked credit; total > 0.
                tb.HasCheckConstraint(
                    "ck_sales_tender_consistency",
                    "(payment_method = 'Cash' AND amount_tendered IS NOT NULL AND change_amount IS NOT NULL AND amount_tendered >= total AND gcash_reference IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'ManualGCash' AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method IN ('Card', 'GCash') AND amount_tendered IS NULL AND change_amount IS NULL AND linked_credit_entry_id IS NULL) OR (payment_method = 'Utang' AND amount_tendered IS NULL AND change_amount IS NULL AND gcash_reference IS NULL AND customer_id IS NOT NULL AND linked_credit_entry_id IS NOT NULL AND total > 0)");
                tb.HasCheckConstraint(
                    "ck_sales_buyer_party_kind",
                    "buyer_party_kind IN ('WalkIn', 'ExternalCustomer', 'Personal', 'Organization')");
                tb.HasCheckConstraint(
                    "ck_sales_stock_reservation",
                    "stock_reservation_state IN ('None', 'Reserved', 'Released', 'Consumed')");
                tb.HasCheckConstraint(
                    "ck_sales_cost_status",
                    "cost_status IS NULL OR cost_status IN ('Complete', 'Partial', 'Unavailable')");
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
            entity.Property(e => e.GrossSubtotal)
                .HasColumnName("gross_subtotal")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.LineDiscountTotal)
                .HasColumnName("line_discount_total")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.SaleDiscountTotal)
                .HasColumnName("sale_discount_total")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.DiscountTotal)
                .HasColumnName("discount_total")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.AmountTendered).HasColumnName("amount_tendered").HasPrecision(18, 2);
            entity.Property(e => e.ChangeAmount).HasColumnName("change_amount").HasPrecision(18, 2);
            entity.Property(e => e.GcashReference)
                .HasColumnName("gcash_reference")
                .HasMaxLength(Sale.GCashReferenceMaxLength);
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.BuyerPartyKind)
                .HasColumnName("buyer_party_kind")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(e => e.BuyerDisplayNameSnapshot)
                .HasColumnName("buyer_display_name_snapshot")
                .HasMaxLength(SaleBuyerParty.DisplayNameMaxLength);
            entity.Property(e => e.BuyerPersonalPublicUserId)
                .HasColumnName("buyer_personal_public_user_id")
                .HasMaxLength(SaleBuyerParty.PersonalPublicUserIdMaxLength);
            entity.Property(e => e.BuyerOrganizationId).HasColumnName("buyer_organization_id");
            entity.Property(e => e.BuyerPublicOrganizationId)
                .HasColumnName("buyer_public_organization_id")
                .HasMaxLength(SaleBuyerParty.PublicOrganizationIdMaxLength);
            entity.Property(e => e.LinkedCreditEntryId).HasColumnName("linked_credit_entry_id");
            entity.Property(e => e.CashierShiftId).HasColumnName("cashier_shift_id");
            entity.Property(e => e.RegisterId).HasColumnName("register_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.StockReservationState)
                .HasColumnName("stock_reservation_state")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by").IsRequired();
            entity.Property(e => e.VoidedAtUtc).HasColumnName("voided_at_utc");
            entity.Property(e => e.VoidedBy).HasColumnName("voided_by");
            entity.Property(e => e.VoidReason)
                .HasColumnName("void_reason")
                .HasMaxLength(Sale.VoidReasonMaxLength);
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.CostStatus)
                .HasColumnName("cost_status")
                .HasMaxLength(16);
            entity.Property(e => e.TotalCostSnapshot)
                .HasColumnName("total_cost_snapshot")
                .HasPrecision(18, 2);
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

            entity.HasIndex(e => new { e.OrganizationId, e.CustomerId, e.RecordedAtUtc })
                .HasDatabaseName("ix_sales_org_customer_recorded_at")
                .HasFilter("customer_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.BuyerPartyKind })
                .HasDatabaseName("ix_sales_org_buyer_party_kind");

            entity.HasIndex(e => new { e.OrganizationId, e.BuyerPersonalPublicUserId })
                .HasDatabaseName("ix_sales_org_buyer_personal")
                .HasFilter("buyer_personal_public_user_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.BuyerOrganizationId })
                .HasDatabaseName("ix_sales_org_buyer_organization")
                .HasFilter("buyer_organization_id IS NOT NULL");

            entity.HasIndex(e => e.CashierShiftId)
                .HasDatabaseName("ix_sales_cashier_shift_id");

            entity.HasIndex(e => e.RegisterId)
                .HasDatabaseName("ix_sales_register_id");

            entity.HasIndex(e => new { e.OrganizationId, e.BranchId })
                .HasDatabaseName("ix_sales_org_branch")
                .HasFilter("branch_id IS NOT NULL");

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
                    "ck_sale_lines_discount_amounts_non_negative",
                    "gross_line_total >= 0 AND line_discount_amount >= 0 AND sale_discount_allocated_amount >= 0");
                tb.HasCheckConstraint(
                    "ck_sale_lines_discount_reconciliation",
                    "gross_line_total - line_discount_amount - sale_discount_allocated_amount = line_total");
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
            entity.Property(e => e.GrossLineTotal)
                .HasColumnName("gross_line_total")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.LineDiscountAmount)
                .HasColumnName("line_discount_amount")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.SaleDiscountAllocatedAmount)
                .HasColumnName("sale_discount_allocated_amount")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.SellingUnitId).HasColumnName("selling_unit_id");
            entity.Property(e => e.SellingUnitNameSnapshot)
                .HasColumnName("selling_unit_name_snapshot")
                .HasMaxLength(SaleLine.SellingUnitNameSnapshotMaxLength);
            entity.Property(e => e.EnteredQuantity).HasColumnName("entered_quantity").HasPrecision(18, 3);
            entity.Property(e => e.MultiplierToBaseSnapshot)
                .HasColumnName("multiplier_to_base_snapshot")
                .HasPrecision(18, 3);
            entity.Property(e => e.UnitCostSnapshot)
                .HasColumnName("unit_cost_snapshot")
                .HasPrecision(18, 2);
            entity.Property(e => e.LineCostSnapshot)
                .HasColumnName("line_cost_snapshot")
                .HasPrecision(18, 2);

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

        modelBuilder.Entity<SaleCommercialDiscountAdjustmentRecord>(entity =>
        {
            entity.ToTable("sale_commercial_discount_adjustments", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_sale_commercial_discount_adjustments_scope",
                    "scope IN ('Line', 'Sale')");
                tb.HasCheckConstraint(
                    "ck_sale_commercial_discount_adjustments_method",
                    "method IN ('Percentage', 'FixedAmount')");
                // Only manual commercial discounts exist. Promotions and regulatory discounts are
                // separate concepts and must not be recorded here.
                tb.HasCheckConstraint(
                    "ck_sale_commercial_discount_adjustments_source",
                    "source = 'Manual'");
                tb.HasCheckConstraint(
                    "ck_sale_commercial_discount_adjustments_amounts",
                    "requested_value > 0 AND calculated_amount >= 0");
                // A line-scoped adjustment always names its line; a sale-scoped one never does.
                tb.HasCheckConstraint(
                    "ck_sale_commercial_discount_adjustments_line_scope",
                    "(scope = 'Line' AND sale_line_id IS NOT NULL) OR (scope = 'Sale' AND sale_line_id IS NULL)");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SaleId).HasColumnName("sale_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.Scope).HasColumnName("scope").HasMaxLength(16).IsRequired();
            entity.Property(e => e.Method).HasColumnName("method").HasMaxLength(16).IsRequired();
            entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(16).IsRequired();
            entity.Property(e => e.RequestedValue)
                .HasColumnName("requested_value")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.CalculatedAmount)
                .HasColumnName("calculated_amount")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(SaleCommercialDiscountRules.ReasonMaxLength)
                .IsRequired();
            entity.Property(e => e.SaleLineId).HasColumnName("sale_line_id");
            entity.Property(e => e.AppliedBy).HasColumnName("applied_by").IsRequired();
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");

            entity.HasIndex(e => new { e.OrganizationId, e.SaleId })
                .HasDatabaseName("ix_sale_commercial_discount_adjustments_org_sale");

            entity.HasIndex(e => e.SaleLineId)
                .HasDatabaseName("ix_sale_commercial_discount_adjustments_sale_line")
                .HasFilter("sale_line_id IS NOT NULL");

            entity.HasOne<SaleRecord>()
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_sale_commercial_discount_adjustments_sales");

            entity.HasOne<SaleLineRecord>()
                .WithMany()
                .HasForeignKey(e => e.SaleLineId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_sale_commercial_discount_adjustments_sale_lines");
        });

        modelBuilder.Entity<SalePriceOverrideAdjustmentRecord>(entity =>
        {
            entity.ToTable("sale_price_override_adjustments", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_sale_price_override_adjustments_prices",
                    "baseline_unit_price >= 0 AND applied_unit_price > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SaleId).HasColumnName("sale_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.SaleLineId).HasColumnName("sale_line_id").IsRequired();
            entity.Property(e => e.BaselineUnitPrice)
                .HasColumnName("baseline_unit_price")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.AppliedUnitPrice)
                .HasColumnName("applied_unit_price")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(SalePriceOverrideRules.ReasonMaxLength)
                .IsRequired();
            entity.Property(e => e.AppliedBy).HasColumnName("applied_by").IsRequired();
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");

            entity.HasIndex(e => new { e.OrganizationId, e.SaleId })
                .HasDatabaseName("ix_sale_price_override_adjustments_org_sale");

            entity.HasIndex(e => e.SaleLineId)
                .HasDatabaseName("ix_sale_price_override_adjustments_sale_line");

            entity.HasOne<SaleRecord>()
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_sale_price_override_adjustments_sales");

            entity.HasOne<SaleLineRecord>()
                .WithMany()
                .HasForeignKey(e => e.SaleLineId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_sale_price_override_adjustments_sale_lines");
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

        modelBuilder.Entity<CustomerOrderRecord>(entity =>
        {
            entity.ToTable("customer_orders", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_customer_orders_status",
                    "status IN ('Draft', 'Submitted', 'Accepted', 'Rejected', 'Cancelled', 'Completed')");
                tb.HasCheckConstraint(
                    "ck_customer_orders_fulfillment_status",
                    "fulfillment_status IN ('Pending', 'Preparing', 'Ready', 'OutForDelivery', 'Delivered', 'ReadyForPickup', 'Collected')");
                tb.HasCheckConstraint(
                    "ck_customer_orders_payment_status",
                    "payment_status IN ('Unpaid', 'Pending', 'Paid')");
                tb.HasCheckConstraint(
                    "ck_customer_orders_payment_method",
                    "payment_method IN ('Cash', 'ManualGCash', 'Utang')");
                tb.HasCheckConstraint(
                    "ck_customer_orders_fulfillment_type",
                    "fulfillment_type IN ('Pickup', 'Delivery')");
                tb.HasCheckConstraint(
                    "ck_customer_orders_party_type",
                    "customer_party_type IN ('Personal', 'Organization')");
                tb.HasCheckConstraint(
                    "ck_customer_orders_stock_reservation",
                    "stock_reservation_state IN ('None', 'Reserved', 'Released', 'Consumed')");
                tb.HasCheckConstraint(
                    "ck_customer_orders_totals_non_negative",
                    "merchandise_subtotal >= 0 AND delivery_fee >= 0 AND total >= 0");
                tb.HasCheckConstraint(
                    "ck_customer_orders_party_xor",
                    "(customer_party_type = 'Personal' AND customer_platform_user_id IS NOT NULL AND customer_buyer_organization_id IS NULL)"
                    + " OR (customer_party_type = 'Organization' AND customer_buyer_organization_id IS NOT NULL AND customer_platform_user_id IS NULL)");
                tb.HasCheckConstraint(
                    "ck_customer_orders_money_identity",
                    "total = merchandise_subtotal + delivery_fee");
                tb.HasCheckConstraint(
                    "ck_customer_orders_delivery_destination_lat_long_pair",
                    "(delivery_destination_latitude IS NULL AND delivery_destination_longitude IS NULL)"
                    + " OR (delivery_destination_latitude IS NOT NULL AND delivery_destination_longitude IS NOT NULL)");
                tb.HasCheckConstraint(
                    "ck_customer_orders_delivery_branch_lat_long_pair",
                    "(delivery_branch_latitude_snapshot IS NULL AND delivery_branch_longitude_snapshot IS NULL)"
                    + " OR (delivery_branch_latitude_snapshot IS NOT NULL AND delivery_branch_longitude_snapshot IS NOT NULL)");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SellerOrganizationId).HasColumnName("seller_organization_id").IsRequired();
            entity.Property(e => e.OrderNumber)
                .HasColumnName("order_number")
                .HasMaxLength(CustomerOrderNumbers.MaxLength)
                .IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.FulfillmentStatus).HasColumnName("fulfillment_status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PaymentStatus).HasColumnName("payment_status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.PaymentMethod)
                .HasColumnName("payment_method")
                .HasMaxLength(CustomerOrderPaymentMethods.CodeMaxLength)
                .IsRequired()
                .HasDefaultValue(nameof(CustomerOrderPaymentMethod.Cash));
            entity.Property(e => e.FulfillmentType).HasColumnName("fulfillment_type").HasMaxLength(32).IsRequired();
            entity.Property(e => e.FulfillmentBranchId).HasColumnName("fulfillment_branch_id").IsRequired();
            entity.Property(e => e.BranchNameSnapshot)
                .HasColumnName("branch_name_snapshot")
                .HasMaxLength(CustomerOrder.BranchNameSnapshotMaxLength)
                .IsRequired();
            entity.Property(e => e.CustomerPartyType).HasColumnName("customer_party_type").HasMaxLength(32).IsRequired();
            entity.Property(e => e.CustomerDisplayNameSnapshot)
                .HasColumnName("customer_display_name_snapshot")
                .HasMaxLength(CustomerOrderParty.DisplayNameMaxLength)
                .IsRequired();
            entity.Property(e => e.CustomerPlatformUserId).HasColumnName("customer_platform_user_id");
            entity.Property(e => e.PlatformBusinessCustomerId).HasColumnName("platform_business_customer_id");
            entity.Property(e => e.CustomerBuyerOrganizationId).HasColumnName("customer_buyer_organization_id");
            entity.Property(e => e.CustomerBuyerPublicOrganizationId)
                .HasColumnName("customer_buyer_public_organization_id")
                .HasMaxLength(CustomerOrderParty.PublicOrganizationIdMaxLength);
            entity.Property(e => e.MerchandiseSubtotal).HasColumnName("merchandise_subtotal").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.DeliveryFee).HasColumnName("delivery_fee").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Total).HasColumnName("total").HasPrecision(18, 2).IsRequired();

            entity.Property(e => e.DeliveryRecipientName)
                .HasColumnName("delivery_recipient_name")
                .HasMaxLength(CustomerOrderDeliverySnapshot.RecipientNameMaxLength);
            entity.Property(e => e.DeliveryRecipientPhone)
                .HasColumnName("delivery_recipient_phone")
                .HasMaxLength(CustomerOrderDeliverySnapshot.RecipientPhoneMaxLength);
            entity.Property(e => e.DeliveryAddressLine1)
                .HasColumnName("delivery_address_line1")
                .HasMaxLength(CustomerOrderDeliverySnapshot.AddressLineMaxLength);
            entity.Property(e => e.DeliveryAddressLine2)
                .HasColumnName("delivery_address_line2")
                .HasMaxLength(CustomerOrderDeliverySnapshot.AddressLineMaxLength);
            entity.Property(e => e.DeliveryCity)
                .HasColumnName("delivery_city")
                .HasMaxLength(CustomerOrderDeliverySnapshot.CityMaxLength);
            entity.Property(e => e.DeliveryNotes)
                .HasColumnName("delivery_notes")
                .HasMaxLength(CustomerOrderDeliverySnapshot.NotesMaxLength);
            entity.Property(e => e.DeliveryDestinationLatitude).HasColumnName("delivery_destination_latitude").HasPrecision(9, 6);
            entity.Property(e => e.DeliveryDestinationLongitude).HasColumnName("delivery_destination_longitude").HasPrecision(9, 6);
            entity.Property(e => e.DeliveryBranchLatitudeSnapshot).HasColumnName("delivery_branch_latitude_snapshot").HasPrecision(9, 6);
            entity.Property(e => e.DeliveryBranchLongitudeSnapshot).HasColumnName("delivery_branch_longitude_snapshot").HasPrecision(9, 6);
            entity.Property(e => e.DeliveryDistanceKm).HasColumnName("delivery_distance_km").HasPrecision(18, 3);
            entity.Property(e => e.DeliveryMinimumOrderAmountSnapshot).HasColumnName("delivery_minimum_order_amount_snapshot").HasPrecision(18, 2);
            entity.Property(e => e.DeliveryBaseFeeSnapshot).HasColumnName("delivery_base_fee_snapshot").HasPrecision(18, 2);
            entity.Property(e => e.DeliveryIncludedDistanceKmSnapshot).HasColumnName("delivery_included_distance_km_snapshot").HasPrecision(18, 3);
            entity.Property(e => e.DeliveryAdditionalFeePerKmSnapshot).HasColumnName("delivery_additional_fee_per_km_snapshot").HasPrecision(18, 2);
            entity.Property(e => e.DeliveryMaximumDistanceKmSnapshot).HasColumnName("delivery_maximum_distance_km_snapshot").HasPrecision(18, 3);
            entity.Property(e => e.DeliveryFreeThresholdSnapshot).HasColumnName("delivery_free_threshold_snapshot").HasPrecision(18, 2);
            entity.Property(e => e.DeliveryDistanceCharge).HasColumnName("delivery_distance_charge").HasPrecision(18, 2);
            entity.Property(e => e.DeliveryFinalFee).HasColumnName("delivery_final_fee").HasPrecision(18, 2);
            entity.Property(e => e.DeliveryFreeApplied).HasColumnName("delivery_free_applied");

            entity.Property(e => e.StockReservationState).HasColumnName("stock_reservation_state").HasMaxLength(32).IsRequired();
            entity.Property(e => e.RejectReason).HasColumnName("reject_reason").HasMaxLength(64);
            entity.Property(e => e.RejectNotes)
                .HasColumnName("reject_notes")
                .HasMaxLength(CustomerOrder.RejectNotesMaxLength);
            entity.Property(e => e.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasMaxLength(CustomerOrder.IdempotencyKeyMaxLength);

            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.SubmittedAtUtc).HasColumnName("submitted_at_utc");
            entity.Property(e => e.SubmittedBy).HasColumnName("submitted_by");
            entity.Property(e => e.AcceptedAtUtc).HasColumnName("accepted_at_utc");
            entity.Property(e => e.AcceptedBy).HasColumnName("accepted_by");
            entity.Property(e => e.RejectedAtUtc).HasColumnName("rejected_at_utc");
            entity.Property(e => e.RejectedBy).HasColumnName("rejected_by");
            entity.Property(e => e.CancelledAtUtc).HasColumnName("cancelled_at_utc");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.CompletedBy).HasColumnName("completed_by");
            entity.Property(e => e.ReadyAtUtc).HasColumnName("ready_at_utc");
            entity.Property(e => e.ReadyBy).HasColumnName("ready_by");
            entity.Property(e => e.OutForDeliveryAtUtc).HasColumnName("out_for_delivery_at_utc");
            entity.Property(e => e.OutForDeliveryBy).HasColumnName("out_for_delivery_by");
            entity.Property(e => e.DeliveredAtUtc).HasColumnName("delivered_at_utc");
            entity.Property(e => e.DeliveredBy).HasColumnName("delivered_by");
            entity.Property(e => e.CollectedAtUtc).HasColumnName("collected_at_utc");
            entity.Property(e => e.CollectedBy).HasColumnName("collected_by");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            // Alternate key for composite tenant FKs from customer_order_lines.
            entity.HasAlternateKey(e => new { e.Id, e.SellerOrganizationId })
                .HasName("AK_customer_orders_id_seller_organization_id");

            entity.HasIndex(e => new { e.SellerOrganizationId, e.OrderNumber })
                .IsUnique()
                .HasDatabaseName("ux_customer_orders_org_order_number");
            entity.HasIndex(e => new { e.SellerOrganizationId, e.Status })
                .HasDatabaseName("ix_customer_orders_org_status");
            entity.HasIndex(e => new { e.SellerOrganizationId, e.CreatedAtUtc })
                .HasDatabaseName("ix_customer_orders_org_created_at");
            entity.HasIndex(e => new { e.SellerOrganizationId, e.CustomerPlatformUserId })
                .HasDatabaseName("ix_customer_orders_org_customer_user")
                .HasFilter("customer_platform_user_id IS NOT NULL");
            entity.HasIndex(e => new { e.SellerOrganizationId, e.CustomerBuyerOrganizationId })
                .HasDatabaseName("ix_customer_orders_org_customer_buyer_org")
                .HasFilter("customer_buyer_organization_id IS NOT NULL");
            entity.HasIndex(e => new { e.CustomerPlatformUserId, e.CreatedAtUtc })
                .HasDatabaseName("ix_customer_orders_customer_user_created_at")
                .HasFilter("customer_platform_user_id IS NOT NULL")
                .IsDescending(false, true);
            entity.HasIndex(e => new { e.CustomerBuyerOrganizationId, e.CreatedAtUtc })
                .HasDatabaseName("ix_customer_orders_customer_buyer_org_created_at")
                .HasFilter("customer_buyer_organization_id IS NOT NULL")
                .IsDescending(false, true);
            entity.HasIndex(e => new { e.SellerOrganizationId, e.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_customer_orders_org_idempotency")
                .HasFilter("idempotency_key IS NOT NULL");
        });

        modelBuilder.Entity<CustomerOrderLineRecord>(entity =>
        {
            entity.ToTable("customer_order_lines", tb =>
            {
                tb.HasCheckConstraint("ck_customer_order_lines_quantity_positive", "quantity > 0");
                tb.HasCheckConstraint(
                    "ck_customer_order_lines_amounts_non_negative",
                    "unit_price >= 0 AND discount >= 0 AND line_total >= 0");
                tb.HasCheckConstraint("ck_customer_order_lines_line_number_positive", "line_number > 0");
                tb.HasCheckConstraint(
                    "ck_customer_order_lines_unit",
                    $"unit_snapshot IN ({string.Join(", ", UnitOfMeasures.Codes.Select(c => $"'{c}'"))})");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id").IsRequired();
            entity.Property(e => e.SellerOrganizationId).HasColumnName("seller_organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.NameSnapshot)
                .HasColumnName("name_snapshot")
                .HasMaxLength(CustomerOrderLine.NameSnapshotMaxLength)
                .IsRequired();
            entity.Property(e => e.SkuSnapshot)
                .HasColumnName("sku_snapshot")
                .HasMaxLength(CustomerOrderLine.SkuSnapshotMaxLength);
            entity.Property(e => e.UnitSnapshot)
                .HasColumnName("unit_snapshot")
                .HasMaxLength(UnitOfMeasures.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Discount).HasColumnName("discount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.LineTotal).HasColumnName("line_total").HasPrecision(18, 2).IsRequired();

            entity.HasIndex(e => new { e.OrderId, e.LineNumber })
                .IsUnique()
                .HasDatabaseName("ux_customer_order_lines_order_line_number");
            entity.HasIndex(e => new { e.SellerOrganizationId, e.ProductId })
                .HasDatabaseName("ix_customer_order_lines_org_product");

            entity.HasOne<CustomerOrderRecord>()
                .WithMany()
                .HasForeignKey(e => new { e.OrderId, e.SellerOrganizationId })
                .HasPrincipalKey(o => new { o.Id, o.SellerOrganizationId })
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_customer_order_lines_orders_tenant");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => new { e.ProductId, e.SellerOrganizationId })
                .HasPrincipalKey(p => new { p.Id, p.OrganizationId })
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_customer_order_lines_products_tenant");
        });

        modelBuilder.Entity<CustomerOrderNumberSequenceRecord>(entity =>
        {
            entity.ToTable("customer_order_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_customer_order_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => e.OrganizationId)
                .HasName("pk_customer_order_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
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
                    "ck_inventory_accounts_reserved_non_negative",
                    "reserved_quantity >= 0");
                tb.HasCheckConstraint(
                    "ck_inventory_accounts_reserved_not_over_on_hand",
                    "reserved_quantity <= on_hand_quantity");
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
            entity.Property(e => e.ReservedQuantity)
                .HasColumnName("reserved_quantity")
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

        modelBuilder.Entity<InventoryLotRecord>(entity =>
        {
            entity.ToTable("inventory_lots", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_inventory_lots_on_hand_non_negative",
                    "quantity_on_hand >= 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.LotNumber)
                .HasColumnName("lot_number")
                .HasMaxLength(InventoryLot.LotNumberMaxLength);
            entity.Property(e => e.NormalizedLotNumber)
                .HasColumnName("normalized_lot_number")
                .HasMaxLength(InventoryLot.LotNumberMaxLength)
                .IsRequired()
                .HasDefaultValue(string.Empty);
            entity.Property(e => e.ExpirationDate).HasColumnName("expiration_date").IsRequired();
            entity.Property(e => e.QuantityOnHand)
                .HasColumnName("quantity_on_hand")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.ExpirationDate, e.NormalizedLotNumber })
                .IsUnique()
                .HasDatabaseName("ux_inventory_lots_identity_org")
                .HasFilter("branch_id IS NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.BranchId, e.ExpirationDate, e.NormalizedLotNumber })
                .IsUnique()
                .HasDatabaseName("ux_inventory_lots_identity_branch")
                .HasFilter("branch_id IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.ExpirationDate })
                .HasDatabaseName("ix_inventory_lots_org_product_expiry")
                .HasFilter("quantity_on_hand > 0");
            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.BranchId, e.ExpirationDate })
                .HasDatabaseName("ix_inventory_lots_org_product_branch_expiry")
                .HasFilter("quantity_on_hand > 0");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_inventory_lots_products");
        });

        modelBuilder.Entity<InventoryLotMovementRecord>(entity =>
        {
            entity.ToTable("inventory_lot_movements", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_inventory_lot_movements_quantity_effect_nonzero",
                    "quantity_effect <> 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.LotId).HasColumnName("lot_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.MovementType)
                .HasColumnName("movement_type")
                .HasMaxLength(StockMovementTypes.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.QuantityEffect)
                .HasColumnName("quantity_effect")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.SourceType)
                .HasColumnName("source_type")
                .HasMaxLength(StockMovementSourceTypes.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.SourceId).HasColumnName("source_id");
            entity.Property(e => e.StockMovementId).HasColumnName("stock_movement_id");
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by").IsRequired();

            entity.HasIndex(e => new { e.OrganizationId, e.SourceId, e.LotId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_inventory_lot_movements_source_lot")
                .HasFilter("source_id IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.LotId, e.RecordedAtUtc })
                .HasDatabaseName("ix_inventory_lot_movements_lot_recorded");

            entity.HasOne<InventoryLotRecord>()
                .WithMany()
                .HasForeignKey(e => e.LotId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_inventory_lot_movements_lots");
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
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.InventoryLotId).HasColumnName("inventory_lot_id");
            entity.Property(e => e.UnitCost)
                .HasColumnName("unit_cost")
                .HasPrecision(18, 2);

            entity.HasIndex(e => new { e.OrganizationId, e.ProductId, e.RecordedAtUtc })
                .HasDatabaseName("ix_stock_movements_org_product_recorded");

            entity.HasIndex(e => new { e.OrganizationId, e.RecordedAtUtc })
                .HasDatabaseName("ix_stock_movements_org_recorded");

            // One unique index covers SaleDeduction and SaleVoidRestoration (movement_type is part of the key).
            // EF Core snapshots only the last filtered unique on this identical column set
            // (currently inventory_transfer_source). Earlier indexes (sale/purchase/count/return/stock_use/production/waste_loss)
            // remain from prior migrations and must not be dropped.
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

            entity.HasIndex(e => new { e.OrganizationId, e.SourceId, e.ProductId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_stock_movements_stock_use_source")
                .HasFilter(
                    $"source_type = '{nameof(StockMovementSourceType.StockUse)}' AND source_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.SourceId, e.ProductId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_stock_movements_production_source")
                .HasFilter(
                    $"source_type = '{nameof(StockMovementSourceType.Production)}' AND source_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.SourceId, e.ProductId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_stock_movements_waste_loss_source")
                .HasFilter(
                    $"source_type = '{nameof(StockMovementSourceType.WasteLoss)}' AND source_id IS NOT NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.SourceId, e.ProductId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_stock_movements_inventory_transfer_source")
                .HasFilter(
                    $"source_type = '{nameof(StockMovementSourceType.InventoryTransfer)}' AND source_id IS NOT NULL AND inventory_lot_id IS NULL");

            entity.HasIndex(e => new { e.OrganizationId, e.SourceId, e.InventoryLotId, e.MovementType })
                .IsUnique()
                .HasDatabaseName("ux_stock_movements_inventory_transfer_lot")
                .HasFilter(
                    $"source_type = '{nameof(StockMovementSourceType.InventoryTransfer)}' AND source_id IS NOT NULL AND inventory_lot_id IS NOT NULL");

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

            entity.HasOne<InventoryLotRecord>()
                .WithMany()
                .HasForeignKey(e => e.InventoryLotId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_stock_movements_inventory_lots");
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
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(StockCount.TitleMaxLength).IsRequired();
            entity.Property(e => e.Notes).HasColumnName("notes").HasMaxLength(StockCount.NotesMaxLength);
            entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
            entity.Property(e => e.StartedBy).HasColumnName("started_by");
            entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
            entity.Property(e => e.CompletedBy).HasColumnName("completed_by");
            entity.Property(e => e.CancelledAtUtc).HasColumnName("cancelled_at_utc");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
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

        modelBuilder.Entity<InventoryTransferRecord>(entity =>
        {
            entity.ToTable("inventory_transfers", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_inventory_transfers_status",
                    $"status IN ({string.Join(", ", InventoryTransferStatuses.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_inventory_transfers_distinct_branches",
                    "source_branch_id <> destination_branch_id");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.TransferNumber)
                .HasColumnName("transfer_number")
                .HasMaxLength(InventoryTransferNumbers.MaxLength);
            entity.Property(e => e.SourceBranchId).HasColumnName("source_branch_id").IsRequired();
            entity.Property(e => e.DestinationBranchId).HasColumnName("destination_branch_id").IsRequired();
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(InventoryTransferStatuses.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasMaxLength(InventoryTransfer.NotesMaxLength);
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.DispatchedAtUtc).HasColumnName("dispatched_at_utc");
            entity.Property(e => e.DispatchedBy).HasColumnName("dispatched_by");
            entity.Property(e => e.ReceivedAtUtc).HasColumnName("received_at_utc");
            entity.Property(e => e.ReceivedBy).HasColumnName("received_by");
            entity.Property(e => e.CancelledAtUtc).HasColumnName("cancelled_at_utc");
            entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by");
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.TransferNumber })
                .IsUnique()
                .HasDatabaseName("ux_inventory_transfers_org_transfer_number")
                .HasFilter("transfer_number IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.SourceBranchId })
                .HasDatabaseName("ix_inventory_transfers_org_source");
            entity.HasIndex(e => new { e.OrganizationId, e.DestinationBranchId })
                .HasDatabaseName("ix_inventory_transfers_org_destination");
            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_inventory_transfers_org_status");
        });

        modelBuilder.Entity<InventoryTransferLineRecord>(entity =>
        {
            entity.ToTable("inventory_transfer_lines", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_inventory_transfer_lines_sent_positive",
                    "sent_qty > 0");
                tb.HasCheckConstraint(
                    "ck_inventory_transfer_lines_received_range",
                    "received_qty >= 0 AND received_qty <= sent_qty");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.TransferId).HasColumnName("transfer_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.NameSnapshot)
                .HasColumnName("name_snapshot")
                .HasMaxLength(InventoryTransferLine.NameSnapshotMaxLength)
                .IsRequired();
            entity.Property(e => e.UnitOfMeasure)
                .HasColumnName("unit_of_measure")
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(e => e.SentQty).HasColumnName("sent_qty").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.ReceivedQty).HasColumnName("received_qty").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.DiscrepancyReason)
                .HasColumnName("discrepancy_reason")
                .HasMaxLength(InventoryTransferDiscrepancyReasons.CodeMaxLength);
            entity.Property(e => e.DiscrepancyNote)
                .HasColumnName("discrepancy_note")
                .HasMaxLength(InventoryTransferLine.DiscrepancyNoteMaxLength);
            entity.Property(e => e.SourceLotId).HasColumnName("source_lot_id");
            entity.Property(e => e.LotNumber)
                .HasColumnName("lot_number")
                .HasMaxLength(InventoryLot.LotNumberMaxLength);
            entity.Property(e => e.ExpirationDate).HasColumnName("expiration_date");

            entity.HasIndex(e => new { e.TransferId, e.LineNumber })
                .IsUnique()
                .HasDatabaseName("ux_inventory_transfer_lines_transfer_line_number");
            entity.HasIndex(e => e.ProductId)
                .HasDatabaseName("ix_inventory_transfer_lines_product");
            entity.HasOne<InventoryLotRecord>()
                .WithMany()
                .HasForeignKey(e => e.SourceLotId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_inventory_transfer_lines_source_lots");

            entity.HasOne<InventoryTransferRecord>()
                .WithMany()
                .HasForeignKey(e => e.TransferId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_inventory_transfer_lines_transfers");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_inventory_transfer_lines_products");
        });

        modelBuilder.Entity<InventoryTransferNumberSequenceRecord>(entity =>
        {
            entity.ToTable("inventory_transfer_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_inventory_transfer_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_inventory_transfer_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date").HasColumnType("date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
        });

        modelBuilder.Entity<DirectPurchaseReceiptRecord>(entity =>
        {
            entity.ToTable("direct_purchase_receipts", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_direct_purchase_receipts_total_cost_non_negative",
                    "total_cost >= 0");
                tb.HasCheckConstraint(
                    "ck_direct_purchase_receipts_status",
                    $"status IN ({string.Join(", ", DirectPurchaseReceiptStatuses.Codes.Select(c => $"'{c}'"))})");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ReceiptNumber)
                .HasColumnName("receipt_number")
                .HasMaxLength(DirectPurchaseReceiptNumbers.MaxLength)
                .IsRequired();
            entity.Property(e => e.PurchaseDate).HasColumnName("purchase_date").HasColumnType("date").IsRequired();
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.SourceNameSnapshot)
                .HasColumnName("source_name_snapshot")
                .HasMaxLength(DirectPurchaseReceipt.SourceNameMaxLength);
            entity.Property(e => e.ReferenceNumber)
                .HasColumnName("reference_number")
                .HasMaxLength(DirectPurchaseReceipt.ReferenceNumberMaxLength);
            entity.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasMaxLength(DirectPurchaseReceipt.NotesMaxLength);
            entity.Property(e => e.TotalCost)
                .HasColumnName("total_cost")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasMaxLength(DirectPurchaseReceipt.IdempotencyKeyMaxLength);
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(DirectPurchaseReceiptStatuses.CodeMaxLength)
                .IsRequired()
                .HasDefaultValue(DirectPurchaseReceiptStatuses.ToCode(DirectPurchaseReceiptStatus.Posted));
            entity.Property(e => e.VoidedAtUtc).HasColumnName("voided_at_utc");
            entity.Property(e => e.VoidedByUserId).HasColumnName("voided_by_user_id");
            entity.Property(e => e.VoidReason)
                .HasColumnName("void_reason")
                .HasMaxLength(DirectPurchaseReceipt.VoidReasonMaxLength);

            entity.HasIndex(e => new { e.OrganizationId, e.ReceiptNumber })
                .IsUnique()
                .HasDatabaseName("ux_direct_purchase_receipts_org_receipt_number");
            entity.HasIndex(e => new { e.OrganizationId, e.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_direct_purchase_receipts_org_idempotency_key")
                .HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.PurchaseDate })
                .HasDatabaseName("ix_direct_purchase_receipts_org_purchase_date");
            entity.HasIndex(e => new { e.OrganizationId, e.SupplierId })
                .HasDatabaseName("ix_direct_purchase_receipts_org_supplier_id");
            entity.HasIndex(e => new { e.OrganizationId, e.ReferenceNumber })
                .HasDatabaseName("ix_direct_purchase_receipts_org_reference");
            entity.HasIndex(e => new { e.OrganizationId, e.CreatedAtUtc })
                .HasDatabaseName("ix_direct_purchase_receipts_org_created_at");
            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_direct_purchase_receipts_org_status");

            entity.HasOne<SupplierRecord>()
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_direct_purchase_receipts_suppliers")
                .IsRequired(false);
        });

        modelBuilder.Entity<DirectPurchaseReceiptLineRecord>(entity =>
        {
            entity.ToTable("direct_purchase_receipt_lines", tb =>
            {
                tb.HasCheckConstraint("ck_direct_purchase_receipt_lines_quantity_positive", "quantity > 0");
                tb.HasCheckConstraint("ck_direct_purchase_receipt_lines_unit_cost_positive", "unit_cost > 0");
                tb.HasCheckConstraint("ck_direct_purchase_receipt_lines_line_total_non_negative", "line_total >= 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ReceiptId).HasColumnName("receipt_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.ProductNameSnapshot)
                .HasColumnName("product_name_snapshot")
                .HasMaxLength(PurchaseOrderLine.NameSnapshotMaxLength)
                .IsRequired();
            entity.Property(e => e.SkuSnapshot)
                .HasColumnName("sku_snapshot")
                .HasMaxLength(CatalogProduct.SkuMaxLength);
            entity.Property(e => e.UnitOfMeasureSnapshot)
                .HasColumnName("unit_of_measure_snapshot")
                .HasMaxLength(UnitOfMeasures.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("quantity").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.UnitCost).HasColumnName("unit_cost").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.LineTotal).HasColumnName("line_total").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date").HasColumnType("date");
            entity.Property(e => e.LotNumber)
                .HasColumnName("lot_number")
                .HasMaxLength(DirectPurchaseReceiptLine.LotNumberMaxLength);
            entity.Property(e => e.InventoryMovementId).HasColumnName("inventory_movement_id");

            entity.HasIndex(e => new { e.ReceiptId, e.LineNumber })
                .IsUnique()
                .HasDatabaseName("ux_direct_purchase_receipt_lines_receipt_line_number");
            entity.HasIndex(e => e.InventoryMovementId)
                .IsUnique()
                .HasDatabaseName("ux_direct_purchase_receipt_lines_inventory_movement_id")
                .HasFilter("inventory_movement_id IS NOT NULL");

            entity.HasOne<DirectPurchaseReceiptRecord>()
                .WithMany()
                .HasForeignKey(e => e.ReceiptId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_direct_purchase_receipt_lines_receipts");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_direct_purchase_receipt_lines_products");
        });

        modelBuilder.Entity<DirectPurchaseReceiptNumberSequenceRecord>(entity =>
        {
            entity.ToTable("direct_purchase_receipt_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_direct_purchase_receipt_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_direct_purchase_receipt_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date").HasColumnType("date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
        });

        modelBuilder.Entity<StockUseRecord>(entity =>
        {
            entity.ToTable("stock_uses", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_stock_uses_reason",
                    $"reason IN ({string.Join(", ", StockUseReasons.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_stock_uses_status",
                    $"status IN ({string.Join(", ", StockUseStatuses.Codes.Select(c => $"'{c}'"))})");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.StockUseNumber)
                .HasColumnName("stock_use_number")
                .HasMaxLength(StockUseNumbers.MaxLength)
                .IsRequired();
            entity.Property(e => e.ReferenceNumber)
                .HasColumnName("reference_number")
                .HasMaxLength(StockUse.ReferenceNumberMaxLength);
            entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(StockUseReasons.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasMaxLength(StockUse.NotesMaxLength);
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(StockUseStatuses.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.VoidedByUserId).HasColumnName("voided_by_user_id");
            entity.Property(e => e.VoidedAtUtc).HasColumnName("voided_at_utc");
            entity.Property(e => e.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasMaxLength(StockUse.IdempotencyKeyMaxLength);

            entity.HasIndex(e => new { e.OrganizationId, e.StockUseNumber })
                .IsUnique()
                .HasDatabaseName("ux_stock_uses_org_stock_use_number");
            entity.HasIndex(e => new { e.OrganizationId, e.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_stock_uses_org_idempotency_key")
                .HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.OccurredAtUtc })
                .HasDatabaseName("ix_stock_uses_org_occurred_at");
            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_stock_uses_org_status");
            entity.HasIndex(e => new { e.OrganizationId, e.BranchId })
                .HasDatabaseName("ix_stock_uses_org_branch_id");
        });

        modelBuilder.Entity<StockUseLineRecord>(entity =>
        {
            entity.ToTable("stock_use_lines", tb =>
            {
                tb.HasCheckConstraint("ck_stock_use_lines_quantity_entered_positive", "quantity_entered > 0");
                tb.HasCheckConstraint("ck_stock_use_lines_multiplier_positive", "multiplier_to_base > 0");
                tb.HasCheckConstraint("ck_stock_use_lines_base_quantity_positive", "base_quantity > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StockUseId).HasColumnName("stock_use_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.ProductUnitId).HasColumnName("product_unit_id");
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.QuantityEntered)
                .HasColumnName("quantity_entered")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.MultiplierToBase)
                .HasColumnName("multiplier_to_base")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.BaseQuantity)
                .HasColumnName("base_quantity")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.NameSnapshot)
                .HasColumnName("name_snapshot")
                .HasMaxLength(PurchaseOrderLine.NameSnapshotMaxLength)
                .IsRequired();
            entity.Property(e => e.UnitLabelSnapshot)
                .HasColumnName("unit_label_snapshot")
                .HasMaxLength(StockUseLine.UnitLabelMaxLength)
                .IsRequired();
            entity.Property(e => e.UnitCostSnapshot)
                .HasColumnName("unit_cost_snapshot")
                .HasPrecision(18, 2);
            entity.Property(e => e.LineCostSnapshot)
                .HasColumnName("line_cost_snapshot")
                .HasPrecision(18, 2);
            entity.Property(e => e.InventoryMovementId).HasColumnName("inventory_movement_id");

            entity.HasIndex(e => new { e.StockUseId, e.LineNumber })
                .IsUnique()
                .HasDatabaseName("ux_stock_use_lines_stock_use_line_number");
            entity.HasIndex(e => e.InventoryMovementId)
                .IsUnique()
                .HasDatabaseName("ux_stock_use_lines_inventory_movement_id")
                .HasFilter("inventory_movement_id IS NOT NULL");

            entity.HasOne<StockUseRecord>()
                .WithMany()
                .HasForeignKey(e => e.StockUseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_stock_use_lines_stock_uses");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_stock_use_lines_products");
        });

        modelBuilder.Entity<StockUseNumberSequenceRecord>(entity =>
        {
            entity.ToTable("stock_use_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_stock_use_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_stock_use_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date").HasColumnType("date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
        });

        modelBuilder.Entity<ProductionDefinitionRecord>(entity =>
        {
            entity.ToTable("production_definitions", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_production_definitions_status",
                    $"status IN ({string.Join(", ", ProductionDefinitionStatuses.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint("ck_production_definitions_output_qty_positive", "output_quantity_entered > 0");
                tb.HasCheckConstraint("ck_production_definitions_output_multiplier_positive", "output_multiplier_to_base > 0");
                tb.HasCheckConstraint("ck_production_definitions_output_base_positive", "output_base_quantity > 0");
                tb.HasCheckConstraint("ck_production_definitions_revision_positive", "revision >= 1");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(ProductionDefinition.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.OutputProductId).HasColumnName("output_product_id").IsRequired();
            entity.Property(e => e.OutputProductUnitId).HasColumnName("output_product_unit_id");
            entity.Property(e => e.OutputQuantityEntered)
                .HasColumnName("output_quantity_entered")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.OutputMultiplierToBase)
                .HasColumnName("output_multiplier_to_base")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.OutputBaseQuantity)
                .HasColumnName("output_base_quantity")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(ProductionDefinitionStatuses.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.Revision).HasColumnName("revision").IsRequired();
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedByUserId).HasColumnName("updated_by_user_id");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasIndex(e => new { e.OrganizationId, e.Name })
                .HasDatabaseName("ix_production_definitions_org_name");
            entity.HasIndex(e => new { e.OrganizationId, e.OutputProductId })
                .HasDatabaseName("ix_production_definitions_org_output");
            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_production_definitions_org_status");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.OutputProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_production_definitions_output_products");
        });

        modelBuilder.Entity<ProductionComponentRecord>(entity =>
        {
            entity.ToTable("production_components", tb =>
            {
                tb.HasCheckConstraint("ck_production_components_quantity_positive", "quantity_entered > 0");
                tb.HasCheckConstraint("ck_production_components_multiplier_positive", "multiplier_to_base > 0");
                tb.HasCheckConstraint("ck_production_components_base_positive", "base_quantity > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductionDefinitionId).HasColumnName("production_definition_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.MaterialProductId).HasColumnName("material_product_id").IsRequired();
            entity.Property(e => e.ProductUnitId).HasColumnName("product_unit_id");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order").IsRequired();
            entity.Property(e => e.QuantityEntered)
                .HasColumnName("quantity_entered")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.MultiplierToBase)
                .HasColumnName("multiplier_to_base")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.BaseQuantity)
                .HasColumnName("base_quantity")
                .HasPrecision(18, 3)
                .IsRequired();

            entity.HasIndex(e => new { e.ProductionDefinitionId, e.SortOrder })
                .IsUnique()
                .HasDatabaseName("ux_production_components_definition_sort");
            entity.HasIndex(e => new { e.ProductionDefinitionId, e.MaterialProductId })
                .IsUnique()
                .HasDatabaseName("ux_production_components_definition_material");

            entity.HasOne<ProductionDefinitionRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductionDefinitionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_production_components_definitions");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.MaterialProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_production_components_materials");
        });

        modelBuilder.Entity<ProductionRunRecord>(entity =>
        {
            entity.ToTable("production_runs", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_production_runs_status",
                    $"status IN ({string.Join(", ", ProductionRunStatuses.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_production_runs_cost_status",
                    $"cost_status IN ({string.Join(", ", ProductionCostStatuses.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint("ck_production_runs_output_qty_positive", "output_quantity_entered > 0");
                tb.HasCheckConstraint("ck_production_runs_output_multiplier_positive", "output_multiplier_to_base > 0");
                tb.HasCheckConstraint("ck_production_runs_output_base_positive", "output_base_quantity > 0");
                tb.HasCheckConstraint("ck_production_runs_revision_positive", "production_definition_revision >= 1");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ProductionNumber)
                .HasColumnName("production_number")
                .HasMaxLength(ProductionNumbers.MaxLength)
                .IsRequired();
            entity.Property(e => e.ReferenceNumber)
                .HasColumnName("reference_number")
                .HasMaxLength(ProductionRun.ReferenceNumberMaxLength);
            entity.Property(e => e.ProductionDefinitionId).HasColumnName("production_definition_id").IsRequired();
            entity.Property(e => e.ProductionDefinitionRevision)
                .HasColumnName("production_definition_revision")
                .IsRequired();
            entity.Property(e => e.ProductionDefinitionNameSnapshot)
                .HasColumnName("production_definition_name_snapshot")
                .HasMaxLength(ProductionDefinition.NameMaxLength)
                .IsRequired();
            entity.Property(e => e.OutputProductId).HasColumnName("output_product_id").IsRequired();
            entity.Property(e => e.OutputProductUnitId).HasColumnName("output_product_unit_id");
            entity.Property(e => e.OutputQuantityEntered)
                .HasColumnName("output_quantity_entered")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.OutputMultiplierToBase)
                .HasColumnName("output_multiplier_to_base")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.OutputBaseQuantity)
                .HasColumnName("output_base_quantity")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.OutputNameSnapshot)
                .HasColumnName("output_name_snapshot")
                .HasMaxLength(PurchaseOrderLine.NameSnapshotMaxLength)
                .IsRequired();
            entity.Property(e => e.OutputUnitLabelSnapshot)
                .HasColumnName("output_unit_label_snapshot")
                .HasMaxLength(ProductionRunMaterial.UnitLabelMaxLength)
                .IsRequired();
            entity.Property(e => e.ProducedAtUtc).HasColumnName("produced_at_utc");
            entity.Property(e => e.OutputExpirationDate)
                .HasColumnName("output_expiration_date")
                .HasColumnType("date");
            entity.Property(e => e.OutputLotNumber)
                .HasColumnName("output_lot_number")
                .HasMaxLength(64);
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(ProductionRunStatuses.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.CostStatus)
                .HasColumnName("cost_status")
                .HasMaxLength(ProductionCostStatuses.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.TotalMaterialCost)
                .HasColumnName("total_material_cost")
                .HasPrecision(18, 2);
            entity.Property(e => e.OutputBaseUnitCost)
                .HasColumnName("output_base_unit_cost")
                .HasPrecision(18, 2);
            entity.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasMaxLength(ProductionRun.NotesMaxLength);
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.VoidedByUserId).HasColumnName("voided_by_user_id");
            entity.Property(e => e.VoidedAtUtc).HasColumnName("voided_at_utc");
            entity.Property(e => e.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasMaxLength(ProductionRun.IdempotencyKeyMaxLength);
            entity.Property(e => e.OutputInventoryMovementId).HasColumnName("output_inventory_movement_id");

            entity.HasIndex(e => new { e.OrganizationId, e.ProductionNumber })
                .IsUnique()
                .HasDatabaseName("ux_production_runs_org_production_number");
            entity.HasIndex(e => new { e.OrganizationId, e.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_production_runs_org_idempotency_key")
                .HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.ProducedAtUtc })
                .HasDatabaseName("ix_production_runs_org_produced_at");
            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_production_runs_org_status");
            entity.HasIndex(e => new { e.OrganizationId, e.OutputProductId })
                .HasDatabaseName("ix_production_runs_org_output");
            entity.HasIndex(e => new { e.OrganizationId, e.ProductionDefinitionId })
                .HasDatabaseName("ix_production_runs_org_definition");
            entity.HasIndex(e => e.OutputInventoryMovementId)
                .IsUnique()
                .HasDatabaseName("ux_production_runs_output_inventory_movement_id")
                .HasFilter("output_inventory_movement_id IS NOT NULL");

            entity.HasOne<ProductionDefinitionRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductionDefinitionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_production_runs_definitions");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.OutputProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_production_runs_output_products");
        });

        modelBuilder.Entity<ProductionRunMaterialRecord>(entity =>
        {
            entity.ToTable("production_run_materials", tb =>
            {
                tb.HasCheckConstraint("ck_production_run_materials_expected_non_negative", "expected_quantity_entered >= 0");
                tb.HasCheckConstraint("ck_production_run_materials_actual_positive", "actual_quantity_entered > 0");
                tb.HasCheckConstraint("ck_production_run_materials_multiplier_positive", "multiplier_to_base > 0");
                tb.HasCheckConstraint("ck_production_run_materials_expected_base_non_negative", "expected_base_quantity >= 0");
                tb.HasCheckConstraint("ck_production_run_materials_actual_base_positive", "actual_base_quantity > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductionRunId).HasColumnName("production_run_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.MaterialProductId).HasColumnName("material_product_id").IsRequired();
            entity.Property(e => e.ProductUnitId).HasColumnName("product_unit_id");
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.ExpectedQuantityEntered)
                .HasColumnName("expected_quantity_entered")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.ActualQuantityEntered)
                .HasColumnName("actual_quantity_entered")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.MultiplierToBase)
                .HasColumnName("multiplier_to_base")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.ExpectedBaseQuantity)
                .HasColumnName("expected_base_quantity")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.ActualBaseQuantity)
                .HasColumnName("actual_base_quantity")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.NameSnapshot)
                .HasColumnName("name_snapshot")
                .HasMaxLength(PurchaseOrderLine.NameSnapshotMaxLength)
                .IsRequired();
            entity.Property(e => e.UnitLabelSnapshot)
                .HasColumnName("unit_label_snapshot")
                .HasMaxLength(ProductionRunMaterial.UnitLabelMaxLength)
                .IsRequired();
            entity.Property(e => e.UnitCostSnapshot)
                .HasColumnName("unit_cost_snapshot")
                .HasPrecision(18, 2);
            entity.Property(e => e.LineCostSnapshot)
                .HasColumnName("line_cost_snapshot")
                .HasPrecision(18, 2);
            entity.Property(e => e.InventoryMovementId).HasColumnName("inventory_movement_id");

            entity.HasIndex(e => new { e.ProductionRunId, e.LineNumber })
                .IsUnique()
                .HasDatabaseName("ux_production_run_materials_run_line");
            entity.HasIndex(e => e.InventoryMovementId)
                .IsUnique()
                .HasDatabaseName("ux_production_run_materials_inventory_movement_id")
                .HasFilter("inventory_movement_id IS NOT NULL");

            entity.HasOne<ProductionRunRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductionRunId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_production_run_materials_runs");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.MaterialProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_production_run_materials_products");
        });

        modelBuilder.Entity<ProductionRunNumberSequenceRecord>(entity =>
        {
            entity.ToTable("production_run_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_production_run_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_production_run_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date").HasColumnType("date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
        });

        modelBuilder.Entity<WasteLossRecord>(entity =>
        {
            entity.ToTable("waste_losses", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_waste_losses_reason",
                    $"reason IN ({string.Join(", ", WasteLossReasons.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_waste_losses_status",
                    $"status IN ({string.Join(", ", WasteLossStatuses.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_waste_losses_cost_status",
                    $"cost_status IN ({string.Join(", ", ProductionCostStatuses.Codes.Select(c => $"'{c}'"))})");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.WasteLossNumber)
                .HasColumnName("waste_loss_number")
                .HasMaxLength(WasteLossNumbers.MaxLength)
                .IsRequired();
            entity.Property(e => e.ReferenceNumber)
                .HasColumnName("reference_number")
                .HasMaxLength(WasteLoss.ReferenceNumberMaxLength);
            entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(e => e.Reason)
                .HasColumnName("reason")
                .HasMaxLength(WasteLossReasons.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasMaxLength(WasteLoss.NotesMaxLength);
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(WasteLossStatuses.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.CostStatus)
                .HasColumnName("cost_status")
                .HasMaxLength(ProductionCostStatuses.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.TotalCostSnapshot)
                .HasColumnName("total_cost_snapshot")
                .HasPrecision(18, 2);
            entity.Property(e => e.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.VoidedByUserId).HasColumnName("voided_by_user_id");
            entity.Property(e => e.VoidedAtUtc).HasColumnName("voided_at_utc");
            entity.Property(e => e.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasMaxLength(WasteLoss.IdempotencyKeyMaxLength);

            entity.HasIndex(e => new { e.OrganizationId, e.WasteLossNumber })
                .IsUnique()
                .HasDatabaseName("ux_waste_losses_org_waste_loss_number");
            entity.HasIndex(e => new { e.OrganizationId, e.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_waste_losses_org_idempotency_key")
                .HasFilter("idempotency_key IS NOT NULL");
            entity.HasIndex(e => new { e.OrganizationId, e.OccurredAtUtc })
                .HasDatabaseName("ix_waste_losses_org_occurred_at");
            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_waste_losses_org_status");
            entity.HasIndex(e => new { e.OrganizationId, e.BranchId })
                .HasDatabaseName("ix_waste_losses_org_branch_id");
        });

        modelBuilder.Entity<WasteLossLineRecord>(entity =>
        {
            entity.ToTable("waste_loss_lines", tb =>
            {
                tb.HasCheckConstraint("ck_waste_loss_lines_quantity_entered_positive", "quantity_entered > 0");
                tb.HasCheckConstraint("ck_waste_loss_lines_multiplier_positive", "multiplier_to_base > 0");
                tb.HasCheckConstraint("ck_waste_loss_lines_base_quantity_positive", "base_quantity > 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.WasteLossId).HasColumnName("waste_loss_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.ProductUnitId).HasColumnName("product_unit_id");
            entity.Property(e => e.InventoryLotId).HasColumnName("inventory_lot_id");
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.QuantityEntered)
                .HasColumnName("quantity_entered")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.MultiplierToBase)
                .HasColumnName("multiplier_to_base")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.BaseQuantity)
                .HasColumnName("base_quantity")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.NameSnapshot)
                .HasColumnName("name_snapshot")
                .HasMaxLength(PurchaseOrderLine.NameSnapshotMaxLength)
                .IsRequired();
            entity.Property(e => e.UnitLabelSnapshot)
                .HasColumnName("unit_label_snapshot")
                .HasMaxLength(WasteLossLine.UnitLabelMaxLength)
                .IsRequired();
            entity.Property(e => e.UnitCostSnapshot)
                .HasColumnName("unit_cost_snapshot")
                .HasPrecision(18, 2);
            entity.Property(e => e.LineCostSnapshot)
                .HasColumnName("line_cost_snapshot")
                .HasPrecision(18, 2);
            entity.Property(e => e.InventoryMovementId).HasColumnName("inventory_movement_id");

            entity.HasIndex(e => new { e.WasteLossId, e.LineNumber })
                .IsUnique()
                .HasDatabaseName("ux_waste_loss_lines_waste_loss_line_number");
            entity.HasIndex(e => e.InventoryMovementId)
                .IsUnique()
                .HasDatabaseName("ux_waste_loss_lines_inventory_movement_id")
                .HasFilter("inventory_movement_id IS NOT NULL");

            entity.HasOne<WasteLossRecord>()
                .WithMany()
                .HasForeignKey(e => e.WasteLossId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_waste_loss_lines_waste_losses");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_waste_loss_lines_products");
        });

        modelBuilder.Entity<WasteLossNumberSequenceRecord>(entity =>
        {
            entity.ToTable("waste_loss_number_sequences", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_waste_loss_number_sequences_last_value_positive",
                    "last_value > 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BusinessDate })
                .HasName("pk_waste_loss_number_sequences");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BusinessDate).HasColumnName("business_date").HasColumnType("date");
            entity.Property(e => e.LastValue).HasColumnName("last_value").IsRequired();
        });

        modelBuilder.Entity<InventoryBranchBalanceRecord>(entity =>
        {
            entity.ToTable("inventory_branch_balances", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_inventory_branch_balances_on_hand_non_negative",
                    "on_hand_quantity >= 0");
            });

            entity.HasKey(e => new { e.OrganizationId, e.BranchId, e.ProductId })
                .HasName("pk_inventory_branch_balances");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.BranchId).HasColumnName("branch_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.OnHandQuantity)
                .HasColumnName("on_hand_quantity")
                .HasPrecision(18, 3)
                .IsRequired();
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasOne<CatalogProductRecord>()
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_inventory_branch_balances_products");
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

        modelBuilder.Entity<SupplierPayableRecord>(entity =>
        {
            entity.ToTable("supplier_payables", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_supplier_payables_status",
                    "status IN ('Open', 'PartiallyPaid', 'Paid', 'Voided')");
                tb.HasCheckConstraint(
                    "ck_supplier_payables_source_type",
                    $"source_type IN ({string.Join(", ", SupplierPayableSourceTypes.Codes.Select(c => $"'{c}'"))})");
                tb.HasCheckConstraint(
                    "ck_supplier_payables_amounts_non_negative",
                    "original_amount > 0 AND paid_at_receipt_amount >= 0 AND paid_amount >= 0 AND balance >= 0");
                tb.HasCheckConstraint(
                    "ck_supplier_payables_balance_identity",
                    "balance = original_amount - paid_amount");
                tb.HasCheckConstraint(
                    "ck_supplier_payables_paid_at_receipt_le_original",
                    "paid_at_receipt_amount <= original_amount");
                tb.HasCheckConstraint(
                    "ck_supplier_payables_void_consistency",
                    "(status <> 'Voided' AND voided_at_utc IS NULL AND voided_by IS NULL AND void_reason IS NULL) OR (status = 'Voided' AND voided_at_utc IS NOT NULL AND voided_by IS NOT NULL AND void_reason IS NOT NULL)");
                tb.HasCheckConstraint(
                    "ck_supplier_payables_payment_method_at_receipt",
                    $"payment_method_at_receipt IS NULL OR payment_method_at_receipt IN ({string.Join(", ", SupplierPayablePaymentMethods.Codes.Select(c => $"'{c}'"))})");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id").IsRequired();
            entity.Property(e => e.SourceType)
                .HasColumnName("source_type")
                .HasMaxLength(SupplierPayableSourceTypes.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.SourceId).HasColumnName("source_id").IsRequired();
            entity.Property(e => e.OriginalAmount).HasColumnName("original_amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.PaidAtReceiptAmount)
                .HasColumnName("paid_at_receipt_amount")
                .HasPrecision(18, 2)
                .IsRequired();
            entity.Property(e => e.PaidAmount).HasColumnName("paid_amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Balance).HasColumnName("balance").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
            entity.Property(e => e.DueDate).HasColumnName("due_date").HasColumnType("date");
            entity.Property(e => e.PaymentMethodAtReceipt)
                .HasColumnName("payment_method_at_receipt")
                .HasMaxLength(SupplierPayablePaymentMethods.CodeMaxLength);
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by").IsRequired();
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(e => e.VoidedAtUtc).HasColumnName("voided_at_utc");
            entity.Property(e => e.VoidedBy).HasColumnName("voided_by");
            entity.Property(e => e.VoidReason)
                .HasColumnName("void_reason")
                .HasMaxLength(SupplierPayable.VoidReasonMaxLength);
            entity.Property(e => e.Xmin)
                .HasColumnName("xmin")
                .HasColumnType("xid")
                .ValueGeneratedOnAddOrUpdate()
                .IsConcurrencyToken();

            entity.HasIndex(e => new { e.OrganizationId, e.SourceType, e.SourceId })
                .IsUnique()
                .HasDatabaseName("ux_supplier_payables_org_source");

            entity.HasIndex(e => new { e.OrganizationId, e.SupplierId, e.Status })
                .HasDatabaseName("ix_supplier_payables_org_supplier_status");

            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_supplier_payables_org_status");

            entity.HasIndex(e => new { e.OrganizationId, e.DueDate })
                .HasDatabaseName("ix_supplier_payables_org_due_date");

            entity.HasOne<SupplierRecord>()
                .WithMany()
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_supplier_payables_suppliers");
        });

        modelBuilder.Entity<SupplierPayablePaymentRecord>(entity =>
        {
            entity.ToTable("supplier_payable_payments", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_supplier_payable_payments_amount_positive",
                    "amount > 0");
                tb.HasCheckConstraint(
                    "ck_supplier_payable_payments_payment_method",
                    $"payment_method IN ({string.Join(", ", SupplierPayablePaymentMethods.Codes.Select(c => $"'{c}'"))})");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.PayableId).HasColumnName("payable_id").IsRequired();
            entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.PaymentMethod)
                .HasColumnName("payment_method")
                .HasMaxLength(SupplierPayablePaymentMethods.CodeMaxLength)
                .IsRequired();
            entity.Property(e => e.Reference)
                .HasColumnName("reference")
                .HasMaxLength(SupplierPayablePayment.ReferenceMaxLength);
            entity.Property(e => e.Notes)
                .HasColumnName("notes")
                .HasMaxLength(SupplierPayablePayment.NotesMaxLength);
            entity.Property(e => e.PaidAtUtc).HasColumnName("paid_at_utc");
            entity.Property(e => e.RecordedBy).HasColumnName("recorded_by").IsRequired();
            entity.Property(e => e.RecordedAtUtc).HasColumnName("recorded_at_utc");

            entity.HasIndex(e => new { e.OrganizationId, e.PayableId })
                .HasDatabaseName("ix_supplier_payable_payments_org_payable");

            entity.HasOne<SupplierPayableRecord>()
                .WithMany()
                .HasForeignKey(e => e.PayableId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_supplier_payable_payments_payables");
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
            entity.Property(e => e.ConnectionType).HasColumnName("connection_type").HasDefaultValue(0).IsRequired();
            entity.Property(e => e.ConnectedRelationshipId).HasColumnName("connected_relationship_id");
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
                tb.HasCheckConstraint("ck_purchase_orders_payment_term", "payment_term BETWEEN 0 AND 2");
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
            entity.Property(e => e.PaymentTerm).HasColumnName("payment_term").IsRequired().HasDefaultValue(0);
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
                tb.HasCheckConstraint("ck_purchase_order_lines_closed_short_qty_nonnegative", "closed_short_qty >= 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id").IsRequired();
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.SupplierProductId).HasColumnName("supplier_product_id");
            entity.Property(e => e.LineNumber).HasColumnName("line_number").IsRequired();
            entity.Property(e => e.NameSnapshot)
                .HasColumnName("name_snapshot")
                .HasMaxLength(PurchaseOrderLine.NameSnapshotMaxLength);
            entity.Property(e => e.UomSnapshot).HasColumnName("uom_snapshot").HasMaxLength(UnitOfMeasures.CodeMaxLength);
            entity.Property(e => e.SkuSnapshot)
                .HasColumnName("sku_snapshot")
                .HasMaxLength(PurchaseOrderLine.NameSnapshotMaxLength);
            entity.Property(e => e.OrderedQty).HasColumnName("ordered_qty").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.UnitPurchaseCost).HasColumnName("unit_purchase_cost").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.LineTotal).HasColumnName("line_total").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.ReceivedQty).HasColumnName("received_qty").HasPrecision(18, 3).IsRequired();
            entity.Property(e => e.ClosedShortQty)
                .HasColumnName("closed_short_qty")
                .HasPrecision(18, 3)
                .IsRequired()
                .HasDefaultValue(0m);
            entity.Property(e => e.LineNotes).HasColumnName("line_notes").HasMaxLength(PurchaseOrderLine.LineNotesMaxLength);
            entity.Property(e => e.PurchaseUnitId).HasColumnName("purchase_unit_id");
            entity.Property(e => e.PurchaseUnitNameSnapshot)
                .HasColumnName("purchase_unit_name_snapshot")
                .HasMaxLength(CatalogProductUnit.DisplayNameMaxLength);
            entity.Property(e => e.MultiplierToBaseSnapshot)
                .HasColumnName("multiplier_to_base_snapshot")
                .HasPrecision(18, 3)
                .IsRequired()
                .HasDefaultValue(1m);

            entity.HasIndex(e => new { e.PurchaseOrderId, e.LineNumber })
                .IsUnique()
                .HasDatabaseName("ux_purchase_order_lines_po_line_number");

            entity.HasIndex(e => new { e.PurchaseOrderId, e.ProductId })
                .IsUnique()
                .HasFilter("product_id IS NOT NULL")
                .HasDatabaseName("ux_purchase_order_lines_po_product");

            entity.HasIndex(e => new { e.PurchaseOrderId, e.SupplierProductId })
                .IsUnique()
                .HasFilter("supplier_product_id IS NOT NULL")
                .HasDatabaseName("ux_purchase_order_lines_po_supplier_product");

            entity.HasOne<PurchaseOrderRecord>()
                .WithMany()
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_purchase_order_lines_purchase_orders");
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
            entity.ToTable("goods_receipts", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_goods_receipts_status",
                    $"status IN ({string.Join(", ", GoodsReceiptStatuses.Codes.Select(c => $"'{c}'"))})");
            });

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
            entity.Property(e => e.Status)
                .HasColumnName("status")
                .HasMaxLength(GoodsReceiptStatuses.CodeMaxLength)
                .IsRequired()
                .HasDefaultValue(GoodsReceiptStatuses.ToCode(GoodsReceiptStatus.Posted));
            entity.Property(e => e.VoidedAtUtc).HasColumnName("voided_at_utc");
            entity.Property(e => e.VoidedByUserId).HasColumnName("voided_by_user_id");
            entity.Property(e => e.VoidReason)
                .HasColumnName("void_reason")
                .HasMaxLength(GoodsReceipt.VoidReasonMaxLength);

            entity.HasIndex(e => new { e.OrganizationId, e.GrnNumber })
                .IsUnique()
                .HasDatabaseName("ux_goods_receipts_org_grn_number");

            entity.HasIndex(e => new { e.OrganizationId, e.PurchaseOrderId })
                .HasDatabaseName("ix_goods_receipts_org_po");

            entity.HasIndex(e => new { e.OrganizationId, e.Status })
                .HasDatabaseName("ix_goods_receipts_org_status");

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
                tb.HasCheckConstraint("ck_goods_receipt_lines_received_qty_nonnegative", "received_qty >= 0");
                tb.HasCheckConstraint("ck_goods_receipt_lines_damaged_qty_nonnegative", "damaged_qty >= 0");
                tb.HasCheckConstraint("ck_goods_receipt_lines_rejected_qty_nonnegative", "rejected_qty >= 0");
                tb.HasCheckConstraint("ck_goods_receipt_lines_short_closed_qty_nonnegative", "short_closed_qty >= 0");
                tb.HasCheckConstraint(
                    "ck_goods_receipt_lines_activity_positive",
                    "(received_qty + damaged_qty + rejected_qty + short_closed_qty) > 0");
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
            entity.Property(e => e.DamagedQty).HasColumnName("damaged_qty").HasPrecision(18, 3).IsRequired().HasDefaultValue(0m);
            entity.Property(e => e.RejectedQty).HasColumnName("rejected_qty").HasPrecision(18, 3).IsRequired().HasDefaultValue(0m);
            entity.Property(e => e.ShortClosedQty).HasColumnName("short_closed_qty").HasPrecision(18, 3).IsRequired().HasDefaultValue(0m);
            entity.Property(e => e.DiscrepancyKind).HasColumnName("discrepancy_kind").HasMaxLength(32).IsRequired().HasDefaultValue("None");
            entity.Property(e => e.DiscrepancyNote).HasColumnName("discrepancy_note").HasMaxLength(280);
            entity.Property(e => e.UnitPurchaseCostSnapshot).HasColumnName("unit_purchase_cost_snapshot").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.LineTotalSnapshot).HasColumnName("line_total_snapshot").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.InventoryMovementId).HasColumnName("inventory_movement_id");
            entity.Property(e => e.PurchaseUnitId).HasColumnName("purchase_unit_id");
            entity.Property(e => e.PurchaseUnitNameSnapshot)
                .HasColumnName("purchase_unit_name_snapshot")
                .HasMaxLength(CatalogProductUnit.DisplayNameMaxLength);
            entity.Property(e => e.MultiplierToBaseSnapshot)
                .HasColumnName("multiplier_to_base_snapshot")
                .HasPrecision(18, 3)
                .IsRequired()
                .HasDefaultValue(1m);
            entity.Property(e => e.ExpiryDate).HasColumnName("expiry_date");
            entity.Property(e => e.LotNumber)
                .HasColumnName("lot_number")
                .HasMaxLength(InventoryLot.LotNumberMaxLength);

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
                    "(status = 'Open' AND closing_cash_amount IS NULL AND expected_cash_amount_snapshot IS NULL AND cash_variance_amount IS NULL AND closed_at_utc IS NULL AND closed_by IS NULL AND cancelled_at_utc IS NULL AND cancelled_by IS NULL) OR (status = 'Closed' AND expected_cash_amount_snapshot IS NOT NULL AND closed_at_utc IS NOT NULL AND closed_by IS NOT NULL AND cancelled_at_utc IS NULL AND cancelled_by IS NULL AND ((closing_cash_amount IS NOT NULL AND cash_variance_amount IS NOT NULL) OR (closing_cash_amount IS NULL AND cash_variance_amount IS NULL))) OR (status = 'Cancelled' AND closing_cash_amount IS NULL AND expected_cash_amount_snapshot IS NULL AND cash_variance_amount IS NULL AND closed_at_utc IS NULL AND closed_by IS NULL AND cancelled_at_utc IS NOT NULL AND cancelled_by IS NOT NULL)");
                tb.HasCheckConstraint(
                    "ck_cashier_shifts_cash_count_mode",
                    "effective_cash_count_mode IN ('Off', 'Optional', 'Required')");
                tb.HasCheckConstraint(
                    "ck_cashier_shifts_opening_cash_count_mode",
                    "effective_opening_cash_count_mode IN ('Off', 'Optional', 'Required')");
                tb.HasCheckConstraint(
                    "ck_cashier_shifts_closing_cash_count_mode",
                    "effective_closing_cash_count_mode IN ('Off', 'Optional', 'Required')");
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
            entity.Property(e => e.EffectiveCashCountMode)
                .HasColumnName("effective_cash_count_mode")
                .HasMaxLength(16)
                .IsRequired()
                .HasDefaultValue("Optional");
            entity.Property(e => e.EffectiveOpeningCashCountMode)
                .HasColumnName("effective_opening_cash_count_mode")
                .HasMaxLength(16)
                .IsRequired()
                .HasDefaultValue("Optional");
            entity.Property(e => e.EffectiveClosingCashCountMode)
                .HasColumnName("effective_closing_cash_count_mode")
                .HasMaxLength(16)
                .IsRequired()
                .HasDefaultValue("Optional");
            entity.Property(e => e.OpeningCashCounted)
                .HasColumnName("opening_cash_counted")
                .IsRequired()
                .HasDefaultValue(true);
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
                tb.HasCheckConstraint(
                    "ck_operational_setups_cash_count_mode",
                    "cash_count_mode IN ('Optional', 'Required')");
                tb.HasCheckConstraint(
                    "ck_operational_setups_opening_cash_count_mode",
                    "opening_cash_count_mode IN ('Optional', 'Required')");
                tb.HasCheckConstraint(
                    "ck_operational_setups_closing_cash_count_mode",
                    "closing_cash_count_mode IN ('Optional', 'Required')");
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
            entity.Property(e => e.CashCountMode)
                .HasColumnName("cash_count_mode")
                .HasMaxLength(16)
                .IsRequired()
                .HasDefaultValue("Optional");
            entity.Property(e => e.OpeningCashCountMode)
                .HasColumnName("opening_cash_count_mode")
                .HasMaxLength(16)
                .IsRequired()
                .HasDefaultValue("Optional");
            entity.Property(e => e.ClosingCashCountMode)
                .HasColumnName("closing_cash_count_mode")
                .HasMaxLength(16)
                .IsRequired()
                .HasDefaultValue("Optional");
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

        modelBuilder.Entity<OrganizationOnboardingProgressRecord>(entity =>
        {
            entity.ToTable("organization_onboarding_progress", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_organization_onboarding_progress_organization_setup_status",
                    "organization_setup_status IN ('NotStarted', 'Completed', 'Skipped')");
                tb.HasCheckConstraint(
                    "ck_organization_onboarding_progress_business_setup_status",
                    "business_setup_status IN ('NotStarted', 'Completed', 'Skipped')");
                tb.HasCheckConstraint(
                    "ck_organization_onboarding_progress_product_template_status",
                    "product_template_status IN ('NotStarted', 'Completed', 'Skipped')");
                tb.HasCheckConstraint(
                    "ck_organization_onboarding_progress_overall_status",
                    "overall_status IN ('InProgress', 'Completed', 'FinishedLater')");
            });

            entity.HasKey(e => e.OrganizationId);
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id");
            entity.Property(e => e.OrganizationSetupStatus)
                .HasColumnName("organization_setup_status")
                .HasMaxLength(OrganizationOnboardingProgress.StatusMaxLength)
                .IsRequired();
            entity.Property(e => e.BusinessSetupStatus)
                .HasColumnName("business_setup_status")
                .HasMaxLength(OrganizationOnboardingProgress.StatusMaxLength)
                .IsRequired();
            entity.Property(e => e.ProductTemplateStatus)
                .HasColumnName("product_template_status")
                .HasMaxLength(OrganizationOnboardingProgress.StatusMaxLength)
                .IsRequired();
            entity.Property(e => e.OverallStatus)
                .HasColumnName("overall_status")
                .HasMaxLength(OrganizationOnboardingProgress.StatusMaxLength)
                .IsRequired();
            entity.Property(e => e.PrimaryBusinessTypeId).HasColumnName("primary_business_type_id");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        });

        modelBuilder.Entity<OrganizationCashDenominationRecord>(entity =>
        {
            entity.ToTable("organization_cash_denominations", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_organization_cash_denominations_value_positive",
                    "value > 0");
                tb.HasCheckConstraint(
                    "ck_organization_cash_denominations_sort_order_non_negative",
                    "sort_order >= 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.Value).HasColumnName("value").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.DisplayLabel)
                .HasColumnName("display_label")
                .HasMaxLength(OrganizationCashDenomination.DisplayLabelMaxLength);
            entity.Property(e => e.IsEnabled).HasColumnName("is_enabled").IsRequired();
            entity.Property(e => e.SortOrder).HasColumnName("sort_order").IsRequired();
            entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");

            entity.HasIndex(e => new { e.OrganizationId, e.Value })
                .IsUnique()
                .HasDatabaseName("ux_organization_cash_denominations_org_value");

            entity.HasIndex(e => new { e.OrganizationId, e.SortOrder })
                .HasDatabaseName("ix_organization_cash_denominations_org_sort");
        });

        modelBuilder.Entity<CashierShiftCashCountLineRecord>(entity =>
        {
            entity.ToTable("cashier_shift_cash_count_lines", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_cashier_shift_cash_count_lines_kind",
                    "count_kind IN ('Opening', 'Closing')");
                tb.HasCheckConstraint(
                    "ck_cashier_shift_cash_count_lines_value_positive",
                    "denomination_value > 0");
                tb.HasCheckConstraint(
                    "ck_cashier_shift_cash_count_lines_quantity_non_negative",
                    "quantity >= 0");
                tb.HasCheckConstraint(
                    "ck_cashier_shift_cash_count_lines_line_total_non_negative",
                    "line_total >= 0");
            });

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrganizationId).HasColumnName("organization_id").IsRequired();
            entity.Property(e => e.ShiftId).HasColumnName("shift_id").IsRequired();
            entity.Property(e => e.CountKind).HasColumnName("count_kind").HasMaxLength(16).IsRequired();
            entity.Property(e => e.DenominationValue).HasColumnName("denomination_value").HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("quantity").IsRequired();
            entity.Property(e => e.LineTotal).HasColumnName("line_total").HasPrecision(18, 2).IsRequired();

            entity.HasIndex(e => new { e.ShiftId, e.CountKind, e.DenominationValue })
                .IsUnique()
                .HasDatabaseName("ux_cashier_shift_cash_count_lines_shift_kind_value");

            entity.HasIndex(e => new { e.OrganizationId, e.ShiftId, e.CountKind })
                .HasDatabaseName("ix_cashier_shift_cash_count_lines_org_shift_kind");

            entity.HasOne<CashierShiftRecord>()
                .WithMany()
                .HasForeignKey(e => e.ShiftId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_cashier_shift_cash_count_lines_shifts");
        });

        modelBuilder.Entity<ConnectedSupplierRelationshipRecord>(entity =>
        {
            entity.ToTable("connected_supplier_relationships", tb =>
            {
                tb.HasCheckConstraint("ck_connected_supplier_relationships_status", "status BETWEEN 0 AND 3");
                tb.HasCheckConstraint("ck_connected_supplier_relationships_catalog_sharing_mode", "catalog_sharing_mode BETWEEN 0 AND 1");
            });
            entity.HasKey(x=>x.Id); entity.Property(x=>x.Id).HasColumnName("id");
            entity.Property(x=>x.BuyerOrganizationId).HasColumnName("buyer_organization_id");
            entity.Property(x=>x.SupplierOrganizationId).HasColumnName("supplier_organization_id");
            entity.Property(x=>x.Status).HasColumnName("status"); entity.Property(x=>x.RequestedAtUtc).HasColumnName("requested_at_utc");
            entity.Property(x=>x.RequestedByUserId).HasColumnName("requested_by_user_id"); entity.Property(x=>x.RespondedAtUtc).HasColumnName("responded_at_utc");
            entity.Property(x=>x.RespondedByUserId).HasColumnName("responded_by_user_id"); entity.Property(x=>x.DisconnectedAtUtc).HasColumnName("disconnected_at_utc");
            entity.Property(x=>x.BuyerDisplayNameSnapshot).HasColumnName("buyer_display_name_snapshot").HasMaxLength(128);
            entity.Property(x=>x.BuyerPublicOrganizationIdSnapshot).HasColumnName("buyer_public_organization_id_snapshot").HasMaxLength(32);
            entity.Property(x=>x.SupplierDisplayNameSnapshot).HasColumnName("supplier_display_name_snapshot").HasMaxLength(128);
            entity.Property(x=>x.SupplierPublicOrganizationIdSnapshot).HasColumnName("supplier_public_organization_id_snapshot").HasMaxLength(32);
            entity.Property(x=>x.CatalogSharingMode).HasColumnName("catalog_sharing_mode").HasDefaultValue(0);
            entity.Property(x=>x.CustomerDiscountPercent).HasColumnName("customer_discount_percent").HasPrecision(5, 2);
            entity.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc"); entity.Property(x=>x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(x=>x.Xmin).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
            entity.HasIndex(x=>new{x.BuyerOrganizationId,x.SupplierOrganizationId}).IsUnique().HasFilter("status IN (0, 1)").HasDatabaseName("ux_connected_supplier_relationships_open");
            entity.HasIndex(x=>x.SupplierOrganizationId).HasDatabaseName("ix_connected_supplier_relationships_supplier");
            entity.HasIndex(x=>x.BuyerOrganizationId).HasDatabaseName("ix_connected_supplier_relationships_buyer");
        });
        modelBuilder.Entity<SupplierProductExposureRecord>(entity =>
        {
            entity.ToTable("supplier_product_exposures"); entity.HasKey(x=>x.Id); entity.Property(x=>x.Id).HasColumnName("id");
            entity.Property(x=>x.SupplierOrganizationId).HasColumnName("supplier_organization_id"); entity.Property(x=>x.ProductId).HasColumnName("product_id");
            entity.Property(x=>x.SkuSnapshot).HasColumnName("sku_snapshot").HasMaxLength(64); entity.Property(x=>x.NameSnapshot).HasColumnName("name_snapshot").HasMaxLength(200);
            entity.Property(x=>x.CategoryNameSnapshot).HasColumnName("category_name_snapshot").HasMaxLength(128); entity.Property(x=>x.UnitOfMeasureCode).HasColumnName("unit_of_measure_code").HasMaxLength(32);
            entity.Property(x=>x.SupplierOrderPrice).HasColumnName("supplier_order_price").HasPrecision(18,2); entity.Property(x=>x.IsOrderable).HasColumnName("is_orderable");
            entity.Property(x=>x.IsExposed).HasColumnName("is_exposed"); entity.Property(x=>x.SyncVersion).HasColumnName("sync_version");
            entity.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc"); entity.Property(x=>x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(x=>x.Xmin).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
            entity.HasIndex(x=>new{x.SupplierOrganizationId,x.ProductId}).IsUnique().HasDatabaseName("ux_supplier_product_exposures_product");
            entity.HasIndex(x=>new{x.SupplierOrganizationId,x.SyncVersion}).HasDatabaseName("ix_supplier_product_exposures_sync");
            entity.HasIndex(x=>new{x.SupplierOrganizationId,x.NameSnapshot}).HasDatabaseName("ix_supplier_product_exposures_name");
            entity.HasIndex(x=>new{x.SupplierOrganizationId,x.SkuSnapshot}).HasDatabaseName("ix_supplier_product_exposures_sku");
        });
        modelBuilder.Entity<ConnectedBuyerProductShareRecord>(entity =>
        {
            entity.ToTable("connected_buyer_product_shares");
            entity.HasKey(x=>x.Id); entity.Property(x=>x.Id).HasColumnName("id");
            entity.Property(x=>x.RelationshipId).HasColumnName("relationship_id");
            entity.Property(x=>x.BuyerOrganizationId).HasColumnName("buyer_organization_id");
            entity.Property(x=>x.SupplierOrganizationId).HasColumnName("supplier_organization_id");
            entity.Property(x=>x.SupplierProductId).HasColumnName("supplier_product_id");
            entity.Property(x=>x.IsShared).HasColumnName("is_shared");
            entity.Property(x=>x.BuyerSpecificPoPrice).HasColumnName("buyer_specific_po_price").HasPrecision(18,2);
            entity.Property(x=>x.SyncVersion).HasColumnName("sync_version");
            entity.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.Property(x=>x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(x=>x.Xmin).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
            entity.HasIndex(x=>new{x.RelationshipId,x.SupplierProductId}).IsUnique().HasDatabaseName("ux_connected_buyer_product_shares_relationship_product");
            entity.HasIndex(x=>new{x.RelationshipId,x.IsShared}).HasDatabaseName("ix_connected_buyer_product_shares_relationship_shared");
            entity.HasIndex(x=>new{x.SupplierOrganizationId,x.SupplierProductId}).HasDatabaseName("ix_connected_buyer_product_shares_supplier_product");
        });
        modelBuilder.Entity<BuyerSupplierProductLinkRecord>(entity =>
        {
            entity.ToTable("buyer_supplier_product_links"); entity.HasKey(x=>x.Id); entity.Property(x=>x.Id).HasColumnName("id");
            entity.Property(x=>x.RelationshipId).HasColumnName("relationship_id"); entity.Property(x=>x.BuyerOrganizationId).HasColumnName("buyer_organization_id");
            entity.Property(x=>x.SupplierOrganizationId).HasColumnName("supplier_organization_id"); entity.Property(x=>x.BuyerProductId).HasColumnName("buyer_product_id");
            entity.Property(x=>x.SupplierProductId).HasColumnName("supplier_product_id"); entity.Property(x=>x.SupplierSkuSnapshot).HasColumnName("supplier_sku_snapshot").HasMaxLength(64);
            entity.Property(x=>x.SupplierNameSnapshot).HasColumnName("supplier_name_snapshot").HasMaxLength(200); entity.Property(x=>x.UnitOfMeasureCode).HasColumnName("unit_of_measure_code").HasMaxLength(32);
            entity.Property(x=>x.LastKnownOrderPrice).HasColumnName("last_known_order_price").HasPrecision(18,2);
            entity.Property(x=>x.BuyerPurchaseUnitId).HasColumnName("buyer_purchase_unit_id");
            entity.Property(x=>x.MultiplierToBase).HasColumnName("multiplier_to_base").HasPrecision(18,3).IsRequired().HasDefaultValue(1m);
            entity.Property(x=>x.PackageLabel).HasColumnName("package_label").HasMaxLength(BuyerSupplierProductLink.PackageLabelMaxLength);
            entity.Property(x=>x.IsActive).HasColumnName("is_active");
            entity.Property(x=>x.SyncVersion).HasColumnName("sync_version"); entity.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc"); entity.Property(x=>x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(x=>x.Xmin).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
            entity.HasIndex(x=>new{x.RelationshipId,x.BuyerProductId}).IsUnique().HasFilter("is_active").HasDatabaseName("ux_buyer_supplier_product_links_active");
            entity.HasIndex(x=>new{x.RelationshipId,x.SupplierProductId}).IsUnique().HasFilter("is_active").HasDatabaseName("ux_buyer_supplier_product_links_supplier_active");
            entity.HasIndex(x=>new{x.RelationshipId,x.SyncVersion}).HasDatabaseName("ix_buyer_supplier_product_links_sync");
        });
        modelBuilder.Entity<ConnectedPurchaseOrderRecord>(entity =>
        {
            entity.ToTable("connected_purchase_orders", tb =>
            {
                tb.HasCheckConstraint("ck_connected_purchase_orders_status", "status BETWEEN 0 AND 6");
                tb.HasCheckConstraint("ck_connected_purchase_orders_payment_term", "payment_term BETWEEN 0 AND 2");
            });
            entity.HasKey(x=>x.Id);entity.Property(x=>x.Id).HasColumnName("id");entity.Property(x=>x.RelationshipId).HasColumnName("relationship_id");
            entity.Property(x=>x.BuyerOrganizationId).HasColumnName("buyer_organization_id");entity.Property(x=>x.SupplierOrganizationId).HasColumnName("supplier_organization_id");
            entity.Property(x=>x.BuyerPurchaseOrderId).HasColumnName("buyer_purchase_order_id");entity.Property(x=>x.BuyerPoNumber).HasColumnName("buyer_po_number").HasMaxLength(64);
            entity.Property(x=>x.OrderDate).HasColumnName("order_date").HasColumnType("date");entity.Property(x=>x.Notes).HasColumnName("notes").HasMaxLength(512);
            entity.Property(x=>x.Status).HasColumnName("status");entity.Property(x=>x.TotalAmount).HasColumnName("total_amount").HasPrecision(18,2);
            entity.Property(x=>x.CreatedAtUtc).HasColumnName("created_at_utc");entity.Property(x=>x.UpdatedAtUtc).HasColumnName("updated_at_utc");
            entity.Property(x=>x.AcceptedAtUtc).HasColumnName("accepted_at_utc");entity.Property(x=>x.DeclinedAtUtc).HasColumnName("declined_at_utc");
            entity.Property(x=>x.PreparingAtUtc).HasColumnName("preparing_at_utc");entity.Property(x=>x.FulfilledAtUtc).HasColumnName("fulfilled_at_utc");
            entity.Property(x=>x.WithdrawnAtUtc).HasColumnName("withdrawn_at_utc");
            entity.Property(x=>x.DeclineReason).HasColumnName("decline_reason");
            entity.Property(x=>x.DeclineNote).HasColumnName("decline_note").HasMaxLength(280);
            entity.Property(x=>x.PaymentTerm).HasColumnName("payment_term").IsRequired().HasDefaultValue(0);
            entity.Property(x=>x.ChangesProposedAtUtc).HasColumnName("changes_proposed_at_utc");
            entity.Property(x=>x.ChangesProposedByUserId).HasColumnName("changes_proposed_by_user_id");
            entity.Property(x=>x.BuyerRespondedAtUtc).HasColumnName("buyer_responded_at_utc");
            entity.Property(x=>x.BuyerRespondedByUserId).HasColumnName("buyer_responded_by_user_id");
            entity.Property(x=>x.Xmin).HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();
            entity.HasIndex(x=>x.BuyerPurchaseOrderId).IsUnique().HasDatabaseName("ux_connected_purchase_orders_buyer_po");
            entity.HasIndex(x=>new{x.SupplierOrganizationId,x.Status}).HasDatabaseName("ix_connected_purchase_orders_supplier_status");
            entity.HasMany(x=>x.Lines).WithOne().HasForeignKey(x=>x.ConnectedPurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<ConnectedPurchaseOrderLineRecord>(entity =>
        {
            entity.ToTable("connected_purchase_order_lines", tb =>
            {
                tb.HasCheckConstraint(
                    "ck_connected_po_lines_proposed_qty",
                    "proposed_qty IS NULL OR (proposed_qty >= 0 AND proposed_qty <= qty)");
                tb.HasCheckConstraint(
                    "ck_connected_po_lines_confirmed_qty",
                    "confirmed_qty IS NULL OR (confirmed_qty >= 0 AND confirmed_qty <= qty)");
                tb.HasCheckConstraint("ck_connected_po_lines_availability", "availability BETWEEN 0 AND 2");
            });
            entity.HasKey(x=>new{x.ConnectedPurchaseOrderId,x.LineNumber});
            entity.Property(x=>x.ConnectedPurchaseOrderId).HasColumnName("connected_purchase_order_id");entity.Property(x=>x.LineNumber).HasColumnName("line_number");
            entity.Property(x=>x.ProductId).HasColumnName("product_id");entity.Property(x=>x.NameSnapshot).HasColumnName("name_snapshot").HasMaxLength(200);
            entity.Property(x=>x.SkuSnapshot).HasColumnName("sku_snapshot").HasMaxLength(64);entity.Property(x=>x.Qty).HasColumnName("qty").HasPrecision(18,3);
            entity.Property(x=>x.ProposedQty).HasColumnName("proposed_qty").HasPrecision(18,3);
            entity.Property(x=>x.ConfirmedQty).HasColumnName("confirmed_qty").HasPrecision(18,3);
            entity.Property(x=>x.Availability).HasColumnName("availability").IsRequired().HasDefaultValue(0);
            entity.Property(x=>x.UnitPriceSnapshot).HasColumnName("unit_price_snapshot").HasPrecision(18,2);entity.Property(x=>x.LineTotal).HasColumnName("line_total").HasPrecision(18,2);
            entity.Property(x=>x.UnitOfMeasureCode).HasColumnName("unit_of_measure_code").HasMaxLength(32);
        });
    }
}
