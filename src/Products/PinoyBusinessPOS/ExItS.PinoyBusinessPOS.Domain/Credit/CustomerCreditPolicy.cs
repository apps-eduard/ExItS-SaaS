using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>
/// Organization-owned credit authorization for one POSCustomer.
/// Does not store outstanding — that remains derived from the credit ledger.
/// </summary>
public sealed class CustomerCreditPolicy
{
    public const decimal MaxCreditLimit = 999_999_999.99m;
    public const int MinTermDays = 1;
    public const int MaxTermDays = 365;
    public const int ReasonMaxLength = 512;
    public const string InitialConfigureReason = "Initial customer credit configuration.";

    public CustomerCreditPolicyId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public POSCustomerId CustomerId { get; }
    public CustomerCreditPolicyStatus Status { get; private set; }
    public decimal CreditLimit { get; private set; }
    public int DefaultTermDays { get; private set; }
    public Guid ConfiguredByUserId { get; private set; }
    public DateTimeOffset ConfiguredAtUtc { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public Guid UpdatedByUserId { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private CustomerCreditPolicy(
        CustomerCreditPolicyId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
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
        OrganizationId = organizationId;
        CustomerId = customerId;
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

    public static CustomerCreditPolicy Rehydrate(
        CustomerCreditPolicyId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
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
            organizationId,
            customerId,
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
    public static (CustomerCreditPolicy Policy, CustomerCreditPolicyChange Change) Configure(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal creditLimit,
        int defaultTermDays,
        Guid actorUserId,
        string? reason,
        DateTimeOffset utcNow,
        CustomerCreditPolicyId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorUserId);
        var limit = NormalizeCreditLimit(creditLimit);
        var term = NormalizeTermDays(defaultTermDays);
        var normalizedReason = NormalizeReason(reason, allowDefault: true, InitialConfigureReason);
        var policyId = id ?? CustomerCreditPolicyId.New();

        var policy = new CustomerCreditPolicy(
            policyId,
            organizationId,
            customerId,
            CustomerCreditPolicyStatus.PendingApproval,
            limit,
            term,
            actorUserId,
            utcNow,
            null,
            null,
            actorUserId,
            utcNow);

        var change = CustomerCreditPolicyChange.Create(
            organizationId,
            customerId,
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

    public CustomerCreditPolicyChange UpdateTerms(
        decimal creditLimit,
        int defaultTermDays,
        Guid actorUserId,
        string reason,
        DateTimeOffset utcNow)
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

        var limit = NormalizeCreditLimit(creditLimit);
        var term = NormalizeTermDays(defaultTermDays);
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

        var normalizedReason = NormalizeReason(reason, allowDefault: false, null);

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
        if (previousStatus == CustomerCreditPolicyStatus.Disabled
            || ConfiguredByUserId == Guid.Empty)
        {
            ConfiguredByUserId = actorUserId;
            ConfiguredAtUtc = utcNow;
        }

        return CustomerCreditPolicyChange.Create(
            OrganizationId,
            CustomerId,
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

    public CustomerCreditPolicyChange Approve(
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

        var normalizedReason = NormalizeReason(reason, allowDefault: false, null);
        var previous = Status;
        Status = CustomerCreditPolicyStatus.Approved;
        ApprovedByUserId = actorUserId;
        ApprovedAtUtc = utcNow;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = utcNow;

        return CustomerCreditPolicyChange.Create(
            OrganizationId,
            CustomerId,
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

    public CustomerCreditPolicyChange Disable(
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

        var normalizedReason = NormalizeReason(reason, allowDefault: false, null);
        var previous = Status;
        Status = CustomerCreditPolicyStatus.Disabled;
        UpdatedByUserId = actorUserId;
        UpdatedAtUtc = utcNow;

        return CustomerCreditPolicyChange.Create(
            OrganizationId,
            CustomerId,
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

    public static decimal AvailableCredit(CustomerCreditPolicyStatus status, decimal creditLimit, decimal outstanding)
    {
        if (status != CustomerCreditPolicyStatus.Approved)
        {
            return 0m;
        }

        var available = creditLimit - outstanding;
        return available <= 0m ? 0m : NormalizeCreditLimit(available);
    }

    public static DateOnly ComputeDefaultDueDate(DateOnly saleDate, int defaultTermDays) =>
        saleDate.AddDays(NormalizeTermDays(defaultTermDays));

    public static decimal NormalizeCreditLimit(decimal amount)
    {
        if (amount < 0m || amount > MaxCreditLimit)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditLimit,
                $"Credit limit must be between 0 and {MaxCreditLimit:0.00}.");
        }

        var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (rounded != amount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditLimit,
                "Credit limit must have at most two decimal places.");
        }

        return rounded;
    }

    public static int NormalizeTermDays(int days)
    {
        if (days < MinTermDays || days > MaxTermDays)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditTermDays,
                $"Default term days must be between {MinTermDays} and {MaxTermDays}.");
        }

        return days;
    }

    public static string NormalizeReason(string? reason, bool allowDefault, string? defaultReason)
    {
        var trimmed = reason?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            if (allowDefault && !string.IsNullOrWhiteSpace(defaultReason))
            {
                return defaultReason!;
            }

            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditPolicyReason,
                "A non-empty reason is required.");
        }

        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditPolicyReason,
                $"Reason cannot exceed {ReasonMaxLength} characters.");
        }

        return trimmed;
    }

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
