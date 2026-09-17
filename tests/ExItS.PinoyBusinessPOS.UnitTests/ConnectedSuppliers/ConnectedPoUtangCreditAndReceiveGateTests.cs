using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedPoUtangCreditAndReceiveGateTests
{
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PosOrganizationId Supplier = PosOrganizationId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Now = new(2026, 9, 16, 10, 0, 0, TimeSpan.Zero);
    private static readonly Guid ProductA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static ConnectedPurchaseOrder NewUtangOrder(decimal qty = 10m, decimal unit = 500m)
    {
        var relationship = ConnectedSupplierRelationship.Request(Buyer, Supplier, Now);
        relationship.Approve(Now.AddMinutes(1));
        var line = ConnectedPurchaseOrderLine.Create(
            CatalogProductId.From(ProductA), "Coke", null, qty, unit, "Piece");
        return ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-UTANG-1",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [line],
            Now.AddMinutes(2),
            paymentTerm: ConnectedPoPaymentTerm.Utang);
    }

    [Fact]
    public void Preparing_and_Accepted_cannot_receive_Shipped_can()
    {
        var order = NewUtangOrder();
        Assert.False(order.CanBuyerReceive);

        order.Accept(Now.AddMinutes(3));
        Assert.False(order.CanBuyerReceive);

        order.StartPreparing(Now.AddMinutes(4));
        Assert.False(order.CanBuyerReceive);
        Assert.Equal(ConnectedPoDisplayStatus.Preparing, ConnectedPoDisplayStatus.ForBuyer(BuyerPo(), order));

        order.MarkFulfilled(Now.AddMinutes(5));
        Assert.True(order.CanBuyerReceive);
        Assert.Equal(ConnectedPoDisplayStatus.Shipped, ConnectedPoDisplayStatus.ForBuyer(BuyerPo(), order));
    }

    [Fact]
    public void Draft_style_New_Utang_reserves_full_amount_until_posted_or_released()
    {
        var order = NewUtangOrder(qty: 10m, unit: 500m);
        Assert.Equal(5_000m, ConnectedPoUtangCredit.ActiveReservationAmount(order));

        order.Decline(Now.AddMinutes(3));
        Assert.Equal(0m, ConnectedPoUtangCredit.ActiveReservationAmount(order));
    }

    [Fact]
    public void Second_po_sees_reduced_available_when_first_is_reserved()
    {
        var first = NewUtangOrder(qty: 10m, unit: 500m);
        var secondBase = ConnectedPoUtangCredit.AvailableCredit(
            CustomerCreditPolicyStatus.Approved,
            creditLimit: 10_000m,
            outstandingUtang: 0m,
            activePoReservations: ConnectedPoUtangCredit.ActiveReservationAmount(first));
        Assert.Equal(5_000m, secondBase);
    }

    [Fact]
    public void Withdraw_releases_reservation()
    {
        var order = NewUtangOrder();
        Assert.Equal(5_000m, ConnectedPoUtangCredit.ActiveReservationAmount(order));
        order.WithdrawByBuyer(Now.AddMinutes(3));
        Assert.Equal(0m, ConnectedPoUtangCredit.ActiveReservationAmount(order));
    }

    [Fact]
    public void Partial_receive_splits_outstanding_posted_and_remaining_reservation()
    {
        var order = NewUtangOrder(qty: 10m, unit: 500m);
        order.Accept(Now.AddMinutes(3));
        order.MarkFulfilled(Now.AddMinutes(4));
        Assert.Equal(5_000m, ConnectedPoUtangCredit.ActiveReservationAmount(order));

        order.PostUtangCreditFromReceipt(2_000m, Now.AddMinutes(5));
        Assert.Equal(2_000m, order.CreditPostedAmount);
        Assert.Equal(3_000m, ConnectedPoUtangCredit.ActiveReservationAmount(order));

        order.PostUtangCreditFromReceipt(3_000m, Now.AddMinutes(6));
        Assert.Equal(5_000m, order.CreditPostedAmount);
        Assert.Equal(0m, ConnectedPoUtangCredit.ActiveReservationAmount(order));
    }

    [Fact]
    public void Available_formula_includes_reservations()
    {
        Assert.Equal(
            4_000m,
            ConnectedPoUtangCredit.AvailableCredit(
                CustomerCreditPolicyStatus.Approved,
                10_000m,
                outstandingUtang: 1_000m,
                activePoReservations: 5_000m));
    }

    private static PurchaseOrder BuyerPo()
    {
        var po = PurchaseOrder.CreateDraft(
            Buyer,
            SupplierId.New(),
            DateOnly.FromDateTime(Now.UtcDateTime),
            [new PurchaseOrderLineDraft(CatalogProductId.From(ProductA), 10m, 50m)],
            Now);
        po.Submit(
            "PO-20260916-000001",
            [new PurchaseOrderLineSnapshotInput(
                CatalogProductId.From(ProductA),
                "Coke",
                UnitOfMeasure.Piece,
                10m,
                50m)],
            Guid.Parse("99999999-9999-9999-9999-999999999999"),
            Now.AddMinutes(1));
        return po;
    }
}
