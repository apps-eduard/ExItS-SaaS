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
        Assert.Equal("TR-20260813-000001", InventoryTransferNumbers.Format(new DateOnly(2026, 8, 13), 1));
        Assert.Equal("TR-20260813-000001", InventoryTransferNumbers.Normalize(" tr-20260813-000001 "));
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
    public void Dispatch_then_partial_receive_keeps_sent_qty_immutable()
    {
        var transfer = InventoryTransfer.CreateDraft(
            Org,
            BranchA,
            BranchB,
            [Line(Coke, 20m, "Coke"), Line(Sprite, 10m, "Sprite")],
            Actor,
            Utc);
        transfer.Dispatch("TR-20260813-000123", Actor, Utc.AddMinutes(1));
        Assert.Equal(InventoryTransferStatus.InTransit, transfer.Status);

        transfer.Receive(
            [
                new InventoryTransferReceiveLineDraft(Coke, 20m),
                new InventoryTransferReceiveLineDraft(Sprite, 8m, InventoryTransferDiscrepancyReason.ShortShipment)
            ],
            Actor,
            Utc.AddMinutes(2));

        Assert.Equal(InventoryTransferStatus.PartiallyReceived, transfer.Status);
        Assert.Equal(20m, transfer.Lines[0].SentQty);
        Assert.Equal(20m, transfer.Lines[0].ReceivedQty);
        Assert.Equal(10m, transfer.Lines[1].SentQty);
        Assert.Equal(8m, transfer.Lines[1].ReceivedQty);
        Assert.Equal(2m, transfer.Lines[1].DifferenceQty);
        Assert.Equal("Short", transfer.Lines[1].LineStatus);

        var again = Assert.Throws<DomainException>(() =>
            transfer.Receive(
                [new InventoryTransferReceiveLineDraft(Coke, 20m), new InventoryTransferReceiveLineDraft(Sprite, 8m)],
                Actor,
                Utc.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferStatusTransition, again.ErrorCode);

        var edit = Assert.Throws<DomainException>(() =>
            transfer.UpdateDraft([Line(Coke, 99m, "Coke")], Utc.AddMinutes(4)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferStatusTransition, edit.ErrorCode);
    }

    [Fact]
    public void Zero_received_line_is_missing_not_rejected_product()
    {
        var transfer = InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, 10m, "Coke")], Actor, Utc);
        transfer.Dispatch("TR-20260813-000001", Actor, Utc.AddMinutes(1));
        transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 0m)], Actor, Utc.AddMinutes(2));

        Assert.Equal(InventoryTransferStatus.PartiallyReceived, transfer.Status);
        Assert.Equal(0m, transfer.Lines[0].ReceivedQty);
        Assert.Equal(10m, transfer.Lines[0].DifferenceQty);
        Assert.Equal("Missing", transfer.Lines[0].LineStatus);
    }

    [Fact]
    public void Cancelled_in_transit_cannot_be_received()
    {
        var transfer = InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, 5m, "Coke")], Actor, Utc);
        transfer.Dispatch("TR-20260813-000002", Actor, Utc.AddMinutes(1));
        transfer.Cancel(Actor, Utc.AddMinutes(2));

        var ex = Assert.Throws<DomainException>(() =>
            transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 5m)], Actor, Utc.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Receive_rejects_qty_above_sent()
    {
        var transfer = InventoryTransfer.CreateDraft(Org, BranchA, BranchB, [Line(Coke, 5m, "Coke")], Actor, Utc);
        transfer.Dispatch("TR-20260813-000003", Actor, Utc.AddMinutes(1));
        var ex = Assert.Throws<DomainException>(() =>
            transfer.Receive([new InventoryTransferReceiveLineDraft(Coke, 6m)], Actor, Utc.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidInventoryTransferReceiveQty, ex.ErrorCode);
    }

    private static InventoryTransferLineDraft Line(CatalogProductId productId, decimal qty, string name) =>
        new(productId, qty, name, UnitOfMeasure.Piece);
}
