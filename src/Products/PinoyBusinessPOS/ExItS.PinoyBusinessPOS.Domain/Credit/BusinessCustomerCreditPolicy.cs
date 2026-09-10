using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>
/// Seller-owned B2B credit authorization for one buyer organization (via connected-supplier relationship).
/// Does not create a POSCustomer. Outstanding is always 0 until a B2B ledger exists.
/// Reuses <see cref="CustomerCreditPolicyStatus"/> and <see cref="CustomerCreditPolicyChangeAction"/>.
/// </summary>
public sealed class BusinessCustomerCreditPolicy
{
    public const string InitialConfigureReason = "Initial business customer credit configuration.";

    public BusinessCustomerCreditPolicyId Id { get; }
    public PosOrganizationId SellerOrganizationId { get; }
    public PosOrganizationId BuyerOrganizationId { get; }
    /// <summary>Correlated connected-supplier relationship id (nullable; no Platform FK).</summary>
    public Guid? ConnectionId { get; private set; }
    public CustomerCreditPolicyStatus Status { get; private set; }
    public decimal CreditLimit { get; private set; }
    public int DefaultTermDays { get; private set; }
    public Guid ConfiguredByUserId { get; private set; }
    public DateTimeOffset ConfiguredAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private BusinessCustomerCreditPolicy(
        BusinessCustomerCreditPolicyId id,
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        Guid? connectionId,
        CustomerCreditPolicyStatus status,
        decimal creditLimit,
        int defaultTermDays,
        Guid configuredByUserId,
        DateTimeOffset configuredAtUtc,
        Guid? approvedByUserId,
        DateTimeOffset? approvedAtUtc,
        Guid updatedByUserId,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        SellerOrganizationId = sellerOrganizationId;
        BuyerOrganizationId = buyerOrganizationId;
        ConnectionId = connectionId;
        Status = status;
        CreditLimit = creditLimit;
        DefaultTermDays = defaultTermDays;
        ConfiguredByUserId = configuredByUserId;
        ConfiguredAtUtc = configuredAtUtc;
        ApprovedByUserId = approvedByUserId;
        ApprovedAtUtc = approvedAtUtc;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static BusinessCustomerCreditPolicy Rehydrate(
        BusinessCustomerCreditPolicyId id,
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        Guid? connectionId,
        CustomerCreditPolicyStatus status,
        decimal creditLimit,
        int defaultTermDays,
        Guid configuredByUserId,
        DateTimeOffset configuredAtUtc,
        Guid? approvedByUserId,
        DateTimeOffset? approvedAtUtc,
        Guid updatedByUserId,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            sellerOrganizationId,
            buyerOrganizationId,
            connectionId,
            status,
            creditLimit,
            defaultTermDays,
            configuredByUserId,
            configuredAtUtc,
            approvedByUserId,
            approvedAtUtc,
            updatedByUserId,
            updatedAtUtc);

    /// <summary>Creates the first policy row in PendingApproval.</summary>
    public static (BusinessCustomerCreditPolicy Policy, BusinessCustomerCreditPolicyChange Change) Configure(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        Guid? connectionId,
        decimal creditLimit,
        int defaultTermDays,
        Guid actorUserId,
        string? reason,
        DateTimeOffset utcNow,
        BusinessCustomerCreditPolicyId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorUserId);
        var limit = CustomerCreditPolicy.NormalizeCreditLimit(creditLimit);
        var term = CustomerCreditPolicy.NormalizeTermDays(defaultTermDays);
        var normalizedReason = CustomerCreditPolicy.NormalizeReason(
            reason,
            allowDefault: true,
            InitialConfigureReason);
        var policyId = id ?? BusinessCustomerCreditPolicyId.New();

        var policy = new BusinessCustomerCreditPolicy(
            policyId,
            sellerOrganizationId,
            buyerOrganizationId,
            NormalizeConnectionId(connectionId),
            CustomerCreditPolicyStatus.PendingApproval,
            limit,
            term,
            actorUserId,
            utcNow,
            null,
            null,
            actorUserId,
            utcNow);

        var change = BusinessCustomerCreditPolicyChange.Create(
            sellerOrganizationId,
            buyerOrganizationId,
            policyId,
            CustomerCreditPolicyChangeAction.Configured,
            previousStatus: null,
            newStatus: CustomerCreditPolicyStatus.PendingApproval,
            previousCreditLimit: null,
            newCreditLimit: limit,
            previousTermDays: null,
            newTermDays: term,
            actorUserId,
            normalizedReason,
            utcNow);

        return (policy, change);
    }

