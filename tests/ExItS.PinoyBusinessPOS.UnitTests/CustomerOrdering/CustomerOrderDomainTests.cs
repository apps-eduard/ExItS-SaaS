using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class CustomerOrderDomainTests
{
    private static readonly PosOrganizationId Seller =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product =
        CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid BranchId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid PlatformUser = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid BuyerOrg = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    private static readonly DateTimeOffset Utc = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Personal_party_can_place_pickup_order()
    {
        var party = CustomerOrderParty.Personal(PlatformUser, "Ana Reyes");
        var order = CreatePickup(party);

        Assert.Equal(CustomerOrderStatus.Submitted, order.Status);
        Assert.Equal(CustomerPartyType.Personal, order.CustomerParty.PartyType);
        Assert.Equal(PlatformUser, order.CustomerParty.PlatformUserId);
        Assert.Equal("SO-000001", order.OrderNumber);
        Assert.Equal(50m, order.MerchandiseSubtotal);
        Assert.Equal(0m, order.DeliveryFee);
        Assert.Equal(50m, order.Total);
        Assert.Null(order.DeliverySnapshot);
        Assert.Equal(CustomerOrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Equal(CustomerOrderPaymentMethod.Cash, order.PaymentMethod);
    }

    [Fact]
    public void Organization_party_can_place_delivery_order()
    {
        var party = CustomerOrderParty.Organization(BuyerOrg, "ORG000123", "Corner Store");
        var snapshot = FreeDeliverySnapshot();
        var order = CustomerOrder.CreateSubmitted(
            Seller,
            "SO-000002",
            party,
            CustomerOrderFulfillmentType.Delivery,
            BranchId,
            "Main Branch",
            [Line(10m, 25m)],
            Actor,
            Utc,
            snapshot);

        Assert.Equal(CustomerPartyType.Organization, order.CustomerParty.PartyType);
        Assert.Equal("ORG000123", order.CustomerParty.BuyerPublicOrganizationId);
        Assert.Equal(0m, order.DeliveryFee);
        Assert.True(order.DeliverySnapshot!.FreeDeliveryApplied);
        Assert.Equal(250m, order.Total);
    }

    [Fact]
    public void Invalid_party_is_rejected()
    {
        var emptyPersonal = Assert.Throws<DomainException>(() =>
            CustomerOrderParty.Personal(Guid.Empty, "Name"));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderParty, emptyPersonal.ErrorCode);

        var badOrg = Assert.Throws<DomainException>(() =>
            CustomerOrderParty.Organization(BuyerOrg, "STORE1", "Name"));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderParty, badOrg.ErrorCode);

        var mixed = CustomerOrderParty.Rehydrate(
            CustomerPartyType.Personal,
            "Name",
            PlatformUser,
            BuyerOrg,
            "ORG000001");
        var inconsistent = Assert.Throws<DomainException>(() => mixed.EnsureConsistent());
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderParty, inconsistent.ErrorCode);
    }

    [Fact]
    public void Line_price_snapshots_are_immutable()
    {
        var order = CreatePickup(CustomerOrderParty.Personal(PlatformUser, "Ana"));
        var line = order.Lines.Single();

        Assert.Null(typeof(CustomerOrderLine).GetProperty(nameof(CustomerOrderLine.UnitPrice))!.SetMethod);
        Assert.Null(typeof(CustomerOrderLine).GetProperty(nameof(CustomerOrderLine.Quantity))!.SetMethod);
        Assert.Null(typeof(CustomerOrderLine).GetProperty(nameof(CustomerOrderLine.LineTotal))!.SetMethod);
        Assert.Equal(25m, line.UnitPrice);
        Assert.Equal(2m, line.Quantity);
        Assert.Equal(50m, line.LineTotal);
    }

    [Fact]
    public void Pickup_flow_completes()
    {
        var order = CreatePickup(CustomerOrderParty.Personal(PlatformUser, "Ana"));
        order.Accept(Actor, Utc.AddMinutes(1));
        Assert.Equal(CustomerOrderStatus.Accepted, order.Status);
        Assert.Equal(CustomerOrderFulfillmentStatus.Preparing, order.FulfillmentStatus);
        Assert.Equal(CustomerOrderStockReservationState.None, order.StockReservationState);

        order.StartPreparing(Utc.AddMinutes(2), Actor);
        order.MarkReady(Utc.AddMinutes(3), Actor);
        Assert.Equal(CustomerOrderFulfillmentStatus.ReadyForPickup, order.FulfillmentStatus);
        Assert.Equal(Actor, order.ReadyBy);
        Assert.Equal(Utc.AddMinutes(3), order.ReadyAtUtc);

        order.MarkCollected(Utc.AddMinutes(4), Actor);
        Assert.Equal(Actor, order.CollectedBy);
        order.Complete(Actor, Utc.AddMinutes(5));
        Assert.Equal(CustomerOrderStatus.Completed, order.Status);
        Assert.Equal(CustomerOrderFulfillmentStatus.Collected, order.FulfillmentStatus);
    }

    [Fact]
    public void Delivery_flow_completes()
    {
        var order = CreateDelivery(fee: 40m);
        order.Accept(Actor, Utc.AddMinutes(1));
        order.MarkReady(Utc.AddMinutes(2), Actor);
        Assert.Equal(CustomerOrderFulfillmentStatus.Ready, order.FulfillmentStatus);
        Assert.Equal(Actor, order.ReadyBy);

        order.MarkOutForDelivery(Utc.AddMinutes(3), Actor);
        Assert.Equal(Actor, order.OutForDeliveryBy);
        order.MarkDelivered(Utc.AddMinutes(4), Actor);
        Assert.Equal(Actor, order.DeliveredBy);
        order.Complete(Actor, Utc.AddMinutes(5));

        Assert.Equal(CustomerOrderStatus.Completed, order.Status);
        Assert.Equal(CustomerOrderFulfillmentStatus.Delivered, order.FulfillmentStatus);
        Assert.Equal(90m, order.Total);
    }

    [Fact]
    public void Reject_and_cancel_rules()
    {
        var rejectable = CreatePickup(CustomerOrderParty.Personal(PlatformUser, "Ana"));
        rejectable.Reject(CustomerOrderRejectReason.OutOfStock, "No rice", Actor, Utc.AddMinutes(1));
        Assert.Equal(CustomerOrderStatus.Rejected, rejectable.Status);
        Assert.Equal(CustomerOrderRejectReason.OutOfStock, rejectable.RejectReason);

        var cancellable = CreatePickup(CustomerOrderParty.Personal(PlatformUser, "Ana"));
        cancellable.Cancel(Actor, Utc.AddMinutes(1));
        Assert.Equal(CustomerOrderStatus.Cancelled, cancellable.Status);

        var accepted = CreatePickup(CustomerOrderParty.Personal(PlatformUser, "Ana"));
        accepted.Accept(Actor, Utc.AddMinutes(1));
        var cancelAfterAccept = Assert.Throws<DomainException>(() =>
            accepted.Cancel(Actor, Utc.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderStatusTransition, cancelAfterAccept.ErrorCode);

        var rejectAfterAccept = Assert.Throws<DomainException>(() =>
            accepted.Reject(CustomerOrderRejectReason.StoreTooBusy, null, Actor, Utc.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderStatusTransition, rejectAfterAccept.ErrorCode);
    }

    [Fact]
    public void Invalid_transitions_are_rejected()
    {
        var pickup = CreatePickup(CustomerOrderParty.Personal(PlatformUser, "Ana"));
        var outForDelivery = Assert.Throws<DomainException>(() =>
            pickup.MarkOutForDelivery(Utc.AddMinutes(1), Actor));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderFulfillmentTransition, outForDelivery.ErrorCode);

        pickup.Accept(Actor, Utc.AddMinutes(1));
        pickup.MarkReady(Utc.AddMinutes(2), Actor);
        var deliveryOnly = Assert.Throws<DomainException>(() =>
            pickup.MarkOutForDelivery(Utc.AddMinutes(3), Actor));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderFulfillmentTransition, deliveryOnly.ErrorCode);

        var delivery = CreateDelivery(fee: 10m);
        delivery.Accept(Actor, Utc.AddMinutes(1));
        delivery.MarkReady(Utc.AddMinutes(2), Actor);
        var collectOnDelivery = Assert.Throws<DomainException>(() =>
            delivery.MarkCollected(Utc.AddMinutes(3), Actor));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderFulfillmentTransition, collectOnDelivery.ErrorCode);
    }

    [Fact]
    public void Free_delivery_fee_is_zero_on_snapshot()
    {
        var snapshot = FreeDeliverySnapshot();
        Assert.Equal(0m, snapshot.FinalDeliveryFee);
        Assert.True(snapshot.FreeDeliveryApplied);

        var bad = Assert.Throws<DomainException>(() =>
            CustomerOrderDeliverySnapshot.Create(
                "Recipient",
                "09171234567",
                "123 Street",
                null,
                "Manila",
                null,
                14.6m,
                121.0m,
                14.5m,
                121.0m,
                1.2m,
                0m,
                50m,
                2m,
                10m,
                10m,
                200m,
                distanceCharge: 0m,
                finalDeliveryFee: 25m,
                freeDeliveryApplied: true));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderDeliveryFee, bad.ErrorCode);
    }

    [Fact]
    public void Manipulated_totals_rejected_at_create()
    {
        var party = CustomerOrderParty.Personal(PlatformUser, "Ana");

        var pickupWithDelivery = Assert.Throws<DomainException>(() =>
            CustomerOrder.CreateSubmitted(
                Seller,
                "SO-000010",
                party,
                CustomerOrderFulfillmentType.Pickup,
                BranchId,
                "Main Branch",
                [Line()],
                Actor,
                Utc,
                DeliverySnapshot(fee: 10m)));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderDelivery, pickupWithDelivery.ErrorCode);

        var deliveryWithoutSnapshot = Assert.Throws<DomainException>(() =>
            CustomerOrder.CreateSubmitted(
                Seller,
                "SO-000011",
                party,
                CustomerOrderFulfillmentType.Delivery,
                BranchId,
                "Main Branch",
                [Line()],
                Actor,
                Utc,
                deliverySnapshot: null));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderDelivery, deliveryWithoutSnapshot.ErrorCode);

        var discountTooLarge = Assert.Throws<DomainException>(() =>
            CustomerOrder.CreateSubmitted(
                Seller,
                "SO-000012",
                party,
                CustomerOrderFulfillmentType.Pickup,
                BranchId,
                "Main Branch",
                [new CustomerOrderLineDraft(Product, "Rice", "SKU-1", UnitOfMeasure.Piece, 1m, 10m, Discount: 11m)],
                Actor,
                Utc));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderLine, discountTooLarge.ErrorCode);
    }

    [Fact]
    public void Order_numbers_format_so_sequence()
    {
        Assert.Equal("SO-000001", CustomerOrderNumbers.Format(1));
        Assert.Equal("SO-000042", CustomerOrderNumbers.Normalize(" so-000042 "));
    }

    [Theory]
    [InlineData(null, CustomerOrderPaymentMethod.Cash)]
    [InlineData("", CustomerOrderPaymentMethod.Cash)]
    [InlineData("Cash", CustomerOrderPaymentMethod.Cash)]
    [InlineData("GCash", CustomerOrderPaymentMethod.ManualGCash)]
    [InlineData("ManualGCash", CustomerOrderPaymentMethod.ManualGCash)]
    [InlineData("Utang", CustomerOrderPaymentMethod.Utang)]
    public void Payment_method_parse_accepts_v1_values(string? raw, CustomerOrderPaymentMethod expected)
    {
        Assert.Equal(expected, CustomerOrderPaymentMethods.Parse(raw));
        Assert.Equal("GCash", CustomerOrderPaymentMethods.ToUiLabel(CustomerOrderPaymentMethod.ManualGCash));
    }

    [Fact]
    public void Invalid_payment_method_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => CustomerOrderPaymentMethods.Parse("Card"));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPaymentMethod, ex.ErrorCode);
    }

    [Theory]
    [InlineData(CustomerOrderPaymentMethod.Cash)]
    [InlineData(CustomerOrderPaymentMethod.ManualGCash)]
    [InlineData(CustomerOrderPaymentMethod.Utang)]
    public void Submitted_orders_remain_unpaid_for_all_manual_methods(CustomerOrderPaymentMethod method)
    {
        var order = CustomerOrder.CreateSubmitted(
            Seller,
            "SO-000020",
            CustomerOrderParty.Personal(PlatformUser, "Ana Reyes"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [Line()],
            Actor,
            Utc,
            paymentMethod: method);

        Assert.Equal(method, order.PaymentMethod);
        Assert.Equal(CustomerOrderPaymentStatus.Unpaid, order.PaymentStatus);
    }

    private static CustomerOrder CreatePickup(CustomerOrderParty party) =>
        CustomerOrder.CreateSubmitted(
            Seller,
            "SO-000001",
            party,
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [Line()],
            Actor,
            Utc);

    private static CustomerOrder CreateDelivery(decimal fee) =>
        CustomerOrder.CreateSubmitted(
            Seller,
            "SO-000003",
            CustomerOrderParty.Organization(BuyerOrg, "ORG000123", "Corner Store"),
            CustomerOrderFulfillmentType.Delivery,
            BranchId,
            "Main Branch",
            [Line()],
            Actor,
            Utc,
            DeliverySnapshot(fee));

    private static CustomerOrderLineDraft Line(decimal quantity = 2m, decimal unitPrice = 25m) =>
        new(Product, "Rice 25kg", "SKU-RICE", UnitOfMeasure.Piece, quantity, unitPrice);

    private static CustomerOrderDeliverySnapshot FreeDeliverySnapshot() =>
        CustomerOrderDeliverySnapshot.Create(
            "Recipient",
            "09171234567",
            "123 Street",
            null,
            "Manila",
            null,
            14.6m,
            121.0m,
            14.5m,
            121.0m,
            1.2m,
            0m,
            50m,
            2m,
            10m,
            10m,
            200m,
            distanceCharge: 0m,
            finalDeliveryFee: 0m,
            freeDeliveryApplied: true);

    private static CustomerOrderDeliverySnapshot DeliverySnapshot(decimal fee) =>
        CustomerOrderDeliverySnapshot.Create(
            "Recipient",
            "09171234567",
            "123 Street",
            "Unit 2",
            "Manila",
            "Leave at gate",
            14.6m,
            121.0m,
            14.5m,
            121.0m,
            3.5m,
            0m,
            fee,
            2m,
            10m,
            15m,
            null,
            distanceCharge: 0m,
            finalDeliveryFee: fee,
            freeDeliveryApplied: false);
}
