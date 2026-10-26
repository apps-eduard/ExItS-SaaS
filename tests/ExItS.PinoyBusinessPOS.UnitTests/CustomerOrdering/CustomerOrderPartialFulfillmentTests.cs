using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class CustomerOrderPartialFulfillmentTests
{
    private static readonly PosOrganizationId Seller =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Apple =
        CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly CatalogProductId Banana =
        CatalogProductId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly Guid BranchId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly DateTimeOffset Utc = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Seller_can_reduce_line_without_buyer_approval()
    {
        var order = CreateOrder();
        var apple = order.Lines.Single(l => l.ProductId == Apple);

        order.AdjustLines(
            [new CustomerOrderLineAdjustment(apple.Id.Value, 3m, CustomerOrderLineAdjustmentReason.InsufficientStock)],
            Actor,
            Utc);

        Assert.Equal(3m, apple.Quantity);
        Assert.Equal(10m, apple.OriginalQuantity);
        Assert.Equal(7m, apple.CancelledQuantity);
        Assert.True(order.OriginalTotal > order.Total);
        Assert.Contains(order.ActivityEvents, e => e.Action == CustomerOrderActivityEvent.Actions.QuantityReduced);
        Assert.Contains(order.ActivityEvents, e => e.Action == CustomerOrderActivityEvent.Actions.OrderTotalAdjusted);
    }

    [Fact]
    public void Seller_cannot_increase_quantity()
    {
        var order = CreateOrder();
        var apple = order.Lines.Single(l => l.ProductId == Apple);

        var ex = Assert.Throws<DomainException>(() =>
            order.AdjustLines(
                [new CustomerOrderLineAdjustment(apple.Id.Value, 12m, CustomerOrderLineAdjustmentReason.OutOfStock)],
                Actor,
                Utc));

        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderLineQuantity, ex.ErrorCode);
    }

    [Fact]
    public void Cancelling_all_lines_cancels_order()
    {
        var order = CreateOrder();
        var adjustments = order.Lines
            .Select(l => new CustomerOrderLineAdjustment(
                l.Id.Value,
                0m,
                CustomerOrderLineAdjustmentReason.OutOfStock))
            .ToList();

        order.AdjustLines(adjustments, Actor, Utc);

        Assert.Equal(CustomerOrderStatus.Cancelled, order.Status);
        Assert.Equal(CustomerOrderCancelReason.AllItemsCancelled, order.CancelReason);
        Assert.Equal(0m, order.Total);
        Assert.All(order.Lines, l => Assert.True(l.IsCancelled));
    }

    [Fact]
    public void Paid_preparing_order_allows_adjustment_and_keeps_paid_with_overpayment()
    {
        var order = CreateOrder(CustomerOrderPaymentMethod.Cash);
        order.Accept(Actor, Utc);
        var paidTotal = order.Total;
        order.ConfirmPayment(paidTotal, Actor, Utc.AddMinutes(1));

        var apple = order.Lines.Single(l => l.ProductId == Apple);
        order.AdjustLines(
            [new CustomerOrderLineAdjustment(apple.Id.Value, 1m, CustomerOrderLineAdjustmentReason.OutOfStock)],
            Actor,
            Utc.AddMinutes(2));

        Assert.Equal(CustomerOrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal(paidTotal, order.AmountReceived);
        Assert.True(order.Total < paidTotal);
        Assert.Equal(SaleMoney.RoundMoney(paidTotal - order.Total), order.ChangeAmount);
        Assert.Equal(1m, apple.Quantity);
    }

    [Fact]
    public void Manual_gcash_place_allows_null_reference_and_seller_confirms_with_reference()
    {
        var order = CreateOrder(CustomerOrderPaymentMethod.ManualGCash);
        Assert.Equal(CustomerOrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Null(order.PaymentReference);

        order.Accept(Actor, Utc);
        order.ConfirmPayment(order.Total, Actor, Utc.AddMinutes(1), "GCASH-REF-001");

        Assert.Equal(CustomerOrderPaymentStatus.Paid, order.PaymentStatus);
        Assert.Equal("GCASH-REF-001", order.PaymentReference);
    }

    [Fact]
    public void Buyer_cannot_cancel_ready_for_pickup()
    {
        var order = CreateOrder();
        order.Accept(Actor, Utc);
        order.ConfirmPayment(order.Total, Actor, Utc.AddMinutes(1));
        order.MarkReady(Utc.AddMinutes(2), Actor);

        var ex = Assert.Throws<DomainException>(() => order.CancelByBuyer(Actor, Utc.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Seller_records_customer_refusal()
    {
        var order = CreateOrder();
        order.Accept(Actor, Utc);
        order.ConfirmPayment(order.Total, Actor, Utc.AddMinutes(1));
        order.MarkReady(Utc.AddMinutes(2), Actor);

        order.CancelBySeller(
            CustomerOrderCancelReason.CustomerRefusedPickup,
            "Customer refused adjusted order at counter.",
            Actor,
            Utc.AddMinutes(3));

        Assert.Equal(CustomerOrderStatus.Cancelled, order.Status);
        Assert.Equal(CustomerOrderCancelReason.CustomerRefusedPickup, order.CancelReason);
        Assert.Contains(order.ActivityEvents, e => e.Action == CustomerOrderActivityEvent.Actions.CustomerRefused);
    }

    [Fact]
    public void Other_reason_requires_note()
    {
        var order = CreateOrder();
        var apple = order.Lines.Single(l => l.ProductId == Apple);

        var ex = Assert.Throws<DomainException>(() =>
            order.AdjustLines(
                [new CustomerOrderLineAdjustment(apple.Id.Value, 1m, CustomerOrderLineAdjustmentReason.Other)],
                Actor,
                Utc));

        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderLineAdjustment, ex.ErrorCode);
    }

    [Fact]
    public void Seller_can_reduce_during_preparing()
    {
        var order = CreateOrder();
        order.Accept(Actor, Utc);
        Assert.Equal(CustomerOrderFulfillmentStatus.Preparing, order.FulfillmentStatus);

        var apple = order.Lines.Single(l => l.ProductId == Apple);
        order.AdjustLines(
            [new CustomerOrderLineAdjustment(apple.Id.Value, 4m, CustomerOrderLineAdjustmentReason.ProductUnavailable)],
            Actor,
            Utc.AddMinutes(1));

        Assert.Equal(4m, apple.Quantity);
        Assert.Equal(CustomerOrderStatus.Accepted, order.Status);
    }

    [Fact]
    public void Ready_for_pickup_allows_line_adjustments()
    {
        var order = CreateOrder();
        order.Accept(Actor, Utc);
        order.MarkReady(Utc.AddMinutes(1), Actor);

        var apple = order.Lines.Single(l => l.ProductId == Apple);
        order.AdjustLines(
            [new CustomerOrderLineAdjustment(apple.Id.Value, 1m, CustomerOrderLineAdjustmentReason.CustomerRequested)],
            Actor,
            Utc.AddMinutes(2));

        Assert.Equal(1m, apple.Quantity);
        Assert.Equal(CustomerOrderFulfillmentStatus.ReadyForPickup, order.FulfillmentStatus);
        Assert.Equal(CustomerOrderStatus.Accepted, order.Status);
    }

    [Theory]
    [InlineData("CustomerRequested")]
    [InlineData("customerRequested")]
    [InlineData("ProductUnavailable")]
    [InlineData("InsufficientStock")]
    public void Application_parses_seller_adjustment_reason_names(string reason)
    {
        Assert.True(
            AdjustCustomerOrderLines.TryParseAdjustmentReason(reason, out var parsed));
        Assert.True(Enum.IsDefined(parsed));
    }

    [Fact]
    public void Collected_orders_reject_line_adjustments()
    {
        var order = CreateOrder();
        order.Accept(Actor, Utc);
        order.MarkReady(Utc.AddMinutes(1), Actor);
        order.ConfirmPayment(order.Total, Actor, Utc.AddMinutes(2));
        order.MarkCollected(Utc.AddMinutes(3), Actor);

        var apple = order.Lines.Single(l => l.ProductId == Apple);
        var ex = Assert.Throws<DomainException>(() =>
            order.AdjustLines(
                [new CustomerOrderLineAdjustment(apple.Id.Value, 1m, CustomerOrderLineAdjustmentReason.CustomerRequested)],
                Actor,
                Utc.AddMinutes(4)));

        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderStatusTransition, ex.ErrorCode);
        Assert.Equal(10m, apple.Quantity);
    }

    [Fact]
    public void Mark_ready_succeeds_when_fulfillable_items_remain()
    {
        var order = CreateOrder();
        order.Accept(Actor, Utc);
        var apple = order.Lines.Single(l => l.ProductId == Apple);
        order.AdjustLines(
            [new CustomerOrderLineAdjustment(apple.Id.Value, 2m, CustomerOrderLineAdjustmentReason.InsufficientStock)],
            Actor,
            Utc.AddMinutes(1));

        order.MarkReady(Utc.AddMinutes(2), Actor);

        Assert.Equal(CustomerOrderFulfillmentStatus.ReadyForPickup, order.FulfillmentStatus);
        Assert.Equal(2m, apple.Quantity);
    }

    [Fact]
    public void Ready_for_pickup_rejects_non_refusal_cancel_reason()
    {
        var order = CreateOrder();
        order.Accept(Actor, Utc);
        order.MarkReady(Utc.AddMinutes(1), Actor);

        var ex = Assert.Throws<DomainException>(() =>
            order.CancelBySeller(
                CustomerOrderCancelReason.SellerUnableToFulfill,
                "Too busy",
                Actor,
                Utc.AddMinutes(2)));

        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderCancelReason, ex.ErrorCode);
        Assert.Equal(CustomerOrderStatus.Accepted, order.Status);
    }

    [Fact]
    public void Seller_can_cancel_while_preparing()
    {
        var order = CreateOrder();
        order.Accept(Actor, Utc);

        order.CancelBySeller(
            CustomerOrderCancelReason.SellerUnableToFulfill,
            null,
            Actor,
            Utc.AddMinutes(1));

        Assert.Equal(CustomerOrderStatus.Cancelled, order.Status);
        Assert.Equal(CustomerOrderCancelReason.SellerUnableToFulfill, order.CancelReason);
    }

    private static CustomerOrder CreateOrder(
        CustomerOrderPaymentMethod paymentMethod = CustomerOrderPaymentMethod.Cash) =>
        CustomerOrder.CreateSubmitted(
            Seller,
            "ORD-261025-001",
            CustomerOrderParty.Personal(Actor, "Ana"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main",
            [
                new CustomerOrderLineDraft(Apple, "Apple", "APL", UnitOfMeasure.Kilogram, 10m, 50m),
                new CustomerOrderLineDraft(Banana, "Banana", "BAN", UnitOfMeasure.Kilogram, 5m, 30m),
            ],
            Actor,
            Utc,
            paymentMethod: paymentMethod);
}
