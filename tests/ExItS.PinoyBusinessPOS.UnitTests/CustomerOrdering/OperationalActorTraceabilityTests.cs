using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class OperationalActorTraceabilityTests
{
    private static readonly PosOrganizationId Seller =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product =
        CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid BranchId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid SpoofedActor = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset Utc = new(2026, 8, 19, 6, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Fulfillment_mutations_reject_empty_actor()
    {
        var order = CreateAcceptedPickup();
        var ex = Assert.Throws<DomainException>(() => order.MarkReady(Utc, Guid.Empty));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderActor, ex.ErrorCode);
    }

    [Fact]
    public void Fulfillment_actor_is_persisted_on_domain_state()
    {
        var order = CreateAcceptedPickup();
        order.MarkReady(Utc, Actor);
        Assert.Equal(Actor, order.ReadyBy);
        Assert.NotEqual(SpoofedActor, order.ReadyBy);
    }

    [Fact]
    public void Rehydrate_preserves_null_legacy_fulfillment_provenance()
    {
        var order = CustomerOrder.Rehydrate(
            CustomerOrderId.New(),
            Seller,
            "SO-000099",
            CustomerOrderStatus.Accepted,
            CustomerOrderFulfillmentStatus.Preparing,
            CustomerOrderPaymentStatus.Unpaid,
            CustomerOrderPaymentMethod.Cash,
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            CustomerOrderParty.Personal(Guid.NewGuid(), "Ana"),
            [],
            0m,
            0m,
            0m,
            null,
            CustomerOrderStockReservationState.None,
            null,
            null,
            null,
            Utc,
            Utc,
            Actor,
            Utc,
            Actor,
            null,
            null,
            null,
            null,
            null,
            null,
            readyAtUtc: null,
            readyBy: null,
            outForDeliveryAtUtc: null,
            outForDeliveryBy: null,
            deliveredAtUtc: null,
            deliveredBy: null,
            collectedAtUtc: null,
            collectedBy: null,
            Utc);

        Assert.Null(order.ReadyBy);
        Assert.Null(order.CollectedBy);
    }

    private static CustomerOrder CreateAcceptedPickup()
    {
        var order = CustomerOrder.CreateSubmitted(
            Seller,
            "SO-000050",
            CustomerOrderParty.Personal(Guid.NewGuid(), "Ana"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [new CustomerOrderLineDraft(Product, "Rice", "SKU", UnitOfMeasure.Piece, 1m, 10m)],
            Actor,
            Utc);
        order.Accept(Actor, Utc.AddMinutes(1));
        return order;
    }
}
