using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>
/// Organization-owned Business Utang write-off (uncollectible recognition). Append-only after create:
/// amount and reason cannot be edited; corrections use explicit reversal with a reason.
/// Not a repayment, credit reversal, discount, or delete.
/// </summary>
public sealed class WriteOff
{
    public const int ReasonMaxLength = 512;
    public const int ReversalReasonMaxLength = 512;
    public const decimal MaxAmount = 999_999_999.99m;

    public WriteOffId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public POSCustomerId CustomerId { get; }
    public decimal Amount { get; }
    public string Reason { get; }
    public WriteOffStatus Status { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; }
    public Guid RecordedBy { get; }
    public DateTimeOffset? ReversedAtUtc { get; private set; }
    public string? ReversalReason { get; private set; }
    public Guid? ReversedBy { get; private set; }

    private WriteOff(
        WriteOffId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string reason,
        WriteOffStatus status,
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
        Reason = reason;
        Status = status;
        RecordedAtUtc = recordedAtUtc;
        RecordedBy = recordedBy;
        ReversedAtUtc = reversedAtUtc;
        ReversalReason = reversalReason;
        ReversedBy = reversedBy;
    }

    public static WriteOff Create(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string reason,
        Guid recordedBy,
        DateTimeOffset utcNow,
        WriteOffId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(recordedBy, DomainErrorCodes.InvalidWriteOffActor);
        return new WriteOff(
            id ?? WriteOffId.New(),
            organizationId,
            customerId,
            NormalizeAmount(amount),
            NormalizeReason(reason),
            WriteOffStatus.Active,
            utcNow,
            recordedBy,
            null,
            null,
            null);
    }

    public static WriteOff Rehydrate(
        WriteOffId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string reason,
        WriteOffStatus status,
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
            reason,
            status,
            recordedAtUtc,
            recordedBy,
            reversedAtUtc,
            reversalReason,
            reversedBy);

    public void Reverse(string reason, Guid reversedBy, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(reversedBy, DomainErrorCodes.InvalidWriteOffActor);
        if (Status == WriteOffStatus.Reversed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWriteOffStatusTransition,
                "Write-off is already reversed.");
        }

        Status = WriteOffStatus.Reversed;
        ReversedAtUtc = utcNow;
        ReversalReason = NormalizeReversalReason(reason);
        ReversedBy = reversedBy;
    }

    public static decimal NormalizeAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWriteOffAmount,
                "Write-off amount must be a positive decimal.");
        }

        if (amount > MaxAmount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWriteOffAmount,
                $"Write-off amount must be at most {MaxAmount}.");
        }

        var rounded = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (rounded != amount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWriteOffAmount,
                "Write-off amount may have at most two decimal places.");
        }

        return rounded;
    }

    public static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWriteOffReason,
                "Write-off reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWriteOffReason,
                $"Write-off reason must be at most {ReasonMaxLength} characters.");
        }

        return trimmed;
    }

    public static string NormalizeReversalReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWriteOffReversalReason,
                "Reversal reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReversalReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWriteOffReversalReason,
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
