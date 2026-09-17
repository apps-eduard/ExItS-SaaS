using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedPoInventoryReservationDomainTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosBranchId Branch =
        PosBranchId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Temporary_create_release_and_confirm_behave()
    {
        var productId = CatalogProductId.New();
        var orderId = ConnectedPurchaseOrderId.New();
        var expires = Now.AddHours(24);
        var row = ConnectedPoInventoryReservation.CreateTemporary(
            Org, Branch, productId, orderId, revision: 1, quantity: 5m, Now, expires);

        Assert.Equal(ConnectedPoReservationType.TemporaryProposal, row.Type);
        Assert.Equal(ConnectedPoReservationStatus.Active, row.Status);
        Assert.Equal(5m, row.RemainingQuantity);
        Assert.Equal(expires, row.ExpiresAtUtc);

        row.ConfirmFromTemporary(Now.AddMinutes(1));
        Assert.Equal(ConnectedPoReservationType.ConfirmedOrder, row.Type);
        Assert.Null(row.ExpiresAtUtc);
        Assert.Equal(ConnectedPoReservationStatus.Active, row.Status);

        row.Release(Now.AddMinutes(2));
        Assert.Equal(ConnectedPoReservationStatus.Released, row.Status);
        Assert.Equal(0m, row.RemainingQuantity);
    }

    [Fact]
    public void Consume_reduces_remaining_then_marks_consumed()
    {
        var row = ConnectedPoInventoryReservation.CreateConfirmed(
            Org, Branch, CatalogProductId.New(), ConnectedPurchaseOrderId.New(), 1, 4m, Now);
        row.Consume(1.5m);
        Assert.Equal(2.5m, row.RemainingQuantity);
        Assert.Equal(ConnectedPoReservationStatus.Active, row.Status);
        row.Consume(2.5m);
        Assert.Equal(0m, row.RemainingQuantity);
        Assert.Equal(ConnectedPoReservationStatus.Consumed, row.Status);
    }

    [Fact]
    public void Expire_only_for_active_temporary()
    {
        var row = ConnectedPoInventoryReservation.CreateTemporary(
            Org, Branch, CatalogProductId.New(), ConnectedPurchaseOrderId.New(), 1, 2m, Now, Now.AddHours(1));
        row.Expire(Now.AddHours(2));
        Assert.Equal(ConnectedPoReservationStatus.Expired, row.Status);
        Assert.Equal(0m, row.RemainingQuantity);
    }

    [Fact]
    public void Connected_purchase_order_inventory_marks_and_revision()
    {
        var relationship = ConnectedSupplierRelationship.Request(
            Org,
            PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
            Now,
            supplierBranchId: Branch.Value,
            supplierBranchName: "Main");
        relationship.Approve(Now.AddMinutes(1));
        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-1",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [ConnectedPurchaseOrderLine.Create(CatalogProductId.New(), "Item", "SKU", 3m, 10m, "Piece")],
            Now.AddMinutes(2));

        Assert.Equal(ConnectedPoInventoryReservationState.None, order.InventoryReservationState);
        Assert.Equal(0, order.InventoryReservationRevision);

        order.ProposeLineChanges(
            [new ConnectedPoLineProposal(order.Lines[0].ProductId, 2m, false, null)],
            Now.AddMinutes(3));
        Assert.Equal(1, order.InventoryReservationRevision);
        Assert.Equal(ConnectedPurchaseOrderStatus.ChangesProposed, order.Status);

        order.MarkInventoryTemporaryHold(Now.AddHours(24), Now.AddMinutes(3));
        Assert.Equal(ConnectedPoInventoryReservationState.TemporaryProposal, order.InventoryReservationState);
        Assert.NotNull(order.InventoryReservationExpiresAtUtc);

        order.MarkInventoryConfirmed(Now.AddMinutes(4));
        Assert.Equal(ConnectedPoInventoryReservationState.Confirmed, order.InventoryReservationState);
        Assert.Null(order.InventoryReservationExpiresAtUtc);

        order.MarkInventoryConsumed(Now.AddMinutes(5));
        Assert.Equal(ConnectedPoInventoryReservationState.Consumed, order.InventoryReservationState);
    }

    [Fact]
    public void IsEffectivelyActive_excludes_time_expired_released_and_zero_remaining()
    {
        var productId = CatalogProductId.New();
        var orderId = ConnectedPurchaseOrderId.New();
        var active = ConnectedPoInventoryReservation.CreateTemporary(
            Org, Branch, productId, orderId, 1, 2m, Now, Now.AddHours(1));
        Assert.True(active.IsEffectivelyActive(Now));
        Assert.False(active.IsEffectivelyActive(Now.AddHours(1)));
        Assert.False(active.IsEffectivelyActive(Now.AddHours(2)));

        var released = ConnectedPoInventoryReservation.CreateConfirmed(
            Org, Branch, productId, orderId, 1, 3m, Now);
        released.Release(Now.AddMinutes(1));
        Assert.False(released.IsEffectivelyActive(Now.AddMinutes(2)));

        var confirmed = ConnectedPoInventoryReservation.CreateConfirmed(
            Org, Branch, productId, ConnectedPurchaseOrderId.New(), 1, 4m, Now);
        Assert.True(confirmed.IsEffectivelyActive(Now.AddDays(30)));
    }

    [Fact]
    public void Available_formula_on_hand_minus_active_reservations()
    {
        const decimal onHand = 3m;
        var reserved = ConnectedPoInventoryReservation.CreateTemporary(
            Org, Branch, CatalogProductId.New(), ConnectedPurchaseOrderId.New(), 1, 2m, Now, Now.AddHours(24));
        Assert.True(reserved.IsEffectivelyActive(Now));
        var available = Math.Max(0m, onHand - reserved.RemainingQuantity);
        Assert.Equal(1m, available);
        Assert.Equal(3m, onHand);
    }
}
