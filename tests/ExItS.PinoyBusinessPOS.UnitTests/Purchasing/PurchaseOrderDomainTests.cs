using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class PurchaseOrderDomainTests
{
    private static readonly Guid OrgA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProductA = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ProductB = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid SupplierA = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Now = new(2026, 7, 31, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Po_and_grn_numbers_format_and_normalize()
    {
        var date = new DateOnly(2026, 7, 31);
        Assert.Equal("PO-20260731-000001", PurchaseOrderNumbers.Format(date, 1));
        Assert.Equal("GRN-20260731-000042", GoodsReceiptNumbers.Format(date, 42));
        Assert.Equal("PO-20260731-000001", PurchaseOrderNumbers.Normalize(" po-20260731-000001 "));
    }

    [Fact]
    public void Draft_rejects_duplicate_products_and_negative_cost()
    {
        var org = PosOrganizationId.From(OrgA);
        var lines = new[]
        {
            new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 10m, 5m),
            new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 1m, 1m)
        };

        var dup = Assert.Throws<DomainException>(() =>
            PurchaseOrder.CreateDraft(org, SupplierId.From(SupplierA), DateOnly.FromDateTime(Now.Date), lines, Now));
        Assert.Equal(DomainErrorCodes.PurchaseOrderDuplicateProduct, dup.ErrorCode);

        var negative = Assert.Throws<DomainException>(() =>
            PurchaseOrder.CreateDraft(
                org,
                SupplierId.From(SupplierA),
                DateOnly.FromDateTime(Now.Date),
                [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 1m, -0.01m)],
                Now));
        Assert.Equal(DomainErrorCodes.InvalidPurchaseUnitCost, negative.ErrorCode);
    }

    [Fact]
    public void Submit_freezes_snapshots_and_allocates_ordered_state()
    {
        var org = PosOrganizationId.From(OrgA);
        var po = PurchaseOrder.CreateDraft(
            org,
            SupplierId.From(SupplierA),
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 2m, 10.5m, "line note")],
            Now);

        var snapshots = new[]
        {
            new PurchaseOrderLineSnapshotInput(
                CatalogProductId.From(ProductA),
                "Bigas Premium",
                UnitOfMeasure.Kilogram,
                2m,
                10.5m,
                "line note")
        };

        po.Submit("PO-20260731-000001", snapshots, Guid.NewGuid(), Now);

        Assert.Equal(PurchaseOrderStatus.Ordered, po.Status);
        Assert.Equal("PO-20260731-000001", po.PoNumber);
        var line = po.Lines.Single();
        Assert.Equal("Bigas Premium", line.NameSnapshot);
        Assert.Equal(UnitOfMeasure.Kilogram, line.UomSnapshot);
        Assert.Equal(21m, line.LineTotal);
        Assert.Equal(2m, line.OutstandingQty);
    }

    [Fact]
    public void Receive_partial_then_full_and_deny_over_receipt()
    {
        var org = PosOrganizationId.From(OrgA);
        var po = BuildOrderedPo(org, orderedQty: 10m);

        po.ApplyReceiptLines([new PurchaseOrderReceiveLineDraft(CatalogProductId.From(ProductA), 4m)], Now);
        Assert.Equal(PurchaseOrderStatus.PartiallyReceived, po.Status);
        Assert.Equal(6m, po.Lines.Single().OutstandingQty);

        var over = Assert.Throws<DomainException>(() =>
            po.ApplyReceiptLines([new PurchaseOrderReceiveLineDraft(CatalogProductId.From(ProductA), 7m)], Now));
        Assert.Equal(DomainErrorCodes.PurchaseOverReceipt, over.ErrorCode);

        var grn = GoodsReceipt.Create(
            org,
            po.Id,
            "GRN-20260731-000001",
            po,
            [new PurchaseOrderReceiveLineDraft(CatalogProductId.From(ProductA), 4m)],
            Guid.NewGuid(),
            Now);
        Assert.Single(grn.Lines);
        Assert.Equal(SupplierA, grn.SupplierId.Value);
        Assert.Equal(4m, grn.Lines.Single().QuantityReceived);
        Assert.Equal(10m, grn.Lines.Single().UnitPurchaseCostSnapshot);
        Assert.Equal(40m, grn.Lines.Single().LineTotalSnapshot);

        po.ApplyReceiptLines([new PurchaseOrderReceiveLineDraft(CatalogProductId.From(ProductA), 6m)], Now);
        Assert.Equal(PurchaseOrderStatus.Received, po.Status);

        var afterReceived = Assert.Throws<DomainException>(() =>
            po.ApplyReceiptLines([new PurchaseOrderReceiveLineDraft(CatalogProductId.From(ProductA), 1m)], Now));
        Assert.Equal(DomainErrorCodes.InvalidPurchaseOrderStatusTransition, afterReceived.ErrorCode);
    }

    [Fact]
    public void Cancel_allowed_from_draft_and_ordered_not_after_receipts()
    {
        var org = PosOrganizationId.From(OrgA);
        var draft = PurchaseOrder.CreateDraft(
            org,
            SupplierId.From(SupplierA),
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 1m, 1m)],
            Now);
        draft.Cancel(Now);
        Assert.Equal(PurchaseOrderStatus.Cancelled, draft.Status);

        var ordered = BuildOrderedPo(org, 5m);
        ordered.Cancel(Now);
        Assert.Equal(PurchaseOrderStatus.Cancelled, ordered.Status);

        var partial = BuildOrderedPo(org, 5m);
        partial.ApplyReceiptLines([new PurchaseOrderReceiveLineDraft(CatalogProductId.From(ProductA), 1m)], Now);
        var ex = Assert.Throws<DomainException>(() => partial.Cancel(Now));
        Assert.Equal(DomainErrorCodes.InvalidPurchaseOrderStatusTransition, ex.ErrorCode);
    }

    private static PurchaseOrder BuildOrderedPo(PosOrganizationId org, decimal orderedQty)
    {
        var po = PurchaseOrder.CreateDraft(
            org,
            SupplierId.From(SupplierA),
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), orderedQty, 10m)],
            Now);
        po.Submit(
            "PO-20260731-000099",
            [new PurchaseOrderLineSnapshotInput(
                CatalogProductId.From(ProductA),
                "Item",
                UnitOfMeasure.Piece,
                orderedQty,
                10m)],
            Guid.NewGuid(),
            Now);
        return po;
    }
}
