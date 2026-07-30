using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>
/// Organization-owned customer Utang repayment. Append-only after create:
/// amount and remarks cannot be edited; corrections use explicit reversal with a reason.
/// Not a SaaS subscription payment, retail sale payment, wallet top-up, or credit entry.
/// </summary>
public sealed class Repayment
{
    public const int RemarksMaxLength = 512;
    public const int ReversalReasonMaxLength = 512;
    public const decimal MaxAmount = 999_999_999.99m;

    public RepaymentId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public POSCustomerId CustomerId { get; }
    public decimal Amount { get; }
    public string? Remarks { get; }
    public RepaymentStatus Status { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; }
    public Guid RecordedBy { get; }
    public DateTimeOffset? ReversedAtUtc { get; private set; }
    public string? ReversalReason { get; private set; }
    public Guid? ReversedBy { get; private set; }

    private Repayment(
        RepaymentId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string? remarks,
        RepaymentStatus status,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy,
        DateTimeOffset? reversedAtUtc,
        string? reversalReason,
        Guid? reversedBy)
    {
        Id = id;
        OrganizationId = organizationId;
        CustomerId = customerId;
        Amount = amount;
        Remarks = remarks;
        Status = status;
        RecordedAtUtc = recordedAtUtc;
        RecordedBy = recordedBy;
        ReversedAtUtc = reversedAtUtc;
        ReversalReason = reversalReason;
        ReversedBy = reversedBy;
    }

    public static Repayment Create(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string? remarks,
        Guid recordedBy,
        DateTimeOffset utcNow,
        RepaymentId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(recordedBy, DomainErrorCodes.InvalidRepaymentActor);
        return new Repayment(
            id ?? RepaymentId.New(),
            organizationId,
            customerId,
            NormalizeAmount(amount),
            NormalizeOptionalRemarks(remarks),
            RepaymentStatus.Active,
            utcNow,
            recordedBy,
            null,
            null,
            null);
    }

    public static Repayment Rehydrate(
        RepaymentId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string? remarks,
        RepaymentStatus status,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy,
        DateTimeOffset? reversedAtUtc,
        string? reversalReason,
        Guid? reversedBy) =>
        new(
            id,
            organizationId,
            customerId,
            amount,
            remarks,
            status,
            recordedAtUtc,
            recordedBy,
            reversedAtUtc,
            reversalReason,
            reversedBy);

    public void Reverse(string reason, Guid reversedBy, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(reversedBy, DomainErrorCodes.InvalidRepaymentActor);
        if (Status == RepaymentStatus.Reversed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentStatusTransition,
                "Repayment is already reversed.");
        }

        Status = RepaymentStatus.Reversed;
        ReversedAtUtc = utcNow;
        ReversalReason = NormalizeReversalReason(reason);
        ReversedBy = reversedBy;
    }

    public static decimal NormalizeAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentAmount,
                "Repayment amount must be a positive decimal.");
        }

        if (amount > MaxAmount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentAmount,
                $"Repayment amount must be at most {MaxAmount}.");
        }

        var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (rounded != amount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentAmount,
                "Repayment amount may have at most two decimal places.");
        }

        return rounded;
    }

    public static string? NormalizeOptionalRemarks(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            return null;
        }

        var trimmed = remarks.Trim();
        if (trimmed.Length > RemarksMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentRemarks,
                $"Remarks must be at most {RemarksMaxLength} characters.");
        }

        return trimmed;
    }

    public static string NormalizeReversalReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentReversalReason,
                "Reversal reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReversalReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentReversalReason,
                $"Reversal reason must be at most {ReversalReasonMaxLength} characters.");
        }

        return trimmed;
    }

    private static void EnsureActor(Guid actorId, string errorCode)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(errorCode, "Actor id must be a non-empty GUID.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
