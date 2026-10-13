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
        Assert.Equal("ORD-260816-001", order.OrderNumber);
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
            "ORD-260816-002",
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

        order.ConfirmPayment(order.Total, Actor, Utc.AddMinutes(4));
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
                "ORD-260816-010",
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
                "ORD-260816-011",
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
                "ORD-260816-012",
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
    public void Order_numbers_format_daily_sequence()
    {
        var day = new DateOnly(2026, 8, 16);
        Assert.Equal("ORD-260816-001", CustomerOrderNumbers.Format(day, 1));
        Assert.Equal("260816-042", CustomerOrderNumbers.Normalize(" 260816-042 "));
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
    [InlineData(CustomerOrderPaymentMethod.Cash, CustomerOrderPaymentStatus.Unpaid)]
    [InlineData(CustomerOrderPaymentMethod.ManualGCash, CustomerOrderPaymentStatus.Pending)]
    [InlineData(CustomerOrderPaymentMethod.Utang, CustomerOrderPaymentStatus.Unpaid)]
    public void Submitted_orders_start_with_the_method_payment_status(
        CustomerOrderPaymentMethod method,
        CustomerOrderPaymentStatus expectedStatus)
    {
        var order = CustomerOrder.CreateSubmitted(
            Seller,
            "ORD-260816-020",
            CustomerOrderParty.Personal(PlatformUser, "Ana Reyes"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [Line()],
            Actor,
            Utc,
            paymentMethod: method,
            paymentReference: method == CustomerOrderPaymentMethod.ManualGCash ? "  GCASH-1001  " : null);

        Assert.Equal(method, order.PaymentMethod);
        Assert.Equal(expectedStatus, order.PaymentStatus);
        Assert.Null(order.AmountReceived);
        Assert.Null(order.ChangeAmount);
        Assert.Null(order.PaymentConfirmedAtUtc);
        Assert.Equal(
            method == CustomerOrderPaymentMethod.ManualGCash ? "GCASH-1001" : null,
            order.PaymentReference);
    }

    [Fact]
    public void Fulfillment_does_not_mark_cash_paid()
    {
        var order = CreatePickup(CustomerOrderParty.Personal(PlatformUser, "Ana"));
        order.Accept(Actor, Utc.AddMinutes(1));
        order.MarkReady(Utc.AddMinutes(2), Actor);
        var unpaid = Assert.Throws<DomainException>(() => order.MarkCollected(Utc.AddMinutes(3), Actor));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPaymentConfirmation, unpaid.ErrorCode);
        Assert.Equal(CustomerOrderFulfillmentStatus.ReadyForPickup, order.FulfillmentStatus);
        Assert.Equal(CustomerOrderPaymentStatus.Unpaid, order.PaymentStatus);

        order.ConfirmPayment(order.Total, Actor, Utc.AddMinutes(3));
        order.MarkCollected(Utc.AddMinutes(4), Actor);
        order.Complete(Actor, Utc.AddMinutes(5));

        Assert.Equal(CustomerOrderStatus.Completed, order.Status);
        Assert.Equal(CustomerOrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(50m, order.Total);
        Assert.Equal(50m, order.AmountReceived);
    }

    [Fact]
    public void Cash_confirmation_records_received_amount_and_derives_change()
    {
        var order = CustomerOrder.CreateSubmitted(
            Seller,
            "ORD-260816-030",
            CustomerOrderParty.Personal(PlatformUser, "Ana Reyes"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [Line(1m, 350m)],
            Actor,
            Utc);

        var shortPay = Assert.Throws<DomainException>(() => order.ConfirmPayment(349.99m, Actor, Utc.AddMinutes(1)));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPaymentConfirmation, shortPay.ErrorCode);
        Assert.Equal(CustomerOrderPaymentStatus.Unpaid, order.PaymentStatus);

        order.ConfirmPayment(500m, Actor, Utc.AddMinutes(2));
        order.ConfirmPayment(500m, Actor, Utc.AddMinutes(9));

        Assert.Equal(CustomerOrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(500m, order.AmountReceived);
        Assert.Equal(150m, order.ChangeAmount);
        Assert.Equal(350m, order.Total);
        Assert.Equal(Actor, order.PaymentConfirmedBy);
        Assert.Equal(Utc.AddMinutes(2), order.PaymentConfirmedAtUtc);

        var changed = Assert.Throws<DomainException>(() => order.ConfirmPayment(600m, Actor, Utc.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPaymentConfirmation, changed.ErrorCode);
        Assert.Equal(500m, order.AmountReceived);
    }

    [Fact]
    public void Manual_gcash_confirmation_records_the_total_and_decline_returns_to_unpaid()
    {
        var order = CustomerOrder.CreateSubmitted(
            Seller,
            "ORD-260816-031",
            CustomerOrderParty.Personal(PlatformUser, "Ana Reyes"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [Line(1m, 350m)],
            Actor,
            Utc,
            paymentMethod: CustomerOrderPaymentMethod.ManualGCash,
            paymentReference: "1234567890123");

        Assert.Equal(CustomerOrderPaymentStatus.Pending, order.PaymentStatus);
        order.DeclinePayment(Actor, Utc.AddMinutes(1));
        Assert.Equal(CustomerOrderPaymentStatus.Unpaid, order.PaymentStatus);
        order.DeclinePayment(Actor, Utc.AddMinutes(2));

        order.ConfirmPayment(null, Actor, Utc.AddMinutes(3));
        Assert.Equal(CustomerOrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(350m, order.AmountReceived);
        Assert.Equal(0m, order.ChangeAmount);
        Assert.Equal("1234567890123", order.PaymentReference);

        var declineAfterPay = Assert.Throws<DomainException>(() => order.DeclinePayment(Actor, Utc.AddMinutes(4)));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPaymentConfirmation, declineAfterPay.ErrorCode);
    }

    [Fact]
    public void Utang_cannot_be_confirmed_as_paid()
    {
        var order = CustomerOrder.CreateSubmitted(
            Seller,
            "ORD-260816-032",
            CustomerOrderParty.Personal(PlatformUser, "Ana Reyes"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [Line()],
            Actor,
            Utc,
            paymentMethod: CustomerOrderPaymentMethod.Utang);
        order.Accept(Actor, Utc.AddMinutes(1));
        order.MarkReady(Utc.AddMinutes(2), Actor);
        order.MarkCollected(Utc.AddMinutes(3), Actor);
        order.Complete(Actor, Utc.AddMinutes(4));

        var ex = Assert.Throws<DomainException>(() => order.ConfirmPayment(order.Total, Actor, Utc.AddMinutes(5)));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPaymentConfirmation, ex.ErrorCode);
        Assert.Equal(CustomerOrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Equal(CustomerOrderStatus.Completed, order.Status);
    }

    [Fact]
    public void Manual_gcash_requires_a_reference()
    {
        var ex = Assert.Throws<DomainException>(() => CustomerOrder.CreateSubmitted(
            Seller,
            "ORD-260816-021",
            CustomerOrderParty.Personal(PlatformUser, "Ana Reyes"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [Line()],
            Actor,
            Utc,
            paymentMethod: CustomerOrderPaymentMethod.ManualGCash));

        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPaymentReference, ex.ErrorCode);
    }

    [Fact]
    public void Requested_pickup_time_is_an_immutable_snapshot()
    {
        var order = CustomerOrder.CreateSubmitted(
            Seller,
            "ORD-260816-022",
            CustomerOrderParty.Personal(PlatformUser, "Ana Reyes"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [Line()],
            Actor,
            Utc,
            requestedPickupLocal: "2026-08-16 20:30",
            requestedPickupTimeZoneId: "Asia/Manila",
            requestedPickupAtUtc: Utc.AddHours(1));

        Assert.Equal("2026-08-16 20:30", order.RequestedPickupLocal);
        Assert.Equal("Asia/Manila", order.RequestedPickupTimeZoneId);
        Assert.Equal(Utc.AddHours(1), order.RequestedPickupAtUtc);
    }

    [Fact]
    public void Past_requested_pickup_time_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => CustomerOrder.CreateSubmitted(
            Seller,
            "ORD-260816-023",
            CustomerOrderParty.Personal(PlatformUser, "Ana Reyes"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [Line()],
            Actor,
            Utc,
            requestedPickupLocal: "2026-08-16 11:00",
            requestedPickupTimeZoneId: "Asia/Manila",
            requestedPickupAtUtc: Utc.AddMinutes(-5)));

        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderPickupRequest, ex.ErrorCode);
    }

    private static CustomerOrder CreatePickup(CustomerOrderParty party) =>
        CustomerOrder.CreateSubmitted(
            Seller,
            "ORD-260816-001",
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
            "ORD-260816-003",
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
