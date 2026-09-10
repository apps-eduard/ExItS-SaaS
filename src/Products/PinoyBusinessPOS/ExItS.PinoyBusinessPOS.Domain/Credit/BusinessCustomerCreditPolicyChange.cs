using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>Append-only audit row for business-customer credit policy mutations.</summary>
public sealed class BusinessCustomerCreditPolicyChange
{
    public BusinessCustomerCreditPolicyChangeId Id { get; }
    public PosOrganizationId SellerOrganizationId { get; }
    public PosOrganizationId BuyerOrganizationId { get; }
    public BusinessCustomerCreditPolicyId BusinessCustomerCreditPolicyId { get; }
    public CustomerCreditPolicyChangeAction Action { get; }
    public CustomerCreditPolicyStatus? PreviousStatus { get; }
    public CustomerCreditPolicyStatus NewStatus { get; }
    public decimal? PreviousCreditLimit { get; }
    public decimal? NewCreditLimit { get; }
    public int? PreviousTermDays { get; }
    public int? NewTermDays { get; }
    public Guid ActorUserId { get; }
    public string Reason { get; }
    public DateTimeOffset ChangedAtUtc { get; }

    private BusinessCustomerCreditPolicyChange(
        BusinessCustomerCreditPolicyChangeId id,
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        BusinessCustomerCreditPolicyId businessCustomerCreditPolicyId,
        CustomerCreditPolicyChangeAction action,
        CustomerCreditPolicyStatus? previousStatus,
        CustomerCreditPolicyStatus newStatus,
        decimal? previousCreditLimit,
        decimal? newCreditLimit,
        int? previousTermDays,
        int? newTermDays,
        Guid actorUserId,
        string reason,
        DateTimeOffset changedAtUtc)
    {
        Id = id;
        SellerOrganizationId = sellerOrganizationId;
        BuyerOrganizationId = buyerOrganizationId;
        BusinessCustomerCreditPolicyId = businessCustomerCreditPolicyId;
        Action = action;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        PreviousCreditLimit = previousCreditLimit;
        NewCreditLimit = newCreditLimit;
        PreviousTermDays = previousTermDays;
        NewTermDays = newTermDays;
        ActorUserId = actorUserId;
        Reason = reason;
        ChangedAtUtc = changedAtUtc;
    }

    public static BusinessCustomerCreditPolicyChange Create(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        BusinessCustomerCreditPolicyId businessCustomerCreditPolicyId,
        CustomerCreditPolicyChangeAction action,
        CustomerCreditPolicyStatus? previousStatus,
        CustomerCreditPolicyStatus newStatus,
        decimal? previousCreditLimit,
        decimal? newCreditLimit,
        int? previousTermDays,
        int? newTermDays,
        Guid actorUserId,
        string reason,
        DateTimeOffset changedAtUtc,
        BusinessCustomerCreditPolicyChangeId? id = null)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditPolicyActor,
                "Actor user id is required.");
        }

        if (changedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamp must be UTC.");
        }

        return new BusinessCustomerCreditPolicyChange(
            id ?? BusinessCustomerCreditPolicyChangeId.New(),
            sellerOrganizationId,
            buyerOrganizationId,
            businessCustomerCreditPolicyId,
            action,
            previousStatus,
            newStatus,
            previousCreditLimit,
            newCreditLimit,
            previousTermDays,
            newTermDays,
            actorUserId,
            reason,
            changedAtUtc);
    }

    public static BusinessCustomerCreditPolicyChange Rehydrate(
        BusinessCustomerCreditPolicyChangeId id,
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        BusinessCustomerCreditPolicyId businessCustomerCreditPolicyId,
        CustomerCreditPolicyChangeAction action,
        CustomerCreditPolicyStatus? previousStatus,
        CustomerCreditPolicyStatus newStatus,
        decimal? previousCreditLimit,
        decimal? newCreditLimit,
        int? previousTermDays,
        int? newTermDays,
        Guid actorUserId,
        string reason,
        DateTimeOffset changedAtUtc) =>
        new(
            id,
            sellerOrganizationId,
            buyerOrganizationId,
            businessCustomerCreditPolicyId,
            action,
            previousStatus,
            newStatus,
            previousCreditLimit,
            newCreditLimit,
            previousTermDays,
            newTermDays,
            actorUserId,
            reason,
            changedAtUtc);
}
