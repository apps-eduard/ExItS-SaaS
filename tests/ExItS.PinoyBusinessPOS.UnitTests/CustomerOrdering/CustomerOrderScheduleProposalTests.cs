using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.CustomerOrdering;

public sealed class CustomerOrderScheduleProposalTests
{
    private static readonly PosOrganizationId Seller =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product =
        CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid BranchId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid SellerActor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid BuyerActor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly DateTimeOffset Utc = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Propose_accept_updates_confirmed_schedule_without_changing_payment_or_status()
    {
        var order = CreateScheduledPickup();
        var proposalId = Guid.Parse("11111111-1111-4111-8111-111111111111");
        var proposedAt = Utc.AddHours(3);

        order.ProposeSchedule(
            proposalId,
            "2026-08-16 22:00",
            "Asia/Manila",
            proposedAt,
            "Can you do 10pm?",
            SellerActor,
            Utc.AddMinutes(5));

        Assert.Equal(CustomerOrderStatus.Submitted, order.Status);
        Assert.Equal(CustomerOrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.NotNull(order.PendingScheduleProposal);
        Assert.Equal(proposalId, order.PendingScheduleProposal!.ProposalId);
        Assert.Equal("2026-08-16 20:30", order.RequestedPickupLocal);

        order.AcceptScheduleProposal(proposalId, BuyerActor, Utc.AddMinutes(10));

        Assert.Null(order.PendingScheduleProposal);
        Assert.Equal("2026-08-16 22:00", order.ConfirmedFulfillmentLocal);
        Assert.Equal("2026-08-16 22:00", order.EffectiveFulfillmentLocal);
        Assert.Equal("2026-08-16 20:30", order.RequestedPickupLocal);
        Assert.Equal(CustomerOrderStatus.Submitted, order.Status);
        Assert.Equal(CustomerOrderPaymentStatus.Unpaid, order.PaymentStatus);
        Assert.Equal(CustomerOrderStockReservationState.None, order.StockReservationState);
    }

    [Fact]
    public void Decline_clears_proposal_and_keeps_requested_schedule()
    {
        var order = CreateScheduledPickup();
        var proposalId = Guid.NewGuid();
        order.ProposeSchedule(
            proposalId,
            "2026-08-16 22:00",
            "Asia/Manila",
            Utc.AddHours(3),
            null,
            SellerActor,
            Utc.AddMinutes(5));

        order.DeclineScheduleProposal(proposalId, BuyerActor, Utc.AddMinutes(10));

        Assert.Null(order.PendingScheduleProposal);
        Assert.Null(order.ConfirmedFulfillmentLocal);
        Assert.Equal("2026-08-16 20:30", order.EffectiveFulfillmentLocal);
        Assert.Equal(CustomerOrderStatus.Submitted, order.Status);
    }

    [Fact]
    public void Stale_proposal_id_is_rejected()
    {
        var order = CreateScheduledPickup();
        var current = Guid.NewGuid();
        order.ProposeSchedule(
            current,
            "2026-08-16 22:00",
            "Asia/Manila",
            Utc.AddHours(3),
            null,
            SellerActor,
            Utc.AddMinutes(5));

        var stale = Assert.Throws<DomainException>(() =>
            order.AcceptScheduleProposal(Guid.NewGuid(), BuyerActor, Utc.AddMinutes(10)));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderStatusTransition, stale.ErrorCode);
        Assert.NotNull(order.PendingScheduleProposal);
    }

    [Fact]
    public void Accept_order_blocked_while_proposal_pending()
    {
        var order = CreateScheduledPickup();
        order.ProposeSchedule(
            Guid.NewGuid(),
            "2026-08-16 22:00",
            "Asia/Manila",
            Utc.AddHours(3),
            null,
            SellerActor,
            Utc.AddMinutes(5));

        var blocked = Assert.Throws<DomainException>(() =>
            order.Accept(SellerActor, Utc.AddMinutes(10)));
        Assert.Equal(DomainErrorCodes.InvalidCustomerOrderStatusTransition, blocked.ErrorCode);
    }

    [Fact]
    public void Propose_same_proposal_id_is_idempotent()
    {
        var order = CreateScheduledPickup();
        var proposalId = Guid.NewGuid();
        order.ProposeSchedule(
            proposalId,
            "2026-08-16 22:00",
            "Asia/Manila",
            Utc.AddHours(3),
            "first",
            SellerActor,
            Utc.AddMinutes(5));
        order.ProposeSchedule(
            proposalId,
            "2026-08-16 22:00",
            "Asia/Manila",
            Utc.AddHours(3),
            "first",
            SellerActor,
            Utc.AddMinutes(6));

        Assert.Equal(proposalId, order.PendingScheduleProposal!.ProposalId);
        Assert.Equal("first", order.PendingScheduleProposal.Note);
    }

    [Fact]
    public void Reject_clears_pending_proposal()
    {
        var order = CreateScheduledPickup();
        order.ProposeSchedule(
            Guid.NewGuid(),
            "2026-08-16 22:00",
            "Asia/Manila",
            Utc.AddHours(3),
            null,
            SellerActor,
            Utc.AddMinutes(5));

        order.Reject(CustomerOrderRejectReason.UnableToFulfill, null, SellerActor, Utc.AddMinutes(10));

        Assert.Equal(CustomerOrderStatus.Rejected, order.Status);
        Assert.Null(order.PendingScheduleProposal);
    }

    private static CustomerOrder CreateScheduledPickup() =>
        CustomerOrder.CreateSubmitted(
            Seller,
            "ORD-260816-100",
            CustomerOrderParty.Personal(BuyerActor, "Ana Reyes"),
            CustomerOrderFulfillmentType.Pickup,
            BranchId,
            "Main Branch",
            [new CustomerOrderLineDraft(Product, "Rice 25kg", "SKU-RICE", UnitOfMeasure.Piece, 2m, 25m)],
            SellerActor,
            Utc,
            requestedPickupLocal: "2026-08-16 20:30",
            requestedPickupTimeZoneId: "Asia/Manila",
            requestedPickupAtUtc: Utc.AddHours(1));
}