    public BusinessCustomerCreditPolicyChange UpdateTerms(
        decimal creditLimit,
        int defaultTermDays,
        Guid actorUserId,
        string reason,
        DateTimeOffset utcNow,
        Guid? connectionId = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorUserId);
        if (Status is not (CustomerCreditPolicyStatus.PendingApproval
            or CustomerCreditPolicyStatus.Approved
            or CustomerCreditPolicyStatus.Disabled))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditPolicyStatusTransition,
                "Only PendingApproval, Approved, or Disabled policies can be updated.");
        }

        var limit = CustomerCreditPolicy.NormalizeCreditLimit(creditLimit);
        var term = CustomerCreditPolicy.NormalizeTermDays(defaultTermDays);
        var previousStatus = Status;
        var previousLimit = CreditLimit;
        var previousTerm = DefaultTermDays;
        var limitChanged = previousLimit != limit;
        var termChanged = previousTerm != term;

        if (!limitChanged && !termChanged && Status != CustomerCreditPolicyStatus.Disabled)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerCreditPolicyUnchanged,
                "Credit limit and term are unchanged.");
        }

        var normalizedReason = CustomerCreditPolicy.NormalizeReason(reason, allowDefault: false, null);

        CustomerCreditPolicyChangeAction action;
        if (Status == CustomerCreditPolicyStatus.Disabled)
        {
            action = CustomerCreditPolicyChangeAction.Reconfigured;
        }
        else if (limitChanged && termChanged)
        {
            action = CustomerCreditPolicyChangeAction.CreditLimitAndTermChanged;
        }
        else if (limitChanged)
        {
            action = CustomerCreditPolicyChangeAction.CreditLimitChanged;
        }
        else if (termChanged)
        {
            action = CustomerCreditPolicyChangeAction.TermChanged;
        }
        else
        {
            action = CustomerCreditPolicyChangeAction.Reconfigured;
        }

        CreditLimit = limit;
        DefaultTermDays = term;
        Status = CustomerCreditPolicyStatus.PendingApproval;
        ApprovedByUserId = null;
        ApprovedAtUtc = null;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = utcNow;
        if (connectionId is Guid cid && cid != Guid.Empty)
        {
            ConnectionId = cid;
        }

        if (previousStatus == CustomerCreditPolicyStatus.Disabled
            || ConfiguredByUserId == Guid.Empty)
        {
            ConfiguredByUserId = actorUserId;
            ConfiguredAtUtc = utcNow;
        }

        return BusinessCustomerCreditPolicyChange.Create(
            SellerOrganizationId,
            BuyerOrganizationId,
            Id,
            action,
            previousStatus,
            CustomerCreditPolicyStatus.PendingApproval,
            previousLimit,
            limit,
            previousTerm,
            term,
            actorUserId,
            normalizedReason,
            utcNow);
    }

    public BusinessCustomerCreditPolicyChange Approve(
        Guid actorUserId,
        string reason,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorUserId);
        if (Status != CustomerCreditPolicyStatus.PendingApproval)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditPolicyStatusTransition,
                "Only PendingApproval policies can be approved.");
        }

        var normalizedReason = CustomerCreditPolicy.NormalizeReason(reason, allowDefault: false, null);
        var previous = Status;
        Status = CustomerCreditPolicyStatus.Approved;
        ApprovedByUserId = actorUserId;
        ApprovedAtUtc = utcNow;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = utcNow;

        return BusinessCustomerCreditPolicyChange.Create(
            SellerOrganizationId,
            BuyerOrganizationId,
            Id,
            CustomerCreditPolicyChangeAction.Approved,
            previous,
            CustomerCreditPolicyStatus.Approved,
            CreditLimit,
            CreditLimit,
            DefaultTermDays,
            DefaultTermDays,
            actorUserId,
            normalizedReason,
            utcNow);
    }

    public BusinessCustomerCreditPolicyChange Disable(
        Guid actorUserId,
        string reason,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorUserId);
        if (Status is not (CustomerCreditPolicyStatus.PendingApproval or CustomerCreditPolicyStatus.Approved))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditPolicyStatusTransition,
                "Only PendingApproval or Approved policies can be disabled.");
        }

        var normalizedReason = CustomerCreditPolicy.NormalizeReason(reason, allowDefault: false, null);
        var previous = Status;
        Status = CustomerCreditPolicyStatus.Disabled;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = utcNow;

        return BusinessCustomerCreditPolicyChange.Create(
            SellerOrganizationId,
            BuyerOrganizationId,
            Id,
            CustomerCreditPolicyChangeAction.Disabled,
            previous,
            CustomerCreditPolicyStatus.Disabled,
            CreditLimit,
            CreditLimit,
            DefaultTermDays,
            DefaultTermDays,
            actorUserId,
            normalizedReason,
            utcNow);
    }

    public bool PermitsNewUtang => Status == CustomerCreditPolicyStatus.Approved;

    /// <summary>
    /// B2B outstanding is always 0 until a ledger exists.
    /// AvailableCredit = Approved ? max(0, limit - outstanding) : 0.
    /// </summary>
    public static decimal AvailableCredit(CustomerCreditPolicyStatus status, decimal creditLimit, decimal outstanding = 0m) =>
        CustomerCreditPolicy.AvailableCredit(status, creditLimit, outstanding);

    private static Guid? NormalizeConnectionId(Guid? connectionId) =>
        connectionId is Guid id && id != Guid.Empty ? id : null;

    private static void EnsureActor(Guid actorUserId)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditPolicyActor,
                "Actor user id is required.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamp must be UTC.");
        }
    }
}
