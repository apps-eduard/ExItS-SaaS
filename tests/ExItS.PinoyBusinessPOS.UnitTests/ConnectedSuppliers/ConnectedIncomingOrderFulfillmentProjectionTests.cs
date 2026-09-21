using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedIncomingOrderFulfillmentProjectionTests
{
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PosOrganizationId Supplier = PosOrganizationId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly PosBranchId Branch = PosBranchId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    private static readonly Guid Actor = Guid.Parse("99999999-9999-9999-9999-999999999999");
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid AppleId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid BananaId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Partial_receipt_projects_good_damaged_and_outstanding_per_line()
    {
        var (order, po) = SeedTwoLineOrder();
        var receive = new List<PurchaseOrderReceiveLineDraft>
        {
            new(CatalogProductId.From(AppleId), ReceiveQty: 3m, DamagedQty: 1m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Damaged, DiscrepancyNote: "Bruised"),
            new(CatalogProductId.From(BananaId), ReceiveQty: 2m, RejectedQty: 2m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short),
        };
        po.ApplyReceiptLines(receive, Now.AddMinutes(10));
        var grn = GoodsReceipt.Create(
            Buyer,
            po.Id,
            "GRN-20260917-000001",
            po,
            receive,
            Actor,
            Now.AddMinutes(10),
            deliveryReference: "DRV-1",
            notes: "Partial drop",
            receivingBranchId: Branch);

        var progress = ConnectedIncomingOrderFulfillmentProjection.ProjectLineProgress(order, po, [grn]);
        var apple = progress[AppleId];
        var banana = progress[BananaId];

        Assert.Equal(4m, apple.OrderedQty);
        Assert.Equal(3m, apple.GoodReceivedQty);
        Assert.Equal(1m, apple.DamagedQty);
        Assert.Equal(0m, apple.MissingQty);
        Assert.Equal(1m, apple.OutstandingQty);
        Assert.Equal(10m, apple.UnitCost);
        Assert.Equal(10m, apple.RemainingValue);

        Assert.Equal(4m, banana.OrderedQty);
        Assert.Equal(2m, banana.GoodReceivedQty);
        Assert.Equal(0m, banana.DamagedQty);
        Assert.Equal(2m, banana.OutstandingQty);
        Assert.Equal(16m, banana.RemainingValue);

        var receipts = ConnectedIncomingOrderFulfillmentProjection.ProjectReceipts([grn], remainingOutstandingAfterLatest: 3m);
        var latest = Assert.Single(receipts);
        Assert.Equal("GRN-20260917-000001", latest.GrnNumber);
        Assert.Equal(5m, latest.GoodQtyTotal);
        Assert.Equal(1m, latest.DamagedQtyTotal);
        Assert.Equal("DRV-1", latest.DeliveryReference);
        Assert.Equal("Partial drop", latest.Notes);
        Assert.Equal("Posted", latest.Status);
    }

    [Fact]
    public void Cumulative_receipts_reduce_outstanding_and_exclude_voided()
    {
        var (order, po) = SeedTwoLineOrder();
        var first = new List<PurchaseOrderReceiveLineDraft>
        {
            new(CatalogProductId.From(AppleId), ReceiveQty: 3m, DamagedQty: 1m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Damaged),
            new(CatalogProductId.From(BananaId), ReceiveQty: 2m, RejectedQty: 2m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short),
        };
        po.ApplyReceiptLines(first, Now.AddMinutes(10));
        var grn1 = GoodsReceipt.Create(
            Buyer, po.Id, "GRN-20260917-000001", po, first, Actor, Now.AddMinutes(10), receivingBranchId: Branch);

        var second = new List<PurchaseOrderReceiveLineDraft>
        {
            new(CatalogProductId.From(AppleId), ReceiveQty: 1m),
            new(CatalogProductId.From(BananaId), ReceiveQty: 1m, RejectedQty: 1m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short),
        };
        po.ApplyReceiptLines(second, Now.AddMinutes(20));
        var grn2 = GoodsReceipt.Create(
            Buyer, po.Id, "GRN-20260917-000002", po, second, Actor, Now.AddMinutes(20), receivingBranchId: Branch);

        var voidedDraft = new List<PurchaseOrderReceiveLineDraft>
        {
            new(CatalogProductId.From(BananaId), ReceiveQty: 1m),
        };
        var voided = GoodsReceipt.Create(
            Buyer, po.Id, "GRN-20260917-000099", po, voidedDraft, Actor, Now.AddMinutes(5), receivingBranchId: Branch);
        voided.Void(Now.AddMinutes(6), Actor, "Mistake");

        var progress = ConnectedIncomingOrderFulfillmentProjection.ProjectLineProgress(
            order, po, [grn1, grn2, voided]);

        Assert.Equal(4m, progress[AppleId].GoodReceivedQty);
        Assert.Equal(1m, progress[AppleId].DamagedQty);
        Assert.Equal(0m, progress[AppleId].OutstandingQty);
        Assert.Equal(3m, progress[BananaId].GoodReceivedQty);
        Assert.Equal(1m, progress[BananaId].OutstandingQty);

        var receipts = ConnectedIncomingOrderFulfillmentProjection.ProjectReceipts(
            [grn1, grn2, voided], remainingOutstandingAfterLatest: 1m);
        Assert.Equal(3, receipts.Count);
        Assert.Equal("GRN-20260917-000002", receipts[0].GrnNumber);
        Assert.Equal("Voided", receipts[2].Status);
    }

    [Fact]
    public void Damaged_does_not_count_as_good_received()
    {
        var (order, po) = SeedTwoLineOrder(appleQty: 4m, bananaQty: 0m, includeBanana: false);
        var receive = new List<PurchaseOrderReceiveLineDraft>
        {
            new(CatalogProductId.From(AppleId), ReceiveQty: 0m, DamagedQty: 4m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Damaged),
        };
        po.ApplyReceiptLines(receive, Now.AddMinutes(10));
        var grn = GoodsReceipt.Create(
            Buyer, po.Id, "GRN-20260917-000010", po, receive, Actor, Now.AddMinutes(10), receivingBranchId: Branch);

        var apple = ConnectedIncomingOrderFulfillmentProjection.ProjectLineProgress(order, po, [grn])[AppleId];
        Assert.Equal(0m, apple.GoodReceivedQty);
        Assert.Equal(4m, apple.DamagedQty);
        Assert.Equal(4m, apple.OutstandingQty);
    }

    [Fact]
    public void Missing_rejected_qty_is_projected_separately()
    {
        var (order, po) = SeedTwoLineOrder(appleQty: 5m, bananaQty: 0m, includeBanana: false);
        var receive = new List<PurchaseOrderReceiveLineDraft>
        {
            new(CatalogProductId.From(AppleId), ReceiveQty: 3m, RejectedQty: 2m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short, DiscrepancyNote: "Not on truck"),
        };
        po.ApplyReceiptLines(receive, Now.AddMinutes(10));
        var grn = GoodsReceipt.Create(
            Buyer, po.Id, "GRN-20260917-000011", po, receive, Actor, Now.AddMinutes(10), receivingBranchId: Branch);

        var apple = ConnectedIncomingOrderFulfillmentProjection.ProjectLineProgress(order, po, [grn])[AppleId];
        Assert.Equal(3m, apple.GoodReceivedQty);
        Assert.Equal(2m, apple.MissingQty);
        Assert.Equal(2m, apple.OutstandingQty);
    }

    [Fact]
    public void Cross_catalog_ids_match_via_product_links_and_project_damage()
    {
        // Seller CPO uses supplier catalog ids; buyer PO/GRN use distinct buyer catalog ids.
        // Without links, FindBuyerLine fails → good/damaged stay 0 and outstanding falls back to ordered.
        var supplierApple = AppleId;
        var buyerApple = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var supplierBanana = BananaId;
        var buyerBanana = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));

        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-20260920-000001",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [
                ConnectedPurchaseOrderLine.Create(CatalogProductId.From(supplierApple), "Apple", null, 1m, 10m, "Piece"),
                ConnectedPurchaseOrderLine.Create(CatalogProductId.From(supplierBanana), "Banana", null, 1m, 8m, "Piece"),
            ],
            Now.AddMinutes(2));
        order.Accept(Now.AddMinutes(3));
        order.MarkFulfilled(Now.AddMinutes(4));

        var po = PurchaseOrder.CreateDraft(
            Buyer,
            SupplierId.New(),
            DateOnly.FromDateTime(Now.UtcDateTime),
            [
                new(CatalogProductId.From(buyerApple), 1m, 10m),
                new(CatalogProductId.From(buyerBanana), 1m, 8m),
            ],
            Now);
        po.Submit(
            "PO-20260920-000001",
            [
                new(CatalogProductId.From(buyerApple), "Apple", UnitOfMeasure.Piece, 1m, 10m),
                new(CatalogProductId.From(buyerBanana), "Banana", UnitOfMeasure.Piece, 1m, 8m),
            ],
            Actor,
            Now.AddMinutes(1));

        // Banana fully good; Apple all damaged (deliver-later) → only 1 outstanding to fulfill.
        var receive = new List<PurchaseOrderReceiveLineDraft>
        {
            new(CatalogProductId.From(buyerApple), ReceiveQty: 0m, DamagedQty: 1m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Damaged),
            new(CatalogProductId.From(buyerBanana), ReceiveQty: 1m),
        };
        po.ApplyReceiptLines(receive, Now.AddMinutes(10));
        var grn = GoodsReceipt.Create(
            Buyer, po.Id, "GRN-20260920-000001", po, receive, Actor, Now.AddMinutes(10), receivingBranchId: Branch);

        var withoutLinks = ConnectedIncomingOrderFulfillmentProjection.ProjectLineProgress(order, po, [grn]);
        Assert.Equal(0m, withoutLinks[supplierApple].GoodReceivedQty);
        Assert.Equal(0m, withoutLinks[supplierApple].DamagedQty);
        Assert.Equal(1m, withoutLinks[supplierApple].OutstandingQty);
        Assert.Equal(0m, withoutLinks[supplierBanana].GoodReceivedQty);
        Assert.Equal(1m, withoutLinks[supplierBanana].OutstandingQty);

        var links = new ConnectedIncomingOrderFulfillmentProjection.ProductLinkMaps(
            new Dictionary<Guid, Guid>
            {
                [supplierApple] = buyerApple,
                [supplierBanana] = buyerBanana,
            },
            new Dictionary<Guid, Guid>
            {
                [buyerApple] = supplierApple,
                [buyerBanana] = supplierBanana,
            });

        var withLinks = ConnectedIncomingOrderFulfillmentProjection.ProjectLineProgress(order, po, [grn], links);
        Assert.Equal(0m, withLinks[supplierApple].GoodReceivedQty);
        Assert.Equal(1m, withLinks[supplierApple].DamagedQty);
        Assert.Equal(1m, withLinks[supplierApple].OutstandingQty);
        Assert.Equal(1m, withLinks[supplierBanana].GoodReceivedQty);
        Assert.Equal(0m, withLinks[supplierBanana].DamagedQty);
        Assert.Equal(0m, withLinks[supplierBanana].OutstandingQty);
    }

    [Fact]
    public void Cross_catalog_ids_match_via_supplier_product_id_on_buyer_line()
    {
        var supplierApple = AppleId;
        var buyerApple = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));

        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-LINKED-SUPPLIER-ID",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [
                ConnectedPurchaseOrderLine.Create(CatalogProductId.From(supplierApple), "Apple", null, 4m, 10m, "Piece"),
            ],
            Now.AddMinutes(2));
        order.Accept(Now.AddMinutes(3));
        order.MarkFulfilled(Now.AddMinutes(4));

        var po = PurchaseOrder.CreateDraft(
            Buyer,
            SupplierId.New(),
            DateOnly.FromDateTime(Now.UtcDateTime),
            [
                new(
                    CatalogProductId.From(buyerApple),
                    4m,
                    10m,
                    SupplierProductId: CatalogProductId.From(supplierApple)),
            ],
            Now);
        po.Submit(
            "PO-20260920-000002",
            [
                new(
                    CatalogProductId.From(buyerApple),
                    "Apple",
                    UnitOfMeasure.Piece,
                    4m,
                    10m,
                    SupplierProductId: CatalogProductId.From(supplierApple)),
            ],
            Actor,
            Now.AddMinutes(1));

        var receive = new List<PurchaseOrderReceiveLineDraft>
        {
            new(CatalogProductId.From(buyerApple), ReceiveQty: 3m, DamagedQty: 1m,
                DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Damaged),
        };
        po.ApplyReceiptLines(receive, Now.AddMinutes(10));
        var grn = GoodsReceipt.Create(
            Buyer, po.Id, "GRN-20260920-000002", po, receive, Actor, Now.AddMinutes(10), receivingBranchId: Branch);

        var apple = ConnectedIncomingOrderFulfillmentProjection.ProjectLineProgress(order, po, [grn])[supplierApple];
        Assert.Equal(3m, apple.GoodReceivedQty);
        Assert.Equal(1m, apple.DamagedQty);
        Assert.Equal(1m, apple.OutstandingQty);
    }

    private static (ConnectedPurchaseOrder Order, PurchaseOrder BuyerPo) SeedTwoLineOrder(
        decimal appleQty = 4m,
        decimal bananaQty = 4m,
        bool includeBanana = true)
    {
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));

        var cpoLines = new List<ConnectedPurchaseOrderLine>
        {
            ConnectedPurchaseOrderLine.Create(CatalogProductId.From(AppleId), "Apple", null, appleQty, 10m, "Piece"),
        };
        if (includeBanana)
        {
            cpoLines.Add(
                ConnectedPurchaseOrderLine.Create(CatalogProductId.From(BananaId), "Banana", null, bananaQty, 8m, "Piece"));
        }

        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-PARTIAL-1",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            cpoLines,
            Now.AddMinutes(2));
        order.Accept(Now.AddMinutes(3));
        order.MarkFulfilled(Now.AddMinutes(4));

        var drafts = new List<PurchaseOrderLineDraft>
        {
            new(CatalogProductId.From(AppleId), appleQty, 10m),
        };
        var snapshots = new List<PurchaseOrderLineSnapshotInput>
        {
            new(CatalogProductId.From(AppleId), "Apple", UnitOfMeasure.Piece, appleQty, 10m),
        };
        if (includeBanana)
        {
            drafts.Add(new(CatalogProductId.From(BananaId), bananaQty, 8m));
            snapshots.Add(new(CatalogProductId.From(BananaId), "Banana", UnitOfMeasure.Piece, bananaQty, 8m));
        }

        var po = PurchaseOrder.CreateDraft(
            Buyer,
            SupplierId.New(),
            DateOnly.FromDateTime(Now.UtcDateTime),
            drafts,
            Now);
        po.Submit("PO-20260917-000042", snapshots, Actor, Now.AddMinutes(1));
        return (order, po);
    }
}
