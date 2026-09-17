using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class ConnectedPoShortCloseSettlementTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly SupplierId Supplier =
        SupplierId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Ten_ordered_six_good_four_cancelled_settles_on_good_only()
    {
        var po = BuildPo(ordered: 10m, received: 6m, unitCost: 100m);
        po.CloseAllRemaining("Cannot supply remaining", Guid.NewGuid(), Now, refundDueAmount: 0m, amountPaid: 600m);

        Assert.Equal(PurchaseOrderStatus.Received, po.Status);
        Assert.Equal(0m, po.Lines[0].OutstandingQty);
        Assert.Equal(4m, po.Lines[0].ClosedShortQty);
        Assert.Equal(600m, po.FinalAcceptedValue);
        Assert.Equal(400m, po.CancelledRemainingValue);
        Assert.Equal(0m, po.RefundDueAmount);
        Assert.Equal(600m, po.AmountPaidSnapshot);
    }

    [Fact]
    public void Prepaid_excess_creates_refund_due()
    {
        var po = BuildPo(ordered: 10m, received: 6m, unitCost: 100m);
        var payables = new[]
        {
            BuildPayable(paidAtReceipt: 1000m, original: 1000m),
        };
        var preview = ConnectedPoShortCloseSettlement.Compute(po, payables, treatOutstandingAsCancelled: true);
        Assert.Equal(600m, preview.FinalAcceptedValue);
        Assert.Equal(1000m, preview.AmountPaid);
        Assert.Equal(400m, preview.RefundDue);

        po.CloseAllRemaining("Shortage", Guid.NewGuid(), Now, preview.RefundDue, preview.AmountPaid);
        Assert.Equal(400m, po.RefundDueAmount);
    }

    [Fact]
    public void Exact_payment_has_no_refund()
    {
        var po = BuildPo(ordered: 10m, received: 6m, unitCost: 100m);
        var payables = new[] { BuildPayable(paidAtReceipt: 600m, original: 600m) };
        var preview = ConnectedPoShortCloseSettlement.Compute(po, payables, treatOutstandingAsCancelled: true);
        Assert.Equal(0m, preview.RefundDue);
    }

    [Fact]
    public void Cod_charges_good_received_only()
    {
        var po = BuildPo(ordered: 10m, received: 6m, unitCost: 100m);
        var payables = new[] { BuildPayable(paidAtReceipt: 600m, original: 600m) };
        var preview = ConnectedPoShortCloseSettlement.Compute(po, payables, treatOutstandingAsCancelled: true);
        Assert.Equal(600m, preview.FinalAcceptedValue);
        Assert.Equal(400m, preview.CancelledRemainingValue);
        Assert.Equal(0m, preview.RefundDue);
        Assert.Equal(0m, preview.BalanceDue);
    }

    [Fact]
    public void Utang_retains_payable_for_accepted_goods_only()
    {
        var po = BuildPo(ordered: 10m, received: 6m, unitCost: 100m);
        var payables = new[] { BuildPayable(paidAtReceipt: 0m, original: 600m) };
        var preview = ConnectedPoShortCloseSettlement.Compute(po, payables, treatOutstandingAsCancelled: true);
        Assert.Equal(600m, preview.FinalAcceptedValue);
        Assert.Equal(0m, preview.AmountPaid);
        Assert.Equal(600m, preview.BalanceDue);
        Assert.Equal(0m, preview.RefundDue);
    }

    [Fact]
    public void Damaged_and_not_delivered_are_not_charged()
    {
        var po = BuildPoWithDiscrepancy(
            ordered: 10m,
            good: 6m,
            damaged: 2m,
            notDelivered: 2m,
            unitCost: 100m);
        var preview = ConnectedPoShortCloseSettlement.Compute(
            po,
            Array.Empty<SupplierPayable>(),
            treatOutstandingAsCancelled: false);
        Assert.Equal(600m, preview.FinalAcceptedValue);
        Assert.Equal(0m, preview.CancelledRemainingValue);
    }

    [Fact]
    public void Multi_receipt_final_accepted_sums_good_only()
    {
        var po = BuildPo(ordered: 10m, received: 0m, unitCost: 50m);
        ApplyPartialReceipt(po, productId: po.Lines[0].ProductId!, good: 4m, rejectedGap: 6m);
        ApplyPartialReceipt(po, productId: po.Lines[0].ProductId!, good: 2m, rejectedGap: 4m);
        Assert.Equal(6m, po.Lines[0].ReceivedQty);
        var preview = ConnectedPoShortCloseSettlement.Compute(
            po,
            Array.Empty<SupplierPayable>(),
            treatOutstandingAsCancelled: true);
        Assert.Equal(300m, preview.FinalAcceptedValue);
        Assert.Equal(200m, preview.CancelledRemainingValue);
    }

    [Fact]
    public void Duplicate_close_is_idempotent()
    {
        var po = BuildPo(ordered: 10m, received: 6m, unitCost: 100m);
        var actor = Guid.NewGuid();
        po.CloseAllRemaining("First", actor, Now, 0m, 600m);
        po.CloseAllRemaining("Second", actor, Now.AddMinutes(1), 999m, 1m);
        Assert.Equal("First", po.RemainingClosedReason);
        Assert.Equal(0m, po.RefundDueAmount);
        Assert.Equal(600m, po.AmountPaidSnapshot);
    }

    [Fact]
    public void Closing_with_no_outstanding_throws()
    {
        var po = BuildPo(ordered: 10m, received: 10m, unitCost: 100m);
        var ex = Assert.Throws<DomainException>(() =>
            po.CloseAllRemaining("n/a", Guid.NewGuid(), Now, 0m, 1000m));
        Assert.Equal(DomainErrorCodes.InvalidPurchaseOrderStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Weighted_qty_precision_preserved()
    {
        var po = BuildPo(ordered: 2.5m, received: 1.25m, unitCost: 40m, uom: UnitOfMeasure.Kilogram);
        po.CloseAllRemaining("Partial kg", Guid.NewGuid(), Now, 0m, 50m);
        Assert.Equal(1.25m, po.Lines[0].ClosedShortQty);
        Assert.Equal(50m, po.FinalAcceptedValue);
        Assert.Equal(50m, po.CancelledRemainingValue);
    }

    private static PurchaseOrder BuildPo(
        decimal ordered,
        decimal received,
        decimal unitCost,
        UnitOfMeasure uom = UnitOfMeasure.Piece)
    {
        var productId = CatalogProductId.New();
        var draft = PurchaseOrder.CreateDraft(
            Org,
            Supplier,
            DateOnly.FromDateTime(Now.UtcDateTime),
            [
                new PurchaseOrderLineDraft(
                    productId,
                    ordered,
                    unitCost,
                    NameSnapshot: "Apple",
                    UomSnapshot: uom,
                    SupplierProductId: productId),
            ],
            Now);
        draft.Submit(
            "PO-20260918-000001",
            [
                new PurchaseOrderLineSnapshotInput(
                    productId,
                    "Apple",
                    uom,
                    ordered,
                    unitCost,
                    SellingMode: SellingMode.PerItem,
                    SupplierProductId: productId),
            ],
            Guid.NewGuid(),
            Now.AddMinutes(1));
        if (received > 0m)
        {
            var gap = ordered - received;
            draft.ApplyReceiptLines(
                [
                    new PurchaseOrderReceiveLineDraft(
                        productId,
                        received,
                        SellingMode.PerItem,
                        DamagedQty: 0m,
                        RejectedQty: gap > 0m ? gap : 0m,
                        ShortClosedQty: 0m,
                        DiscrepancyKind: gap > 0m
                            ? ConnectedPoReceivingDiscrepancyKind.Short
                            : ConnectedPoReceivingDiscrepancyKind.None),
                ],
                Now.AddMinutes(2));
        }

        return draft;
    }

    private static PurchaseOrder BuildPoWithDiscrepancy(
        decimal ordered,
        decimal good,
        decimal damaged,
        decimal notDelivered,
        decimal unitCost)
    {
        var productId = CatalogProductId.New();
        var draft = PurchaseOrder.CreateDraft(
            Org,
            Supplier,
            DateOnly.FromDateTime(Now.UtcDateTime),
            [
                new PurchaseOrderLineDraft(
                    productId,
                    ordered,
                    unitCost,
                    NameSnapshot: "Apple",
                    UomSnapshot: UnitOfMeasure.Piece,
                    SupplierProductId: productId),
            ],
            Now);
        draft.Submit(
            "PO-20260918-000002",
            [
                new PurchaseOrderLineSnapshotInput(
                    productId,
                    "Apple",
                    UnitOfMeasure.Piece,
                    ordered,
                    unitCost,
                    SellingMode: SellingMode.PerItem,
                    SupplierProductId: productId),
            ],
            Guid.NewGuid(),
            Now.AddMinutes(1));
        draft.ApplyReceiptLines(
            [
                new PurchaseOrderReceiveLineDraft(
                    productId,
                    good,
                    SellingMode.PerItem,
                    DamagedQty: damaged,
                    RejectedQty: notDelivered,
                    ShortClosedQty: 0m,
                    DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short),
            ],
            Now.AddMinutes(2));
        return draft;
    }

    private static void ApplyPartialReceipt(
        PurchaseOrder po,
        CatalogProductId productId,
        decimal good,
        decimal rejectedGap)
    {
        po.ApplyReceiptLines(
            [
                new PurchaseOrderReceiveLineDraft(
                    productId,
                    good,
                    SellingMode.PerItem,
                    DamagedQty: 0m,
                    RejectedQty: rejectedGap,
                    ShortClosedQty: 0m,
                    DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short),
            ],
            Now.AddMinutes(3));
    }

    private static SupplierPayable BuildPayable(decimal paidAtReceipt, decimal original) =>
        SupplierPayable.Create(
            Org,
            Supplier,
            SupplierPayableSourceType.GoodsReceipt,
            Guid.NewGuid(),
            original,
            Guid.NewGuid(),
            Now,
            paidNow: paidAtReceipt,
            paymentMethodAtReceipt: SupplierPayablePaymentMethod.Cash);
}
