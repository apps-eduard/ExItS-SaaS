using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>Append-only audit row for customer credit policy mutations.</summary>
public sealed class CustomerCreditPolicyChange
{
    public CustomerCreditPolicyChangeId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public POSCustomerId CustomerId { get; }
    public CustomerCreditPolicyId CustomerCreditPolicyId { get; }
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

    private CustomerCreditPolicyChange(
        CustomerCreditPolicyChangeId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CustomerCreditPolicyId customerCreditPolicyId,
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
        OrganizationId = organizationId;
        CustomerId = customerId;
        CustomerCreditPolicyId = customerCreditPolicyId;
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

    public static CustomerCreditPolicyChange Create(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CustomerCreditPolicyId customerCreditPolicyId,
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
        CustomerCreditPolicyChangeId? id = null)
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

        return new CustomerCreditPolicyChange(
            id ?? CustomerCreditPolicyChangeId.New(),
            organizationId,
            customerId,
            customerCreditPolicyId,
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

    public static CustomerCreditPolicyChange Rehydrate(
        CustomerCreditPolicyChangeId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CustomerCreditPolicyId customerCreditPolicyId,
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
            organizationId,
            customerId,
            customerCreditPolicyId,
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
