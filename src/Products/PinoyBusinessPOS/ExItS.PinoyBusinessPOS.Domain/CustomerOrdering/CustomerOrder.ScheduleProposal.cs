using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

public sealed partial class CustomerOrder
{
    /// <summary>Effective agreed schedule local string (confirmed proposal, exact request, or preferred day+period).</summary>
    public string? EffectiveFulfillmentLocal =>
        ConfirmedFulfillmentLocal
        ?? (FulfillmentType == CustomerOrderFulfillmentType.Delivery
            ? RequestedDeliveryLocal
            : RequestedPickupLocal)
        ?? PreferredFulfillmentDisplayLocal;

    public string? EffectiveFulfillmentTimeZoneId =>
        ConfirmedFulfillmentTimeZoneId
        ?? (FulfillmentType == CustomerOrderFulfillmentType.Delivery
            ? RequestedDeliveryTimeZoneId
            : RequestedPickupTimeZoneId)
        ?? PreferredFulfillmentTimeZoneId;

    public DateTimeOffset? EffectiveFulfillmentAtUtc =>
        ConfirmedFulfillmentAtUtc
        ?? (FulfillmentType == CustomerOrderFulfillmentType.Delivery
            ? RequestedDeliveryAtUtc
            : RequestedPickupAtUtc)
        ?? PreferredFulfillmentWindowStartUtc;

    /// <summary>Display-only preference label (yyyy-MM-dd Period). Not an exact appointment time.</summary>
    public string? PreferredFulfillmentDisplayLocal =>
        PreferredFulfillmentDate is DateOnly date && PreferredFulfillmentPeriod is CustomerOrderPreferredFulfillmentPeriod period
            ? $"{date:yyyy-MM-dd} {period}"
            : null;

    public bool HasPendingScheduleProposal => PendingScheduleProposal is not null;

    /// <summary>
    /// Seller proposes a replacement schedule. Does not change payment, inventory, or order status.
    /// Idempotent when the same proposal id is already pending.
    /// </summary>
    public void ProposeSchedule(
        Guid proposalId,
        string local,
        string timeZoneId,
        DateTimeOffset atUtc,
        string? note,
        Guid actorId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureStatus(CustomerOrderStatus.Submitted, "Only submitted orders can receive a schedule proposal.");

        if (PendingScheduleProposal is { } existing && existing.ProposalId == proposalId)
        {
            return;
        }

        var customerRequestedAt = FulfillmentType == CustomerOrderFulfillmentType.Delivery
            ? RequestedDeliveryAtUtc
            : RequestedPickupAtUtc;
        if (customerRequestedAt is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "A schedule proposal requires an existing customer-requested fulfillment time.");
        }

        if (atUtc == customerRequestedAt.Value
            || string.Equals(
                local.Trim(),
                EffectiveFulfillmentLocal,
                StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderPickupRequest,
                "Proposed schedule must differ from the customer's requested time.");
        }

        PendingScheduleProposal = new CustomerOrderScheduleProposal(
            proposalId,
            local,
            timeZoneId,
            atUtc,
            note,
            actorId,
            utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void AcceptScheduleProposal(Guid proposalId, Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureStatus(CustomerOrderStatus.Submitted, "Only submitted orders can accept a schedule proposal.");

        if (PendingScheduleProposal is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                "There is no pending schedule proposal on this order.");
        }

        if (PendingScheduleProposal.ProposalId != proposalId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                "This schedule proposal is no longer current.");
        }

        ConfirmedFulfillmentLocal = PendingScheduleProposal.Local;
        ConfirmedFulfillmentTimeZoneId = PendingScheduleProposal.TimeZoneId;
        ConfirmedFulfillmentAtUtc = PendingScheduleProposal.AtUtc;
        PendingScheduleProposal = null;
        UpdatedAtUtc = utcNow;
    }

    public void DeclineScheduleProposal(Guid proposalId, Guid actorId, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureStatus(CustomerOrderStatus.Submitted, "Only submitted orders can decline a schedule proposal.");

        if (PendingScheduleProposal is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                "There is no pending schedule proposal on this order.");
        }

        if (PendingScheduleProposal.ProposalId != proposalId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderStatusTransition,
                "This schedule proposal is no longer current.");
        }

        PendingScheduleProposal = null;
        UpdatedAtUtc = utcNow;
    }
}
