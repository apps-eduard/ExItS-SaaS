using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Returns;

public sealed class ConnectedPoReturnBatchTests
{
    private static readonly PosOrganizationId BuyerOrg =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId SellerOrg =
        PosOrganizationId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly SupplierId Supplier =
        SupplierId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    [Fact]
    public void Buyer_request_owns_batch_on_seller_and_awaits_seller_receipt()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);

        var batch = RequestReturn(po, quantity: 4m);

        Assert.Equal(SellerOrg, batch.OrganizationId);
        Assert.Equal(ReturnBatchSourceType.ConnectedPurchaseOrder, batch.SourceType);
        Assert.Equal(ReturnBatchStatus.AwaitingSellerReceipt, batch.Status);
        Assert.Null(batch.SaleId);
        Assert.Equal(po.Id, batch.PurchaseOrderId);
        Assert.Equal(BuyerOrg, batch.BuyerOrganizationId);
        Assert.Equal(SellerOrg, batch.SellerOrganizationId);
        Assert.Equal(po.PoNumber, batch.PoNumberSnapshot);
        Assert.Equal(100m, batch.AcceptedReturnValue);
        var line = Assert.Single(batch.Lines);
        Assert.Null(line.SaleLineId);
        Assert.Equal(po.Lines[0].Id, line.PurchaseOrderLineId);
    }

    [Fact]
    public void Return_value_uses_locked_unit_purchase_cost()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 33.50m);

        var batch = RequestReturn(po, quantity: 3m);

        Assert.Equal(100.50m, batch.AcceptedReturnValue);
        Assert.Equal(33.50m, batch.Lines[0].UnitPriceSnapshot);
    }

    [Fact]
    public void Cannot_return_more_than_good_received_quantity()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 6m, unitCost: 25m);

        var ex = Assert.Throws<DomainException>(() => RequestReturn(po, quantity: 7m));

        Assert.Equal(DomainErrorCodes.ReturnBatchQuantityExceedsReceived, ex.ErrorCode);
    }

    [Fact]
    public void Prior_returned_quantity_reduces_remaining_returnable()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var poLineId = po.Lines[0].Id;

        var ex = Assert.Throws<DomainException>(() => RequestReturn(
            po,
            quantity: 5m,
            prior: new Dictionary<Guid, decimal> { [poLineId.Value] = 6m }));

        Assert.Equal(DomainErrorCodes.ReturnBatchQuantityExceedsReceived, ex.ErrorCode);
    }

    [Fact]
    public void Classify_before_seller_receipt_is_rejected()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var batch = RequestReturn(po, quantity: 4m);

        var ex = Assert.Throws<DomainException>(() => batch.ClassifyLine(
            batch.Lines[0].Id,
            sellableQuantity: 4m,
            damagedQuantity: 0m,
            inspectionNote: null,
            Actor,
            Now.AddHours(1)));

        Assert.Equal(DomainErrorCodes.ReturnBatchAwaitingSellerReceipt, ex.ErrorCode);
    }

    [Fact]
    public void Seller_receipt_moves_to_pending_inspection_and_is_idempotent()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var batch = RequestReturn(po, quantity: 4m);

        Assert.True(batch.MarkReceivedBySeller(Actor, Now.AddHours(1)));
        Assert.Equal(ReturnBatchStatus.PendingInspection, batch.Status);
        Assert.Equal(Now.AddHours(1), batch.SellerReceivedAtUtc);

        Assert.False(batch.MarkReceivedBySeller(Actor, Now.AddHours(2)));
        Assert.Equal(Now.AddHours(1), batch.SellerReceivedAtUtc);
        Assert.Single(batch.Timeline, e => e.EventType == ReturnBatchAuditEventType.ReceivedBySeller);
    }

    [Fact]
    public void Finalize_before_seller_receipt_is_rejected()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var batch = RequestReturn(po, quantity: 4m);

        var ex = Assert.Throws<DomainException>(() => batch.MarkFinalized(
            null,
            ReturnBatchRefundStatus.RefundDue,
            100m,
            0m,
            Actor,
            Now.AddHours(1)));

        Assert.Equal(DomainErrorCodes.ReturnBatchAwaitingSellerReceipt, ex.ErrorCode);
    }

    [Fact]
    public void Ten_returned_three_sellable_seven_damaged_restocks_three()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var batch = RequestReturn(po, quantity: 10m);
        batch.MarkReceivedBySeller(Actor, Now.AddHours(1));
        batch.ClassifyLine(batch.Lines[0].Id, 3m, 7m, null, Actor, Now.AddHours(2));

        Assert.True(batch.AllLinesClassified);
        Assert.Equal(3m, batch.Lines[0].SellableQuantity);
        Assert.Equal(7m, batch.Lines[0].DamagedQuantity);
        Assert.Equal(250m, batch.AcceptedReturnValue);

        var sellerAccount = InventoryAccount.CreateUntracked(SellerOrg, po.Lines[0].ProductId!, Now);
        sellerAccount.Enable(0m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);
        sellerAccount.IncreasePendingReturn(10m);

        sellerAccount.DecreasePendingReturn(10m);
        sellerAccount.ApplyMovementEffect(batch.Lines[0].SellableQuantity!.Value);

        Assert.Equal(0m, sellerAccount.PendingReturnQuantity);
        Assert.Equal(3m, sellerAccount.OnHandQuantity);
        Assert.Equal(3m, sellerAccount.AvailableQuantity);
    }

    [Fact]
    public void Finalize_is_idempotent()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var batch = RequestReturn(po, quantity: 4m);
        batch.MarkReceivedBySeller(Actor, Now.AddHours(1));
        batch.ClassifyLine(batch.Lines[0].Id, 4m, 0m, null, Actor, Now.AddHours(2));

        batch.MarkFinalized(null, ReturnBatchRefundStatus.RefundDue, 100m, 0m, Actor, Now.AddHours(3));
        Assert.Equal(ReturnBatchStatus.Finalized, batch.Status);

        var ex = Assert.Throws<DomainException>(() =>
            batch.MarkFinalized(null, ReturnBatchRefundStatus.RefundDue, 100m, 0m, Actor, Now.AddHours(4)));
        Assert.Equal(DomainErrorCodes.ReturnBatchAlreadyFinalized, ex.ErrorCode);
        Assert.Single(batch.Timeline, e => e.EventType == ReturnBatchAuditEventType.Finalized);
    }

    [Fact]
    public void Prepaid_return_produces_refund_due_for_full_return_value()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var payables = new[] { BuildPayable(paidAtReceipt: 250m, original: 250m) };

        var goodReceived = ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(po);
        var settled = ConnectedPoReturnSettlementResolver.ResolveSettledPayments(po, payables);
        var settlement = ConnectedPoReturnSettlementResolver.Evaluate(goodReceived, settled, 100m, 0m, 0m);

        Assert.Equal(250m, goodReceived);
        Assert.Equal(250m, settled);
        Assert.Equal(100m, settlement.RefundDue);
        Assert.Equal(
            ReturnBatchRefundStatus.RefundDue,
            ConnectedPoReturnSettlementResolver.ResolveRefundStatus(
                ConnectedPoPaymentTiming.PayBeforeFulfillment,
                ConnectedPoPaymentTerm.Cash,
                settlement));
    }

    [Fact]
    public void Prepaid_short_close_discrepancy_refund_due_uses_amount_paid_snapshot()
    {
        var po = BuildPartiallyReceivedPo(ordered: 10m, received: 6m, unitCost: 100m);
        po.CloseAllRemaining("Cannot supply remaining", Actor, Now.AddMinutes(5), refundDueAmount: 400m, amountPaid: 1000m);

        var goodReceived = ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(po);
        var settled = ConnectedPoReturnSettlementResolver.ResolveSettledPayments(po, Array.Empty<SupplierPayable>());
        var settlement = ConnectedPoReturnSettlementResolver.Evaluate(goodReceived, settled, 200m, 0m, 0m);

        Assert.Equal(600m, goodReceived);
        Assert.Equal(1000m, settled);
        // 400 already owed back from the short close plus 200 for the returned goods.
        Assert.Equal(600m, settlement.RefundDue);
    }

    [Fact]
    public void Pay_on_delivery_partial_payment_reduces_obligation_before_refund()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var payables = new[] { BuildPayable(paidAtReceipt: 150m, original: 250m) };

        var goodReceived = ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(po);
        var settled = ConnectedPoReturnSettlementResolver.ResolveSettledPayments(po, payables);
        var settlement = ConnectedPoReturnSettlementResolver.Evaluate(goodReceived, settled, 50m, 0m, 0m);

        Assert.Equal(150m, settled);
        Assert.Equal(200m, settlement.AdjustedObligation);
        Assert.Equal(50m, settlement.RemainingDue);
        Assert.Equal(50m, settlement.ObligationReducedAmount);
        Assert.Equal(0m, settlement.RefundDue);
        Assert.Equal(
            ReturnBatchRefundStatus.ObligationReduced,
            ConnectedPoReturnSettlementResolver.ResolveRefundStatus(
                ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
                ConnectedPoPaymentTerm.Cash,
                settlement));
    }

    [Fact]
    public void Pay_on_delivery_overpayment_still_refunds_the_excess()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var payables = new[] { BuildPayable(paidAtReceipt: 250m, original: 250m) };

        var settlement = ConnectedPoReturnSettlementResolver.Evaluate(
            ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(po),
            ConnectedPoReturnSettlementResolver.ResolveSettledPayments(po, payables),
            acceptedReturnValue: 50m,
            priorAcceptedReturnValues: 0m,
            alreadyRefundedAcrossBatches: 0m);

        Assert.Equal(50m, settlement.RefundDue);
        Assert.Equal(0m, settlement.RemainingDue);
    }

    [Fact]
    public void Utang_return_reduces_credit_instead_of_paying_cash()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var payables = new[] { BuildPayable(paidAtReceipt: 0m, original: 250m) };

        var settlement = ConnectedPoReturnSettlementResolver.Evaluate(
            ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(po),
            ConnectedPoReturnSettlementResolver.ResolveSettledPayments(po, payables),
            acceptedReturnValue: 100m,
            priorAcceptedReturnValues: 0m,
            alreadyRefundedAcrossBatches: 0m);

        Assert.Equal(0m, settlement.RefundDue);
        Assert.Equal(150m, settlement.AdjustedObligation);
        Assert.Equal(100m, settlement.ObligationReducedAmount);
        Assert.Equal(
            ReturnBatchRefundStatus.CreditReduced,
            ConnectedPoReturnSettlementResolver.ResolveRefundStatus(
                ConnectedPoPaymentTiming.SupplierCredit,
                ConnectedPoPaymentTerm.Utang,
                settlement));
    }

    [Fact]
    public void Pending_check_reduces_obligation_and_cleared_check_refunds()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var payables = new[] { BuildPayable(paidAtReceipt: 250m, original: 250m) };
        var goodReceived = ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(po);

        var pending = ConnectedPoReturnSettlementResolver.Evaluate(
            goodReceived,
            ConnectedPoReturnSettlementResolver.ResolveSettledPayments(po, payables, unclearedCheckAmount: 250m),
            acceptedReturnValue: 100m,
            priorAcceptedReturnValues: 0m,
            alreadyRefundedAcrossBatches: 0m);

        Assert.Equal(0m, pending.RefundDue);
        Assert.Equal(100m, pending.ObligationReducedAmount);

        var cleared = ConnectedPoReturnSettlementResolver.Evaluate(
            goodReceived,
            ConnectedPoReturnSettlementResolver.ResolveSettledPayments(po, payables),
            acceptedReturnValue: 100m,
            priorAcceptedReturnValues: 0m,
            alreadyRefundedAcrossBatches: 0m);

        Assert.Equal(100m, cleared.RefundDue);
    }

    [Fact]
    public void Prior_refunds_are_not_paid_twice()
    {
        var po = BuildReceivedPo(ordered: 10m, received: 10m, unitCost: 25m);
        var payables = new[] { BuildPayable(paidAtReceipt: 250m, original: 250m) };
        var goodReceived = ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(po);
        var settled = ConnectedPoReturnSettlementResolver.ResolveSettledPayments(po, payables);

        var second = ConnectedPoReturnSettlementResolver.Evaluate(
            goodReceived,
            settled,
            acceptedReturnValue: 50m,
            priorAcceptedReturnValues: 100m,
            alreadyRefundedAcrossBatches: 100m);

        Assert.Equal(50m, second.RefundDue);
    }

    [Fact]
    public void Receiving_damage_does_not_create_a_return_batch()
    {
        var po = BuildPoWithReceiptDamage(ordered: 10m, good: 8m, damaged: 2m, unitCost: 25m);

        // Damaged-on-arrival quantity is never received, so it is not returnable stock.
        Assert.Equal(8m, po.Lines[0].ReceivedQty);
        Assert.Equal(2m, po.Lines[0].ClosedShortQty);
        Assert.Equal(200m, ConnectedPoReturnSettlementResolver.ResolveGoodReceivedValue(po));

        var ex = Assert.Throws<DomainException>(() => RequestReturn(po, quantity: 9m));
        Assert.Equal(DomainErrorCodes.ReturnBatchQuantityExceedsReceived, ex.ErrorCode);
    }

    private static ReturnBatch RequestReturn(
        PurchaseOrder po,
        decimal quantity,
        IReadOnlyDictionary<Guid, decimal>? prior = null) =>
        ReturnBatch.CreateAcceptedForConnectedPurchaseOrder(
            SellerOrg,
            BuyerOrg,
            "RB-20260918-000001",
            po,
            [new ReturnBatchConnectedPoLineDraft(po.Lines[0].Id, quantity)],
            prior ?? new Dictionary<Guid, decimal>(),
            "Wrong item delivered",
            Actor,
            Now.AddMinutes(10),
            paymentTiming: ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);

    /// <summary>Fully received purchase order: any shortage is short-closed so the PO reaches Received.</summary>
    private static PurchaseOrder BuildReceivedPo(decimal ordered, decimal received, decimal unitCost)
    {
        var po = BuildSubmittedPo(ordered, unitCost, out var productId);
        var gap = ordered - received;
        po.ApplyReceiptLines(
            [
                new PurchaseOrderReceiveLineDraft(
                    productId,
                    received,
                    SellingMode.PerItem,
                    DamagedQty: 0m,
                    RejectedQty: gap > 0m ? gap : 0m,
                    ShortClosedQty: gap > 0m ? gap : 0m,
                    DiscrepancyKind: gap > 0m
                        ? ConnectedPoReceivingDiscrepancyKind.Short
                        : ConnectedPoReceivingDiscrepancyKind.None),
            ],
            Now.AddMinutes(2));
        return po;
    }

    /// <summary>Leaves outstanding quantity so the caller can exercise <c>CloseAllRemaining</c>.</summary>
    private static PurchaseOrder BuildPartiallyReceivedPo(decimal ordered, decimal received, decimal unitCost)
    {
        var po = BuildSubmittedPo(ordered, unitCost, out var productId);
        po.ApplyReceiptLines(
            [
                new PurchaseOrderReceiveLineDraft(
                    productId,
                    received,
                    SellingMode.PerItem,
                    DamagedQty: 0m,
                    RejectedQty: ordered - received,
                    ShortClosedQty: 0m,
                    DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Short),
            ],
            Now.AddMinutes(2));
        return po;
    }

    private static PurchaseOrder BuildPoWithReceiptDamage(
        decimal ordered,
        decimal good,
        decimal damaged,
        decimal unitCost)
    {
        var po = BuildSubmittedPo(ordered, unitCost, out var productId);
        po.ApplyReceiptLines(
            [
                new PurchaseOrderReceiveLineDraft(
                    productId,
                    good,
                    SellingMode.PerItem,
                    DamagedQty: damaged,
                    RejectedQty: ordered - good - damaged,
                    ShortClosedQty: ordered - good,
                    DiscrepancyKind: ConnectedPoReceivingDiscrepancyKind.Damaged),
            ],
            Now.AddMinutes(2));
        return po;
    }

    private static PurchaseOrder BuildSubmittedPo(decimal ordered, decimal unitCost, out CatalogProductId productId)
    {
        productId = CatalogProductId.New();
        var draft = PurchaseOrder.CreateDraft(
            BuyerOrg,
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
            "PO-20260918-000001",
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
            Actor,
            Now.AddMinutes(1));
        return draft;
    }

    private static SupplierPayable BuildPayable(decimal paidAtReceipt, decimal original) =>
        SupplierPayable.Create(
            BuyerOrg,
            Supplier,
            SupplierPayableSourceType.GoodsReceipt,
            Guid.NewGuid(),
            original,
            Actor,
            Now,
            paidNow: paidAtReceipt,
            paymentMethodAtReceipt: SupplierPayablePaymentMethod.Cash);
}
