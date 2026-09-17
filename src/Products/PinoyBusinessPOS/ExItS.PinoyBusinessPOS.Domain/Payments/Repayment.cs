using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>
/// Organization-owned customer Utang repayment. Append-only after create:
/// amount and remarks cannot be edited; corrections use explicit reversal (settled) or check disposition.
/// Not a SaaS subscription payment, retail sale payment, wallet top-up, or credit entry.
/// Check received (PendingClearing) does not reduce outstanding until Cleared.
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
    public UtangPaymentMethod PaymentMethod { get; }
    public string? CheckNumber { get; }
    public string? BankName { get; }
    public DateOnly? CheckDate { get; }
    public string? AccountName { get; }
    public string? Reference { get; }
    public UtangCheckClearingStatus CheckClearingStatus { get; private set; }
    public RepaymentStatus Status { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; }
    public Guid RecordedBy { get; }
    public DateTimeOffset? ReversedAtUtc { get; private set; }
    public string? ReversalReason { get; private set; }
    public Guid? ReversedBy { get; private set; }
    public DateTimeOffset? ClearedAtUtc { get; private set; }
    public Guid? ClearedBy { get; private set; }
    public DateTimeOffset? BouncedAtUtc { get; private set; }
    public Guid? BouncedBy { get; private set; }
    public string? BounceReason { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public string? CancelReason { get; private set; }

    /// <summary>True when this repayment reduces settled outstanding balance.</summary>
    public bool ReducesOutstanding =>
        UtangCheckPayment.ReducesOutstanding(Status, PaymentMethod, CheckClearingStatus);

    public bool IsPendingCheck =>
        UtangCheckPayment.IsPendingCheck(Status, PaymentMethod, CheckClearingStatus);

    private Repayment(
        RepaymentId id,
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string? remarks,
        UtangPaymentMethod paymentMethod,
        string? checkNumber,
        string? bankName,
        DateOnly? checkDate,
        string? accountName,
        string? reference,
        UtangCheckClearingStatus checkClearingStatus,
        RepaymentStatus status,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy,
        DateTimeOffset? reversedAtUtc,
        string? reversalReason,
        Guid? reversedBy,
        DateTimeOffset? clearedAtUtc,
        Guid? clearedBy,
        DateTimeOffset? bouncedAtUtc,
        Guid? bouncedBy,
        string? bounceReason,
        DateTimeOffset? cancelledAtUtc,
        Guid? cancelledBy,
        string? cancelReason)
    {
        Id = id;
        OrganizationId = organizationId;
        CustomerId = customerId;
        Amount = amount;
        Remarks = remarks;
        PaymentMethod = paymentMethod;
        CheckNumber = checkNumber;
        BankName = bankName;
        CheckDate = checkDate;
        AccountName = accountName;
        Reference = reference;
        CheckClearingStatus = checkClearingStatus;
        Status = status;
        RecordedAtUtc = recordedAtUtc;
        RecordedBy = recordedBy;
        ReversedAtUtc = reversedAtUtc;
        ReversalReason = reversalReason;
        ReversedBy = reversedBy;
        ClearedAtUtc = clearedAtUtc;
        ClearedBy = clearedBy;
        BouncedAtUtc = bouncedAtUtc;
        BouncedBy = bouncedBy;
        BounceReason = bounceReason;
        CancelledAtUtc = cancelledAtUtc;
        CancelledBy = cancelledBy;
        CancelReason = cancelReason;
    }

    /// <summary>
    /// Creates a settled Cash repayment by default. Pass <see cref="UtangPaymentMethod.Check"/> with
    /// required check fields for pending clearing.
    /// </summary>
    public static Repayment Create(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        decimal amount,
        string? remarks,
        Guid recordedBy,
        DateTimeOffset utcNow,
        RepaymentId? id = null,
        UtangPaymentMethod paymentMethod = UtangPaymentMethod.Cash,
        string? checkNumber = null,
        string? bankName = null,
        DateOnly? checkDate = null,
        string? accountName = null,
        string? reference = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(recordedBy, DomainErrorCodes.InvalidRepaymentActor);
        UtangCheckPayment.EnsureNonCheckHasNoCheckFields(
            paymentMethod, checkNumber, bankName, checkDate, accountName, reference);

        string? normalizedCheckNumber = null;
        string? normalizedBankName = null;
        DateOnly? normalizedCheckDate = null;
        string? normalizedAccountName = null;
        string? normalizedReference = null;
        var clearing = UtangCheckClearingStatus.None;

        if (paymentMethod == UtangPaymentMethod.Check)
        {
            normalizedCheckNumber = UtangCheckPayment.NormalizeRequiredCheckNumber(checkNumber);
            normalizedBankName = UtangCheckPayment.NormalizeRequiredBankName(bankName);
            normalizedCheckDate = UtangCheckPayment.NormalizeRequiredCheckDate(checkDate);
            normalizedAccountName = UtangCheckPayment.NormalizeOptionalAccountName(accountName);
            normalizedReference = UtangCheckPayment.NormalizeOptionalReference(reference);
            clearing = UtangCheckClearingStatus.PendingClearing;
        }
        else if (paymentMethod is not (UtangPaymentMethod.Cash or UtangPaymentMethod.ManualGCash))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangPaymentMethod,
                "Payment method must be Cash, ManualGCash, or Check.");
        }

        return new Repayment(
            id ?? RepaymentId.New(),
            organizationId,
            customerId,
            NormalizeAmount(amount),
            NormalizeOptionalRemarks(remarks),
            paymentMethod,
            normalizedCheckNumber,
            normalizedBankName,
            normalizedCheckDate,
            normalizedAccountName,
            normalizedReference,
            clearing,
            RepaymentStatus.Active,
            utcNow,
            recordedBy,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
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
        Guid? reversedBy,
        UtangPaymentMethod paymentMethod = UtangPaymentMethod.Cash,
        string? checkNumber = null,
        string? bankName = null,
        DateOnly? checkDate = null,
        string? accountName = null,
        string? reference = null,
        UtangCheckClearingStatus checkClearingStatus = UtangCheckClearingStatus.None,
        DateTimeOffset? clearedAtUtc = null,
        Guid? clearedBy = null,
        DateTimeOffset? bouncedAtUtc = null,
        Guid? bouncedBy = null,
        string? bounceReason = null,
        DateTimeOffset? cancelledAtUtc = null,
        Guid? cancelledBy = null,
        string? cancelReason = null) =>
        new(
            id,
            organizationId,
            customerId,
            amount,
            remarks,
            paymentMethod,
            checkNumber,
            bankName,
            checkDate,
            accountName,
            reference,
            checkClearingStatus,
            status,
            recordedAtUtc,
            recordedBy,
            reversedAtUtc,
            reversalReason,
            reversedBy,
            clearedAtUtc,
            clearedBy,
            bouncedAtUtc,
            bouncedBy,
            bounceReason,
            cancelledAtUtc,
            cancelledBy,
            cancelReason);

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

        if (PaymentMethod == UtangPaymentMethod.Check
            && CheckClearingStatus == UtangCheckClearingStatus.PendingClearing)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentStatusTransition,
                "Pending checks must be cancelled or bounced; they cannot be reversed.");
        }

        if (PaymentMethod == UtangPaymentMethod.Check
            && CheckClearingStatus is UtangCheckClearingStatus.Bounced or UtangCheckClearingStatus.Cancelled)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentStatusTransition,
                "Disposed checks cannot be reversed.");
        }

        Status = RepaymentStatus.Reversed;
        ReversedAtUtc = utcNow;
        ReversalReason = NormalizeReversalReason(reason);
        ReversedBy = reversedBy;
    }

    /// <summary>
    /// Applies a pending check to outstanding exactly once. Idempotent when already Cleared.
    /// </summary>
    public bool MarkCleared(Guid clearedBy, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(clearedBy, DomainErrorCodes.InvalidRepaymentActor);
        EnsureActiveCheck();

        if (CheckClearingStatus == UtangCheckClearingStatus.Cleared)
        {
            return false;
        }

        if (CheckClearingStatus != UtangCheckClearingStatus.PendingClearing)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangCheckClearingTransition,
                "Only pending checks can be cleared.");
        }

        CheckClearingStatus = UtangCheckClearingStatus.Cleared;
        ClearedAtUtc = utcNow;
        ClearedBy = clearedBy;
        return true;
    }

    public void MarkBounced(Guid bouncedBy, DateTimeOffset utcNow, string? reason = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(bouncedBy, DomainErrorCodes.InvalidRepaymentActor);
        EnsureActiveCheck();

        if (CheckClearingStatus != UtangCheckClearingStatus.PendingClearing)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangCheckClearingTransition,
                "Only pending checks can be marked bounced.");
        }

        CheckClearingStatus = UtangCheckClearingStatus.Bounced;
        BouncedAtUtc = utcNow;
        BouncedBy = bouncedBy;
        BounceReason = UtangCheckPayment.NormalizeOptionalDispositionReason(reason);
    }

    public void CancelCheck(Guid cancelledBy, DateTimeOffset utcNow, string? reason = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(cancelledBy, DomainErrorCodes.InvalidRepaymentActor);
        EnsureActiveCheck();

        if (CheckClearingStatus != UtangCheckClearingStatus.PendingClearing)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangCheckClearingTransition,
                "Only pending checks can be cancelled.");
        }

        CheckClearingStatus = UtangCheckClearingStatus.Cancelled;
        CancelledAtUtc = utcNow;
        CancelledBy = cancelledBy;
        CancelReason = UtangCheckPayment.NormalizeOptionalDispositionReason(reason);
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

    private void EnsureActiveCheck()
    {
        if (Status != RepaymentStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangCheckClearingTransition,
                "Reversed repayments cannot change check clearing status.");
        }

        if (PaymentMethod != UtangPaymentMethod.Check)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtangCheckClearingTransition,
                "Check clearing actions apply only to Check payments.");
        }
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
