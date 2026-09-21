using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedPoReceivingIssueInventoryRulesTests
{
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId Seller = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid FulfillmentSource = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void Missing_FoundAtSeller_and_NeverShipped_restore_seller_stock()
    {
        Assert.True(ConnectedPoMissingResolutions.RestoresSellerStock(ConnectedPoMissingResolution.FoundAtSeller));
        Assert.True(ConnectedPoMissingResolutions.RestoresSellerStock(ConnectedPoMissingResolution.NeverShipped));
        Assert.False(ConnectedPoMissingResolutions.RestoresSellerStock(ConnectedPoMissingResolution.LostInTransit));
        Assert.False(ConnectedPoMissingResolutions.RestoresSellerStock(ConnectedPoMissingResolution.DeliveredDisputed));
        Assert.False(ConnectedPoMissingResolutions.RestoresSellerStock(ConnectedPoMissingResolution.ReplacementPlanned));
        Assert.False(ConnectedPoMissingResolutions.RestoresSellerStock(ConnectedPoMissingResolution.Other));
    }

    [Fact]
    public void Reconciliation_movement_is_positive_and_idempotent_by_issue_line_id()
    {
        var accountId = InventoryAccountId.New();
        var productId = CatalogProductId.New();
        var lineId = Guid.NewGuid();
        var movement = StockMovement.ConnectedPurchaseFulfillmentReconciliation(
            Seller,
            productId,
            accountId,
            quantity: 1m,
            UnitOfMeasure.Piece,
            lineId,
            FulfillmentSource,
            Actor,
            Now,
            branchId: Guid.NewGuid());

        Assert.Equal(StockMovementType.ConnectedPurchaseFulfillmentReconciliation, movement.MovementType);
        Assert.Equal(1m, movement.QuantityEffect);
        Assert.Equal(lineId, movement.SourceId);
        Assert.Equal(StockMovementSourceType.ConnectedPurchaseOrder, movement.SourceType);
        Assert.Contains(FulfillmentSource.ToString("D"), movement.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateFromReceipt_builds_missing_and_damaged_lines_without_touching_grn()
    {
        var issue = BuildIssue(damaged: 1m, missing: 1m, good: 8m);
        Assert.Equal(ConnectedPoReceivingIssueStatus.PendingSellerReview, issue.Status);
        Assert.Equal(2, issue.Lines.Count);
        Assert.Equal(2, issue.UnresolvedLineCount);
        Assert.Contains(issue.Lines, l => l.LineKind == ConnectedPoReceivingIssueLineKind.Missing && l.MissingQty == 1m);
        Assert.Contains(issue.Lines, l => l.LineKind == ConnectedPoReceivingIssueLineKind.Damaged && l.DamagedQty == 1m);
    }

    [Fact]
    public void Good_only_receipt_creates_no_issue()
    {
        var drafts = Array.Empty<ConnectedPoReceivingIssueLineDraft>();
        Assert.Throws<Domain.Common.DomainException>(() =>
            ConnectedPoReceivingIssue.CreateFromReceipt(
                ConnectedPurchaseOrderId.New(),
                PurchaseOrderId.New(),
                GoodsReceiptId.New(),
                Buyer,
                Seller,
                FulfillmentSource,
                Actor,
                Now,
                drafts));
    }

    [Fact]
    public void Resolve_missing_FoundAtSeller_records_movement_id_and_completes_issue()
    {
        var issue = BuildIssue(damaged: 0m, missing: 1m, good: 9m);
        var missing = Assert.Single(issue.Lines);
        var movementId = Guid.NewGuid();
        missing.ResolveMissing(
            ConnectedPoMissingResolution.FoundAtSeller,
            1m,
            sellerNote: null,
            Actor,
            Now.AddMinutes(1),
            movementId);
        issue.RefreshStatus(Actor, Now.AddMinutes(1));

        Assert.True(missing.IsResolved);
        Assert.Equal(movementId, missing.InventoryMovementId);
        Assert.Equal(ConnectedPoReceivingIssueStatus.Resolved, issue.Status);
    }

    [Fact]
    public void Resolve_missing_LostInTransit_has_no_movement()
    {
        var issue = BuildIssue(damaged: 0m, missing: 1m, good: 9m);
        var missing = Assert.Single(issue.Lines);
        missing.ResolveMissing(
            ConnectedPoMissingResolution.LostInTransit,
            1m,
            sellerNote: null,
            Actor,
            Now.AddMinutes(1),
            inventoryMovementId: null);
        Assert.Null(missing.InventoryMovementId);
        Assert.True(missing.IsResolved);
    }

    [Fact]
    public void Resolve_damaged_AcceptedNoReturn_has_no_seller_stock_increase()
    {
        var issue = BuildIssue(damaged: 1m, missing: 0m, good: 9m);
        var damaged = Assert.Single(issue.Lines);
        damaged.ResolveDamaged(
            ConnectedPoDamagedResolution.AcceptedNoReturn,
            1m,
            sellerNote: null,
            Actor,
            Now.AddMinutes(1),
            inventoryMovementId: null);
        Assert.Null(damaged.InventoryMovementId);
        Assert.Null(damaged.ReturnBatchId);
    }

    [Fact]
    public void Resolve_damaged_ReturnRequested_can_link_return_batch()
    {
        var issue = BuildIssue(damaged: 1m, missing: 0m, good: 9m);
        var damaged = Assert.Single(issue.Lines);
        var batchId = Guid.NewGuid();
        damaged.ResolveDamaged(
            ConnectedPoDamagedResolution.ReturnRequested,
            1m,
            sellerNote: "Please return",
            Actor,
            Now.AddMinutes(1),
            inventoryMovementId: null,
            returnBatchId: batchId);
        Assert.Equal(batchId, damaged.ReturnBatchId);
        Assert.Null(damaged.InventoryMovementId);
    }

    [Fact]
    public void Retry_same_resolution_is_idempotent()
    {
        var issue = BuildIssue(damaged: 0m, missing: 1m, good: 9m);
        var missing = Assert.Single(issue.Lines);
        var movementId = Guid.NewGuid();
        missing.ResolveMissing(
            ConnectedPoMissingResolution.NeverShipped,
            1m,
            null,
            Actor,
            Now.AddMinutes(1),
            movementId);
        missing.ResolveMissing(
            ConnectedPoMissingResolution.NeverShipped,
            1m,
            null,
            Actor,
            Now.AddMinutes(2),
            movementId);
        Assert.Equal(movementId, missing.InventoryMovementId);
    }

    [Fact]
    public void Different_resolution_after_resolve_is_rejected()
    {
        var issue = BuildIssue(damaged: 0m, missing: 1m, good: 9m);
        var missing = Assert.Single(issue.Lines);
        missing.ResolveMissing(
            ConnectedPoMissingResolution.FoundAtSeller,
            1m,
            null,
            Actor,
            Now.AddMinutes(1),
            Guid.NewGuid());
        Assert.Throws<Domain.Common.DomainException>(() =>
            missing.ResolveMissing(
                ConnectedPoMissingResolution.LostInTransit,
                1m,
                null,
                Actor,
                Now.AddMinutes(2),
                inventoryMovementId: null));
    }

    [Fact]
    public void EnsureSellerOrganization_rejects_cross_org()
    {
        var issue = BuildIssue(damaged: 0m, missing: 1m, good: 9m);
        Assert.Throws<Domain.Common.DomainException>(() =>
            issue.EnsureSellerOrganization(Buyer));
    }

    [Fact]
    public void Preview_text_matches_inventory_rules()
    {
        Assert.Contains("+1", ConnectedPoReceivingIssueMapper.PreviewInventoryEffectForMissing(
            ConnectedPoMissingResolution.FoundAtSeller, 1m), StringComparison.Ordinal);
        Assert.Contains("No stock change", ConnectedPoReceivingIssueMapper.PreviewInventoryEffectForMissing(
            ConnectedPoMissingResolution.LostInTransit, 1m), StringComparison.Ordinal);
        Assert.Contains("return workflow", ConnectedPoReceivingIssueMapper.PreviewInventoryEffectForDamaged(
            ConnectedPoDamagedResolution.ReturnRequested), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Multi_wave_source_id_differs_from_order_id()
    {
        var orderId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var wave = ConnectedPurchaseOrderFulfillStock.WaveFulfillmentSourceId(orderId, reservationRevision: 2);
        Assert.NotEqual(orderId, wave);
        Assert.Equal(wave, ConnectedPurchaseOrderFulfillStock.WaveFulfillmentSourceId(orderId, 2));
    }

    private static ConnectedPoReceivingIssue BuildIssue(decimal damaged, decimal missing, decimal good)
    {
        var drafts = new List<ConnectedPoReceivingIssueLineDraft>();
        var grnLineId = GoodsReceiptLineId.New();
        var poLineId = PurchaseOrderLineId.New();
        var productId = CatalogProductId.New();
        if (missing > 0m)
        {
            drafts.Add(new ConnectedPoReceivingIssueLineDraft(
                grnLineId,
                poLineId,
                productId,
                productId,
                "Coke 1.5L",
                "Piece",
                good + damaged + missing,
                good,
                damaged,
                missing,
                ConnectedPoReceivingIssueLineKind.Missing,
                ConnectedPoReceivingDiscrepancyKind.Short,
                "carton missing"));
        }

        if (damaged > 0m)
        {
            drafts.Add(new ConnectedPoReceivingIssueLineDraft(
                grnLineId,
                poLineId,
                productId,
                productId,
                "Coke 1.5L",
                "Piece",
                good + damaged + missing,
                good,
                damaged,
                missing,
                ConnectedPoReceivingIssueLineKind.Damaged,
                ConnectedPoReceivingDiscrepancyKind.Damaged,
                "dented"));
        }

        return ConnectedPoReceivingIssue.CreateFromReceipt(
            ConnectedPurchaseOrderId.New(),
            PurchaseOrderId.New(),
            GoodsReceiptId.New(),
            Buyer,
            Seller,
            FulfillmentSource,
            Actor,
            Now,
            drafts);
    }
}
