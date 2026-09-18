using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Returns;

public sealed class ReturnBatchDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 16, 15, 0, TimeSpan.Zero);

    [Fact]
    public void Classify_requires_sellable_and_damaged_to_sum_to_accepted()
    {
        var sale = BuildCompletedSale(10m, 10m);
        var batch = ReturnBatch.CreateAccepted(
            Org,
            "RB-20260918-000001",
            sale,
            [new ReturnBatchAcceptedLineDraft(sale.Lines[0].Id, 10m)],
            new Dictionary<Guid, (decimal, decimal)>(),
            "damaged packaging",
            Actor,
            Now);

        var ex = Assert.Throws<DomainException>(() =>
            batch.ClassifyLine(batch.Lines[0].Id, 3m, 6m, null, Actor, Now));
        Assert.Equal(DomainErrorCodes.InvalidReturnBatchClassification, ex.ErrorCode);
    }

    [Fact]
    public void Cannot_finalize_until_all_lines_are_classified()
    {
        var sale = BuildCompletedSale(10m, 10m);
        var batch = ReturnBatch.CreateAccepted(
            Org,
            "RB-20260918-000002",
            sale,
            [new ReturnBatchAcceptedLineDraft(sale.Lines[0].Id, 10m)],
            new Dictionary<Guid, (decimal, decimal)>(),
            "damaged packaging",
            Actor,
            Now);

        var ex = Assert.Throws<DomainException>(() =>
            batch.MarkFinalized(
                SaleReturnId.New(),
                ReturnBatchRefundStatus.Refunded,
                refundDueAmount: 0m,
                refundedAmount: 100m,
                actorId: Actor,
                utcNow: Now));
        Assert.Equal(DomainErrorCodes.ReturnBatchNotReadyForFinalize, ex.ErrorCode);
    }

    [Fact]
    public void Inventory_pending_return_increase_and_decrease_validate_bounds()
    {
        var account = InventoryAccount.CreateUntracked(Org, CatalogProductId.New(), Now);
        account.Enable(20m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);

        account.IncreasePendingReturn(10m);
        account.DecreasePendingReturn(4m);
        Assert.Equal(6m, account.PendingReturnQuantity);

        var ex = Assert.Throws<DomainException>(() => account.DecreasePendingReturn(7m));
        Assert.Equal(DomainErrorCodes.InvalidInventoryPendingReturnQuantity, ex.ErrorCode);
    }

    [Fact]
    public void Pending_return_reduces_available_without_touching_on_hand()
    {
        var account = InventoryAccount.CreateUntracked(Org, CatalogProductId.New(), Now);
        account.Enable(20m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);
        account.Reserve(5m);
        var onHandBefore = account.OnHandQuantity;
        var availableBefore = account.AvailableQuantity;

        account.IncreasePendingReturn(10m);

        Assert.Equal(10m, account.PendingReturnQuantity);
        Assert.Equal(onHandBefore, account.OnHandQuantity);
        Assert.Equal(availableBefore - 10m, account.AvailableQuantity);

        account.DecreasePendingReturn(10m);

        Assert.Equal(0m, account.PendingReturnQuantity);
        Assert.Equal(availableBefore, account.AvailableQuantity);
    }

    [Fact]
    public void Record_refund_supports_partial_then_full_and_marks_refunded()
    {
        var batch = CreateReadyFinalizedBatch(ReturnBatchRefundStatus.RefundDue, due: 300m, refunded: 0m);

        var first = batch.RecordRefund(100m, SalePaymentMethod.GCash, "ref-1", null, Actor, Now, "refund-1");
        Assert.Equal(100m, first.Amount);
        Assert.Equal(100m, batch.RefundedAmount);
        Assert.Equal(ReturnBatchRefundStatus.RefundDue, batch.RefundStatus);

        batch.RecordRefund(200m, SalePaymentMethod.BankTransfer, "ref-2", "full", Actor, Now.AddMinutes(1), "refund-2");
        Assert.Equal(300m, batch.RefundedAmount);
        Assert.Equal(ReturnBatchRefundStatus.Refunded, batch.RefundStatus);
    }

    [Fact]
    public void Record_refund_is_idempotent_on_client_refund_id()
    {
        var batch = CreateReadyFinalizedBatch(ReturnBatchRefundStatus.RefundDue, due: 300m, refunded: 0m);

        var first = batch.RecordRefund(100m, SalePaymentMethod.GCash, "ref", null, Actor, Now, "dup");
        var second = batch.RecordRefund(100m, SalePaymentMethod.GCash, "ref", null, Actor, Now.AddMinutes(1), "dup");

        Assert.Equal(first.Id, second.Id);
        Assert.Single(batch.Refunds);
        Assert.Equal(100m, batch.RefundedAmount);
    }

    [Fact]
    public void Timeline_events_are_recorded_for_accept_classify_finalize_and_refund()
    {
        var sale = BuildCompletedSale(10m, 100m);
        var batch = ReturnBatch.CreateAccepted(
            Org,
            "RB-20260918-000050",
            sale,
            [new ReturnBatchAcceptedLineDraft(sale.Lines[0].Id, 5m)],
            new Dictionary<Guid, (decimal, decimal)>(),
            "quality return",
            Actor,
            Now);
        batch.ClassifyLine(batch.Lines[0].Id, 2m, 3m, "mixed", Actor, Now.AddMinutes(1));
        batch.MarkFinalized(SaleReturnId.New(), ReturnBatchRefundStatus.RefundDue, 50m, 0m, Actor, Now.AddMinutes(2));
        batch.RecordRefund(50m, SalePaymentMethod.GCash, "proof", null, Actor, Now.AddMinutes(3), "cid-1");

        var eventTypes = batch.Timeline.Select(e => e.EventType).ToList();
        Assert.Contains(ReturnBatchAuditEventType.Accepted, eventTypes);
        Assert.Contains(ReturnBatchAuditEventType.ClassificationSaved, eventTypes);
        Assert.Contains(ReturnBatchAuditEventType.Finalized, eventTypes);
        Assert.Contains(ReturnBatchAuditEventType.RefundRecorded, eventTypes);
    }

    [Fact]
    public void Accepted_quantity_cannot_exceed_refundable_across_batches()
    {
        var sale = BuildCompletedSale(10m, 100m);
        var prior = new Dictionary<Guid, (decimal, decimal)>
        {
            [sale.Lines[0].Id.Value] = (8m, 80m)
        };

        var ex = Assert.Throws<DomainException>(() => ReturnBatch.CreateAccepted(
            Org,
            "RB-20260918-000060",
            sale,
            [new ReturnBatchAcceptedLineDraft(sale.Lines[0].Id, 3m)],
            prior,
            "exceeds",
            Actor,
            Now));
        Assert.Equal(DomainErrorCodes.ReturnBatchQuantityExceedsRefundable, ex.ErrorCode);
    }

    private static ReturnBatch CreateReadyFinalizedBatch(ReturnBatchRefundStatus refundStatus, decimal due, decimal refunded)
    {
        var sale = BuildCompletedSale(40m, 400m);
        var batch = ReturnBatch.CreateAccepted(
            Org,
            "RB-20260918-000040",
            sale,
            [new ReturnBatchAcceptedLineDraft(sale.Lines[0].Id, 40m)],
            new Dictionary<Guid, (decimal, decimal)>(),
            "customer return",
            Actor,
            Now);
        batch.ClassifyLine(batch.Lines[0].Id, 40m, 0m, null, Actor, Now);
        batch.MarkFinalized(SaleReturnId.New(), refundStatus, due, refunded, Actor, Now);
        return batch;
    }

    private static Sale BuildCompletedSale(decimal quantity, decimal lineTotal)
    {
        var saleId = SaleId.New();
        var line = SaleLine.Rehydrate(
            SaleLineId.New(),
            saleId,
            Org,
            CatalogProductId.New(),
            lineNumber: 1,
            "Milk",
            "MILK-1",
            null,
            UnitOfMeasure.Piece,
            unitPrice: 10m,
            quantity,
            lineTotal,
            grossLineTotal: quantity * 10m,
            lineDiscountAmount: (quantity * 10m) - lineTotal);
        return Sale.Rehydrate(
            saleId,
            Org,
            "SALE-20260918-000001",
            SaleStatus.Completed,
            SalePaymentMethod.ManualGCash,
            lineTotal,
            lineTotal,
            0m,
            lineTotal,
            0m,
            null,
            Now,
            Actor,
            null,
            null,
            null,
            Now,
            [line]);
    }
}
