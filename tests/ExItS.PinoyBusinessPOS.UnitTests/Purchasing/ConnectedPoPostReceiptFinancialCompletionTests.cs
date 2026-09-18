using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class ConnectedPoPostReceiptFinancialCompletionTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly SupplierId Supplier =
        SupplierId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 20, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Pod_unpaid_receipt_awaits_payment()
    {
        var outcome = ConnectedPoPostReceiptFinancialCompletion.Evaluate(
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            finalAcceptedValue: 700m,
            settledAmount: 0m);

        Assert.Equal(ConnectedPoFinancialSettlementStatus.AwaitingPayment, outcome.Status);
        Assert.Equal(700m, outcome.RemainingDue);
        Assert.False(outcome.NoPaymentDue);
    }

    [Fact]
    public void Pod_cannot_complete_while_remaining_due_positive()
    {
        var po = BuildReceivedPod(goodQty: 7m, unitCost: 100m);
        ConnectedPoPostReceiptFinancialCompletion.Apply(
            po,
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            finalAcceptedValue: 700m,
            settledAmount: 0m,
            Now);

        Assert.Equal(ConnectedPoFinancialSettlementStatus.AwaitingPayment, po.FinancialSettlementStatus);
        var stillDue = ConnectedPoPostReceiptFinancialCompletion.Evaluate(
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            700m,
            0m);
        Assert.True(stillDue.RemainingDue > 0m);
        Assert.Equal(ConnectedPoFinancialSettlementStatus.AwaitingPayment, stillDue.Status);
        // Domain allows MarkFinanciallySettled only after use-case verifies coverage;
        // commercial gate is the AwaitingPayment status until remaining due is cleared.
        Assert.NotEqual(ConnectedPoFinancialSettlementStatus.Settled, po.FinancialSettlementStatus);
    }

    [Fact]
    public void Pod_partial_payment_remains_awaiting()
    {
        var po = BuildReceivedPod(goodQty: 10m, unitCost: 100m);
        ConnectedPoPostReceiptFinancialCompletion.Apply(
            po,
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            finalAcceptedValue: 1000m,
            settledAmount: 400m,
            Now);

        Assert.Equal(ConnectedPoFinancialSettlementStatus.AwaitingPayment, po.FinancialSettlementStatus);

        var remaining = ConnectedPoPostReceiptFinancialCompletion.Evaluate(
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            1000m,
            400m);
        Assert.Equal(600m, remaining.RemainingDue);
    }

    [Fact]
    public void Pod_final_settlement_marks_settled()
    {
        var po = BuildReceivedPod(goodQty: 10m, unitCost: 100m);
        ConnectedPoPostReceiptFinancialCompletion.Apply(
            po,
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            finalAcceptedValue: 1000m,
            settledAmount: 0m,
            Now);
        Assert.Equal(ConnectedPoFinancialSettlementStatus.AwaitingPayment, po.FinancialSettlementStatus);

        po.RecordPostReceiptSettlementPayment(1000m, Now.AddMinutes(1));
        ConnectedPoPostReceiptFinancialCompletion.Apply(
            po,
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            finalAcceptedValue: 1000m,
            settledAmount: 1000m,
            Now.AddMinutes(1));
        // Apply with remainingDue=0 calls MarkNoPaymentDue → Settled
        Assert.Equal(ConnectedPoFinancialSettlementStatus.Settled, po.FinancialSettlementStatus);
    }

    [Fact]
    public void Zero_amount_due_completes_without_fake_payment()
    {
        var outcome = ConnectedPoPostReceiptFinancialCompletion.Evaluate(
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            finalAcceptedValue: 0m,
            settledAmount: 0m);

        Assert.Equal(ConnectedPoFinancialSettlementStatus.Settled, outcome.Status);
        Assert.True(outcome.NoPaymentDue);

        var po = BuildReceivedPod(goodQty: 0m, unitCost: 100m, ordered: 10m, shortCloseAll: true);
        ConnectedPoPostReceiptFinancialCompletion.Apply(
            po,
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            finalAcceptedValue: 0m,
            settledAmount: 0m,
            Now);
        Assert.Equal(ConnectedPoFinancialSettlementStatus.Settled, po.FinancialSettlementStatus);
    }

    [Fact]
    public void Previously_fully_settled_completes()
    {
        var outcome = ConnectedPoPostReceiptFinancialCompletion.Evaluate(
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Cash,
            finalAcceptedValue: 500m,
            settledAmount: 500m);

        Assert.Equal(ConnectedPoFinancialSettlementStatus.Settled, outcome.Status);
        Assert.Equal(0m, outcome.RemainingDue);
        Assert.True(outcome.NoPaymentDue);
    }

    [Fact]
    public void Pending_check_awaits_payment()
    {
        var outcome = ConnectedPoPostReceiptFinancialCompletion.Evaluate(
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Check,
            finalAcceptedValue: 800m,
            settledAmount: 800m,
            pendingCheckAmount: 800m);

        Assert.Equal(ConnectedPoFinancialSettlementStatus.AwaitingPayment, outcome.Status);
        Assert.Equal(800m, outcome.RemainingDue);
        Assert.False(ConnectedPoPostReceiptFinancialCompletion.CountsAsSettled(
            ConnectedPoPaymentTerm.Check,
            Domain.Payments.UtangCheckClearingStatus.PendingClearing));
    }

    [Fact]
    public void Cleared_check_completes()
    {
        var outcome = ConnectedPoPostReceiptFinancialCompletion.Evaluate(
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Check,
            finalAcceptedValue: 800m,
            settledAmount: 800m,
            pendingCheckAmount: 0m);

        Assert.Equal(ConnectedPoFinancialSettlementStatus.Settled, outcome.Status);
        Assert.True(ConnectedPoPostReceiptFinancialCompletion.CountsAsSettled(
            ConnectedPoPaymentTerm.Check,
            Domain.Payments.UtangCheckClearingStatus.Cleared));
    }

    [Fact]
    public void Pay_before_unchanged_not_required()
    {
        var outcome = ConnectedPoPostReceiptFinancialCompletion.Evaluate(
            ConnectedPoPaymentTiming.PayBeforeFulfillment,
            ConnectedPoPaymentTerm.Cash,
            finalAcceptedValue: 1000m,
            settledAmount: 1000m);

        Assert.Equal(ConnectedPoFinancialSettlementStatus.NotRequired, outcome.Status);
    }

    [Fact]
    public void Supplier_credit_utang_not_required()
    {
        var byTiming = ConnectedPoPostReceiptFinancialCompletion.Evaluate(
            ConnectedPoPaymentTiming.SupplierCredit,
            ConnectedPoPaymentTerm.Cash,
            finalAcceptedValue: 1000m,
            settledAmount: 0m);
        Assert.Equal(ConnectedPoFinancialSettlementStatus.NotRequired, byTiming.Status);

        var byTerm = ConnectedPoPostReceiptFinancialCompletion.Evaluate(
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            ConnectedPoPaymentTerm.Utang,
            finalAcceptedValue: 1000m,
            settledAmount: 0m);
        Assert.Equal(ConnectedPoFinancialSettlementStatus.NotRequired, byTerm.Status);
    }

    [Fact]
    public void Mark_awaiting_payment_is_idempotent()
    {
        var po = BuildReceivedPod(goodQty: 5m, unitCost: 50m);
        po.MarkAwaitingPayment(Now, ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);
        po.MarkAwaitingPayment(Now.AddMinutes(1), ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);
        Assert.Equal(ConnectedPoFinancialSettlementStatus.AwaitingPayment, po.FinancialSettlementStatus);
    }

    [Fact]
    public void Mark_financially_settled_is_idempotent()
    {
        var po = BuildReceivedPod(goodQty: 5m, unitCost: 50m);
        po.MarkAwaitingPayment(Now, ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);
        var actor = Guid.NewGuid();
        po.MarkFinanciallySettled(actor, Now.AddMinutes(1), "first");
        po.MarkFinanciallySettled(Guid.NewGuid(), Now.AddMinutes(2), "second");
        Assert.Equal(ConnectedPoFinancialSettlementStatus.Settled, po.FinancialSettlementStatus);
        Assert.Equal(actor, po.FinanciallySettledBy);
        Assert.Equal("first", po.SellerSettlementRemarks);
    }

    private static PurchaseOrder BuildReceivedPod(
        decimal goodQty,
        decimal unitCost,
        decimal ordered = 10m,
        bool shortCloseAll = false)
    {
        var productId = CatalogProductId.New();
        var po = PurchaseOrder.CreateDraft(
            Org,
            Supplier,
            DateOnly.FromDateTime(Now.UtcDateTime),
            [
                new PurchaseOrderLineDraft(
                    productId,
                    ordered,
                    unitCost,
                    NameSnapshot: "Item",
                    UomSnapshot: UnitOfMeasure.Piece)
            ],
            Now,
            paymentTiming: ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);
        po.Submit(
            "PO-20260918-000101",
            [
                new PurchaseOrderLineSnapshotInput(
                    productId,
                    "Item",
                    UnitOfMeasure.Piece,
                    ordered,
                    unitCost)
            ],
            Guid.NewGuid(),
            Now.AddMinutes(1));

        if (goodQty > 0m || shortCloseAll || goodQty < ordered)
        {
            var discrepancy = Math.Max(0m, ordered - goodQty);
            var shortClosed = shortCloseAll || discrepancy > 0m ? discrepancy : 0m;
            // When short-closing remaining on first receipt, classify discrepancy as not delivered.
            var notDelivered = discrepancy;
            po.ApplyReceiptLines(
                [
                    new PurchaseOrderReceiveLineDraft(
                        productId,
                        goodQty,
                        SellingMode.PerItem,
                        DamagedQty: 0m,
                        RejectedQty: notDelivered,
                        ShortClosedQty: shortClosed,
                        DiscrepancyKind: discrepancy > 0m
                            ? ConnectedPoReceivingDiscrepancyKind.Short
                            : ConnectedPoReceivingDiscrepancyKind.None)
                ],
                Now.AddMinutes(2));
        }

        Assert.Equal(PurchaseOrderStatus.Received, po.Status);
        return po;
    }
}
