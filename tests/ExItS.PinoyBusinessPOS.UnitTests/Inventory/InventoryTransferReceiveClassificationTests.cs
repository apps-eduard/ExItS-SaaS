using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryTransferReceiveClassificationTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosBranchId BranchA = PosBranchId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PosBranchId BranchB = PosBranchId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly CatalogProductId Coke = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Utc = new(2026, 9, 22, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_good_and_damaged_closes_with_discrepancy_and_records_receipt_detail()
    {
        var transfer = DispatchSingleLine(100m);
        var receipt = transfer.Receive(
            [Classify(Coke, good: 70m, damaged: 30m)],
            Actor,
            Utc.AddMinutes(2));

        Assert.Equal(InventoryTransferStatus.ClosedWithDiscrepancy, transfer.Status);
        Assert.Equal(Utc.AddMinutes(2), transfer.ClosedAtUtc);
        Assert.Equal(Actor, transfer.ClosedBy);
        var line = transfer.Lines.Single();
        Assert.Equal(70m, line.ReceivedQty);
        Assert.Equal(30m, line.ClosedQty);
        Assert.Equal(0m, line.OutstandingQty);

        var receiptLine = Assert.Single(receipt.Lines);
        Assert.Equal(70m, receiptLine.QuantityReceived);
        Assert.Equal(30m, receiptLine.QuantityDamaged);
        Assert.Equal(0m, receiptLine.QuantityMissing);
    }

    [Fact]
    public void B_missing_expected_later_leaves_outstanding_and_partially_received()
    {
        var transfer = DispatchSingleLine(100m);
        transfer.Receive(
            [Classify(Coke, good: 70m, missing: 30m, missingDisposition: InventoryTransferMissingDisposition.ExpectedLater)],
            Actor,
            Utc.AddMinutes(2));

        Assert.Equal(InventoryTransferStatus.PartiallyReceived, transfer.Status);
        var line = transfer.Lines.Single();
        Assert.Equal(70m, line.ReceivedQty);
        Assert.Equal(0m, line.ClosedQty);
        Assert.Equal(30m, line.OutstandingQty);
    }

    [Fact]
    public void C_missing_close_missing_closes_with_discrepancy()
    {
        var transfer = DispatchSingleLine(100m);
        transfer.Receive(
            [Classify(Coke, good: 70m, missing: 30m, missingDisposition: InventoryTransferMissingDisposition.CloseMissing)],
            Actor,
            Utc.AddMinutes(2));

        Assert.Equal(InventoryTransferStatus.ClosedWithDiscrepancy, transfer.Status);
        var line = transfer.Lines.Single();
        Assert.Equal(70m, line.ReceivedQty);
        Assert.Equal(30m, line.ClosedQty);
        Assert.Equal(0m, line.OutstandingQty);
    }

    [Fact]
    public void D_mixed_classification_keeps_open_in_transit_for_expected_later_missing()
    {
        var transfer = DispatchSingleLine(100m);
        transfer.Receive(
            [
                Classify(
                    Coke,
                    good: 70m,
                    damaged: 20m,
                    missing: 10m,
                    missingDisposition: InventoryTransferMissingDisposition.ExpectedLater)
            ],
            Actor,
            Utc.AddMinutes(2));

        var line = transfer.Lines.Single();
        Assert.Equal(70m, line.ReceivedQty);
        Assert.Equal(20m, line.ClosedQty);
        Assert.Equal(10m, line.OutstandingQty);
        Assert.Equal(InventoryTransferStatus.PartiallyReceived, transfer.Status);
    }

    [Fact]
    public void E_close_remainder_after_expected_later_missing_updates_stock_request_remaining()
    {
        var transfer = DispatchSingleLine(100m);
        transfer.Receive(
            [
                Classify(
                    Coke,
                    good: 70m,
                    damaged: 20m,
                    missing: 10m,
                    missingDisposition: InventoryTransferMissingDisposition.ExpectedLater)
            ],
            Actor,
            Utc.AddMinutes(2));

        var request = StockRequest.Create(
            Org,
            BranchB,
            BranchA,
            [new StockRequestLineDraft(Coke, 100m, "Coke", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            "260922-200");
        request.Approve(Actor, Utc.AddMinutes(1), new Dictionary<Guid, decimal> { [Coke.Value] = 100m });
        request.MarkDispatched(Actor, Utc.AddMinutes(1), transfer.Id.Value);

        var afterReceive = StockRequestDispatchCoverage.Compute(request, [transfer]);
        Assert.Equal(20m, afterReceive.RemainingToDispatchByProduct[Coke.Value]);

        transfer.CloseRemainder(
            Actor,
            Utc.AddMinutes(3),
            transferLevelReason: InventoryTransferDiscrepancyReason.LostInTransit);

        var afterClose = StockRequestDispatchCoverage.Compute(request, [transfer]);
        Assert.Equal(30m, afterClose.RemainingToDispatchByProduct[Coke.Value]);
        Assert.Equal(30m, transfer.Lines.Single().ClosedQty);
        Assert.Equal(0m, transfer.Lines.Single().OutstandingQty);
    }

    [Fact]
    public void F_multi_wave_good_only_reaches_received()
    {
        var transfer = DispatchSingleLine(100m);
        transfer.Receive([Classify(Coke, good: 50m)], Actor, Utc.AddMinutes(2));
        transfer.Receive([Classify(Coke, good: 50m)], Actor, Utc.AddMinutes(3));

        Assert.Equal(InventoryTransferStatus.Received, transfer.Status);
        Assert.Equal(100m, transfer.Lines.Single().ReceivedQty);
        Assert.Equal(2, transfer.Receipts.Count);
    }

    [Fact]
    public void H_other_classification_closes_without_missing_disposition()
    {
        var transfer = DispatchSingleLine(100m);
        transfer.Receive(
            [
                Classify(
                    Coke,
                    good: 80m,
                    other: 20m,
                    otherReasonCode: ReceiveDiscrepancyOtherReason.WrongItem)
            ],
            Actor,
            Utc.AddMinutes(2));

        var line = transfer.Lines.Single();
        Assert.Equal(80m, line.ReceivedQty);
        Assert.Equal(20m, line.ClosedQty);
        Assert.Equal(InventoryTransferDiscrepancyReason.WrongItem, line.DiscrepancyReason);
        var receiptLine = Assert.Single(transfer.Receipts.Single().Lines);
        Assert.Equal(20m, receiptLine.QuantityOther);
        Assert.Equal(ReceiveDiscrepancyOtherReason.WrongItem, receiptLine.OtherReasonCode);
    }

    [Fact]
    public void G_rejects_over_receive_and_classification_mismatch()
    {
        var transfer = DispatchSingleLine(100m);

        var over = Assert.Throws<DomainException>(() =>
            transfer.Receive([Classify(Coke, good: 101m)], Actor, Utc.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferReceiveQty, over.ErrorCode);

        var mismatch = Assert.Throws<DomainException>(() =>
            transfer.Receive([Classify(Coke, good: 70m, damaged: 20m)], Actor, Utc.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferReceiveClassification, mismatch.ErrorCode);

        var missingDisposition = Assert.Throws<DomainException>(() =>
            transfer.Receive([Classify(Coke, good: 70m, missing: 30m)], Actor, Utc.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferMissingDisposition, missingDisposition.ErrorCode);
    }

    private static InventoryTransfer DispatchSingleLine(decimal sentQty)
    {
        var transfer = InventoryTransfer.CreateDraft(
            Org,
            BranchA,
            BranchB,
            [new InventoryTransferLineDraft(Coke, sentQty, "Coke", UnitOfMeasure.Piece)],
            Actor,
            Utc);
        transfer.Dispatch("260922-001", Actor, Utc.AddMinutes(1));
        return transfer;
    }

    [Fact]
    public void I_missing_request_replacement_increases_stock_request_remaining()
    {
        var transfer = DispatchSingleLine(100m);
        transfer.Receive(
            [Classify(Coke, good: 70m, missing: 30m, missingDisposition: InventoryTransferMissingDisposition.CloseMissing)],
            Actor,
            Utc.AddMinutes(2));

        var request = LinkedStockRequest(transfer, 100m);
        var coverage = StockRequestDispatchCoverage.Compute(request, [transfer]);
        Assert.Equal(30m, coverage.RemainingToDispatchByProduct[Coke.Value]);
    }

    [Fact]
    public void J_damaged_replacement_increases_stock_request_remaining()
    {
        var transfer = DispatchSingleLine(100m);
        transfer.Receive(
            [Classify(Coke, good: 70m, damaged: 30m)],
            Actor,
            Utc.AddMinutes(2));

        var request = LinkedStockRequest(transfer, 100m);
        var coverage = StockRequestDispatchCoverage.Compute(request, [transfer]);
        Assert.Equal(30m, coverage.RemainingToDispatchByProduct[Coke.Value]);
        Assert.Equal(0m, coverage.WaivedByProduct.GetValueOrDefault(Coke.Value));
    }

    [Fact]
    public void K_accept_shortage_waives_and_does_not_increase_remaining()
    {
        var transfer = DispatchSingleLine(100m);
        transfer.Receive(
            [
                Classify(
                    Coke,
                    good: 60m,
                    missing: 20m,
                    damaged: 20m,
                    missingDisposition: InventoryTransferMissingDisposition.AcceptShortage,
                    damagedFollowUp: InventoryTransferDiscrepancyFollowUp.AcceptShortage)
            ],
            Actor,
            Utc.AddMinutes(2));

        var line = transfer.Lines.Single();
        Assert.Equal(40m, line.WaivedQty);
        Assert.Equal(InventoryTransferStatus.ClosedWithDiscrepancy, transfer.Status);

        var request = LinkedStockRequest(transfer, 100m);
        var coverage = StockRequestDispatchCoverage.Compute(request, [transfer]);
        Assert.Equal(0m, coverage.RemainingToDispatchByProduct[Coke.Value]);
        Assert.Equal(40m, coverage.WaivedByProduct[Coke.Value]);
    }

    [Fact]
    public void L_other_wrong_item_closes_with_replacement_remaining()
    {
        var transfer = DispatchSingleLine(100m);
        transfer.Receive(
            [
                Classify(
                    Coke,
                    good: 99m,
                    other: 1m,
                    otherReasonCode: ReceiveDiscrepancyOtherReason.WrongItem)
            ],
            Actor,
            Utc.AddMinutes(2));

        var request = LinkedStockRequest(transfer, 100m);
        var coverage = StockRequestDispatchCoverage.Compute(request, [transfer]);
        Assert.Equal(1m, coverage.RemainingToDispatchByProduct[Coke.Value]);
        Assert.Equal(InventoryTransferDiscrepancyReason.WrongItem, transfer.Lines.Single().DiscrepancyReason);
    }

    private static StockRequest LinkedStockRequest(InventoryTransfer transfer, decimal approvedQty)
    {
        var request = StockRequest.Create(
            Org,
            BranchB,
            BranchA,
            [new StockRequestLineDraft(Coke, approvedQty, "Coke", UnitOfMeasure.Piece)],
            Actor,
            Utc,
            "260922-300");
        request.Approve(Actor, Utc.AddMinutes(1), new Dictionary<Guid, decimal> { [Coke.Value] = approvedQty });
        request.MarkDispatched(Actor, Utc.AddMinutes(1), transfer.Id.Value);
        return request;
    }

    private static InventoryTransferReceiveLineDraft Classify(
        CatalogProductId productId,
        decimal good = 0m,
        decimal damaged = 0m,
        decimal missing = 0m,
        decimal other = 0m,
        string? otherReasonCode = null,
        InventoryTransferMissingDisposition? missingDisposition = null,
        InventoryTransferDiscrepancyFollowUp? damagedFollowUp = null,
        InventoryTransferDiscrepancyFollowUp? otherFollowUp = null) =>
        new(
            productId,
            GoodQty: good,
            DamagedQty: damaged,
            MissingQty: missing,
            OtherQty: other,
            OtherReasonCode: otherReasonCode,
            MissingDisposition: missingDisposition,
            DamagedFollowUp: damaged > 0m
                ? damagedFollowUp ?? InventoryTransferDiscrepancyFollowUp.RequestReplacement
                : null,
            OtherFollowUp: other > 0m
                ? otherFollowUp ?? InventoryTransferDiscrepancyFollowUp.RequestReplacement
                : null);
}
