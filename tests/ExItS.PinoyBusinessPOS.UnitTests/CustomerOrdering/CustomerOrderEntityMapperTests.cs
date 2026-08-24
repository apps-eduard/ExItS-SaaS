using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.CustomerOrdering;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class CustomerOrderEntityMapperTests
{
    [Fact]
    public void Incomplete_delivery_row_maps_without_throwing()
    {
        var record = BaseRecord(CustomerOrderFulfillmentType.Delivery);
        // Intentionally leave delivery_* columns null — previously NRE → HTTP 500 on /mine.

        var order = CustomerOrderEntityMapper.ToDomain(record, []);

        Assert.Equal(CustomerOrderFulfillmentType.Delivery, order.FulfillmentType);
        Assert.Null(order.DeliverySnapshot);
        Assert.Null(CustomerOrderEntityMapper.TryMapDeliverySnapshot(record));
    }

    [Fact]
    public void Complete_delivery_row_maps_snapshot()
    {
        var record = BaseRecord(CustomerOrderFulfillmentType.Delivery);
        record.DeliveryRecipientName = "Ana";
        record.DeliveryRecipientPhone = "0917";
        record.DeliveryAddressLine1 = "123 Main";
        record.DeliveryCity = "Manila";
        record.DeliveryDestinationLatitude = 14.5m;
        record.DeliveryDestinationLongitude = 121.0m;
        record.DeliveryBranchLatitudeSnapshot = 14.6m;
        record.DeliveryBranchLongitudeSnapshot = 121.1m;
        record.DeliveryDistanceKm = 2.5m;
        record.DeliveryMinimumOrderAmountSnapshot = 100m;
        record.DeliveryBaseFeeSnapshot = 40m;
        record.DeliveryIncludedDistanceKmSnapshot = 1m;
        record.DeliveryAdditionalFeePerKmSnapshot = 10m;
        record.DeliveryMaximumDistanceKmSnapshot = 10m;
        record.DeliveryDistanceCharge = 15m;
        record.DeliveryFinalFee = 55m;
        record.DeliveryFreeApplied = false;

        var order = CustomerOrderEntityMapper.ToDomain(record, []);

        Assert.NotNull(order.DeliverySnapshot);
        Assert.Equal("Ana", order.DeliverySnapshot!.RecipientName);
        Assert.Equal(55m, order.DeliverySnapshot.FinalDeliveryFee);
    }

    [Fact]
    public void Pickup_row_has_no_delivery_snapshot()
    {
        var record = BaseRecord(CustomerOrderFulfillmentType.Pickup);

        var order = CustomerOrderEntityMapper.ToDomain(record, []);

        Assert.Null(order.DeliverySnapshot);
    }

    private static CustomerOrderRecord BaseRecord(CustomerOrderFulfillmentType fulfillment) =>
        new()
        {
            Id = Guid.Parse("11111111-1111-4111-8111-111111111111"),
            SellerOrganizationId = Guid.Parse("22222222-2222-4222-8222-222222222222"),
            OrderNumber = "SO-000001",
            Status = nameof(CustomerOrderStatus.Submitted),
            FulfillmentStatus = nameof(CustomerOrderFulfillmentStatus.Pending),
            PaymentStatus = nameof(CustomerOrderPaymentStatus.Unpaid),
            PaymentMethod = "Cash",
            FulfillmentType = fulfillment.ToString(),
            FulfillmentBranchId = Guid.Parse("33333333-3333-4333-8333-333333333333"),
            BranchNameSnapshot = "Branch",
            CustomerPartyType = nameof(CustomerPartyType.Personal),
            CustomerDisplayNameSnapshot = "Buyer",
            CustomerPlatformUserId = Guid.Parse("44444444-4444-4444-8444-444444444444"),
            MerchandiseSubtotal = 100m,
            DeliveryFee = fulfillment == CustomerOrderFulfillmentType.Delivery ? 20m : 0m,
            Total = fulfillment == CustomerOrderFulfillmentType.Delivery ? 120m : 100m,
            StockReservationState = nameof(CustomerOrderStockReservationState.None),
            CreatedAtUtc = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero),
            SubmittedAtUtc = new DateTimeOffset(2026, 8, 24, 12, 0, 0, TimeSpan.Zero),
            SubmittedBy = Guid.Parse("44444444-4444-4444-8444-444444444444")
        };
}
