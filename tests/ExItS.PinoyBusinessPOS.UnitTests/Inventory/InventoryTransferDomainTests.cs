using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class InventoryTransferDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosBranchId BranchA = PosBranchId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PosBranchId BranchB = PosBranchId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly CatalogProductId Coke = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly CatalogProductId Sprite = CatalogProductId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly DateTimeOffset Utc = new(2026, 8, 13, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Transfer_number_formats()
    {
        Assert.Equal("TR-260813-001", InventoryTransferNumbers.Format(new DateOnly(2026, 8, 13), 1));
        Assert.Equal("260813-001", InventoryTransferNumbers.Normalize(" 260813-001 "));
        Assert.Equal("TR-260813-001-R1", InventoryTransferNumbers.FormatReplacement("TR-260813-001", 1));
        Assert.Equal("TR-260813-001-R1", InventoryTransferNumbers.Normalize(" tr-260813-001-r1 "));
    }

    [Fact]
    public void Draft_rejects_same_branch_and_duplicate_product()
    {
        var same = Assert.Throws<DomainException>(() =>
            InventoryTransfer.CreateDraft(Org, BranchA, BranchA, [Line(Coke, 1m, "Coke")], Actor, Utc));
        Assert.Equal(DomainErrorCodes.InventoryTransferSameBranch, same.ErrorCode);

        var dup = Assert.Throws<DomainException>(() =>
            InventoryTransfer.CreateDraft(
                Org,
                BranchA,
                BranchB,
                [Line(Coke, 1m, "Coke"), Line(Coke, 2m, "Coke")],
                Actor,
                Utc));
        Assert.Equal(DomainErrorCodes.InventoryTransferDuplicateProduct, dup.ErrorCode);
    }

    [Fact]
    public void Draft_rejects_zero_and_negative_quantity()
    {
        var zero = Assert.Throws<DomainException>(() =>
            InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, 0m, "Coke")], Actor, Utc));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferQuantity, zero.ErrorCode);

        var negative = Assert.Throws<DomainException>(() =>
            InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, -1m, "Coke")], Actor, Utc));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferQuantity, negative.ErrorCode);
    }

    [Fact]
    public void Dispatch_then_partial_wave_allows_further_receive_without_discrepancy()
    {
        var transfer = InventoryTransfer.CreateDraft(
            Org,
            BranchA,
            BranchB,
            [Line(Coke, 20m, "Coke"), Line(Sprite, 10m, "Sprite")],
            Actor,
            Utc);
        transfer.Dispatch("260813-123", Actor, Utc.AddMinutes(1));
        Assert.Equal(InventoryTransferStatus.InTransit, transfer.Status);

        var receipt1 = transfer.Receive(
            [
                new InventoryTransferReceiveLineDraft(Coke, 20m),
                new InventoryTransferReceiveLineDraft(Sprite, 8m)
            ],
            Actor,
            Utc.AddMinutes(2));

        Assert.Equal(1, receipt1.Sequence);
        Assert.Equal(2, receipt1.Lines.Count);
        Assert.Equal(InventoryTransferStatus.PartiallyReceived, transfer.Status);
        Assert.Equal(20m, transfer.Lines[0].SentQty);
        Assert.Equal(20m, transfer.Lines[0].ReceivedQty);
        Assert.Equal(10m, transfer.Lines[1].SentQty);
        Assert.Equal(8m, transfer.Lines[1].ReceivedQty);
        Assert.Equal(2m, transfer.Lines[1].DifferenceQty);
        Assert.Equal(2m, transfer.Lines[1].OutstandingQty);
        Assert.Equal("Short", transfer.Lines[1].LineStatus);
        Assert.Null(transfer.Lines[1].DiscrepancyReason);
        Assert.Single(transfer.Receipts);

        var receipt2 = transfer.Receive(
            [new InventoryTransferReceiveLineDraft(Sprite, 2m)],
            Actor,
            Utc.AddMinutes(3));

        Assert.Equal(2, receipt2.Sequence);
        Assert.Equal(InventoryTransferStatus.Received, transfer.Status);
        Assert.Equal(10m, transfer.Lines[1].ReceivedQty);
        Assert.Equal(0m, transfer.Lines[1].OutstandingQty);
        Assert.Equal("Received", transfer.Lines[1].LineStatus);
        Assert.Equal(2, transfer.Receipts.Count);

        var again = Assert.Throws<DomainException>(() =>
            transfer.Receive(
                [new InventoryTransferReceiveLineDraft(Coke, 1m)],
                Actor,
                Utc.AddMinutes(4)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferStatusTransition, again.ErrorCode);

        var edit = Assert.Throws<DomainException>(() =>
            transfer.UpdateDraft([Line(Coke, 99m, "Coke")], Utc.AddMinutes(5)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferStatusTransition, edit.ErrorCode);
    }

    [Fact]
    public void Receive_wave_can_omit_lines_and_ignores_zero_qty()
    {
        var transfer = InventoryTransfer.CreateDraft(
            Org,
            BranchA,
            BranchB,
            [Line(Coke, 10m, "Coke"), Line(Sprite, 5m, "Sprite")],
            Actor,
            Utc);
        transfer.Dispatch("260813-001", Actor, Utc.AddMinutes(1));

        var receipt = transfer.Receive(
            [
                new InventoryTransferReceiveLineDraft(Coke, 4m),
                new InventoryTransferReceiveLineDraft(Sprite, 0m)
            ],
            Actor,
            Utc.AddMinutes(2));

        Assert.Single(receipt.Lines);
        Assert.Equal(Coke, receipt.Lines[0].ProductId);
        Assert.Equal(4m, receipt.Lines[0].QuantityReceived);
        Assert.Equal(InventoryTransferStatus.PartiallyReceived, transfer.Status);
        Assert.Equal(4m, transfer.Lines[0].ReceivedQty);
        Assert.Equal(0m, transfer.Lines[1].ReceivedQty);
        Assert.Equal("Missing", transfer.Lines[1].LineStatus);
    }

    [Fact]
    public void Close_remainder_requires_discrepancy_and_sets_closed_with_discrepancy()
    {
        var transfer = InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, 10m, "Coke")], Actor, Utc);
        transfer.Dispatch("260813-010", Actor, Utc.AddMinutes(1));
        transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 7m)], Actor, Utc.AddMinutes(2));

        var missingReason = Assert.Throws<DomainException>(() =>
            transfer.CloseRemainder(Actor, Utc.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferDiscrepancyReason, missingReason.ErrorCode);

        transfer.CloseRemainder(
            Actor,
            Utc.AddMinutes(4),
            transferLevelReason: InventoryTransferDiscrepancyReason.LostInTransit,
            transferLevelNote: "trailer short");

        Assert.Equal(InventoryTransferStatus.ClosedWithDiscrepancy, transfer.Status);
        Assert.Equal(Utc.AddMinutes(4), transfer.ClosedAtUtc);
        Assert.Equal(Actor, transfer.ClosedBy);
        Assert.Equal(7m, transfer.Lines[0].ReceivedQty);
        Assert.Equal(3m, transfer.Lines[0].ClosedQty);
        Assert.Equal(0m, transfer.Lines[0].OutstandingQty);
        Assert.Equal(3m, transfer.Lines[0].DifferenceQty);
        Assert.Equal("ClosedShort", transfer.Lines[0].LineStatus);
        Assert.Equal(InventoryTransferDiscrepancyReason.LostInTransit, transfer.Lines[0].DiscrepancyReason);
        Assert.Equal("trailer short", transfer.Lines[0].DiscrepancyNote);

        var receiveAfterClose = Assert.Throws<DomainException>(() =>
            transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 1m)], Actor, Utc.AddMinutes(5)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferStatusTransition, receiveAfterClose.ErrorCode);
    }

    [Fact]
    public void Close_remainder_with_zero_received_line_is_missing()
    {
        var transfer = InventoryTransfer.CreateDraft(
            Org,
            BranchA,
            BranchB,
            [Line(Coke, 10m, "Coke"), Line(Sprite, 5m, "Sprite")],
            Actor,
            Utc);
        transfer.Dispatch("260813-011", Actor, Utc.AddMinutes(1));
        transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 10m)], Actor, Utc.AddMinutes(2));

        transfer.CloseRemainder(
            Actor,
            Utc.AddMinutes(3),
            closeLines:
            [
                new InventoryTransferCloseRemainderLineDraft(
                    InventoryTransferDiscrepancyReason.LostInTransit,
                    ProductId: Sprite)
            ]);

        Assert.Equal(InventoryTransferStatus.ClosedWithDiscrepancy, transfer.Status);
        Assert.Equal(0m, transfer.Lines[1].ReceivedQty);
        Assert.Equal(5m, transfer.Lines[1].ClosedQty);
        Assert.Equal("Missing", transfer.Lines[1].LineStatus);
    }

    [Fact]
    public void Partial_receive_does_not_require_discrepancy_reason()
    {
        var transfer = InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, 5m, "Coke")], Actor, Utc);
        transfer.Dispatch("260813-004", Actor, Utc.AddMinutes(1));

        var receipt = transfer.Receive(
            [new InventoryTransferReceiveLineDraft(Coke, 4m)],
            Actor,
            Utc.AddMinutes(2));

        Assert.NotNull(receipt);
        Assert.Equal(InventoryTransferStatus.PartiallyReceived, transfer.Status);
        Assert.Null(transfer.Lines[0].DiscrepancyReason);
    }

    [Fact]
    public void Cancelled_in_transit_cannot_be_received()
    {
        var transfer = InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, 5m, "Coke")], Actor, Utc);
        transfer.Dispatch("260813-002", Actor, Utc.AddMinutes(1));
        transfer.Cancel(Actor, Utc.AddMinutes(2));

        var ex = Assert.Throws<DomainException>(() =>
            transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 5m)], Actor, Utc.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Cancel_blocked_after_any_receipt()
    {
        var transfer = InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, 5m, "Coke")], Actor, Utc);
        transfer.Dispatch("260813-020", Actor, Utc.AddMinutes(1));
        transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 2m)], Actor, Utc.AddMinutes(2));

        var ex = Assert.Throws<DomainException>(() => transfer.Cancel(Actor, Utc.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Receive_rejects_qty_above_outstanding()
    {
        var transfer = InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, 5m, "Coke")], Actor, Utc);
        transfer.Dispatch("260813-003", Actor, Utc.AddMinutes(1));
        transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 3m)], Actor, Utc.AddMinutes(2));

        var ex = Assert.Throws<DomainException>(() =>
            transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 3m)], Actor, Utc.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferReceiveQty, ex.ErrorCode);
    }

    [Fact]
    public void Receive_rejects_qty_above_sent_on_first_wave()
    {
        var transfer = InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, 5m, "Coke")], Actor, Utc);
        transfer.Dispatch("260813-003", Actor, Utc.AddMinutes(1));
        var ex = Assert.Throws<DomainException>(() =>
            transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 6m)], Actor, Utc.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferReceiveQty, ex.ErrorCode);
    }

    [Fact]
    public void Status_codes_include_closed_with_discrepancy()
    {
        Assert.Contains(nameof(InventoryTransferStatus.ClosedWithDiscrepancy), InventoryTransferStatuses.Codes);
        Assert.True(InventoryTransferStatuses.TryParse("ClosedWithDiscrepancy", out var status));
        Assert.Equal(InventoryTransferStatus.ClosedWithDiscrepancy, status);
        Assert.Equal(5, (int)InventoryTransferStatus.ClosedWithDiscrepancy);
    }

    private static InventoryTransferLineDraft Line(CatalogProductId productId, decimal qty, string name) =>
        new(productId, qty, name, UnitOfMeasure.Piece);
}
