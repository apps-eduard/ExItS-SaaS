using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>
/// Seller-owned B2B Utang repayment against a buyer organization connection.
/// Same Check clearing semantics as personal <see cref="Repayment"/> — customer kind does not change rules.
/// </summary>
public sealed class BusinessRepayment
{
    public const int RemarksMaxLength = Repayment.RemarksMaxLength;
    public const int ReversalReasonMaxLength = Repayment.ReversalReasonMaxLength;

    public BusinessRepaymentId Id { get; }
    public PosOrganizationId SellerOrganizationId { get; }
    public PosOrganizationId BuyerOrganizationId { get; }
    public Guid ConnectionId { get; }
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

    public bool ReducesOutstanding =>
        UtangCheckPayment.ReducesOutstanding(Status, PaymentMethod, CheckClearingStatus);

    public bool IsPendingCheck =>
        UtangCheckPayment.IsPendingCheck(Status, PaymentMethod, CheckClearingStatus);

    private BusinessRepayment(
        BusinessRepaymentId id,
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        Guid connectionId,
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
        SellerOrganizationId = sellerOrganizationId;
        BuyerOrganizationId = buyerOrganizationId;
        ConnectionId = connectionId;
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

    public static BusinessRepayment Create(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        Guid connectionId,
        decimal amount,
        string? remarks,
        Guid recordedBy,
        DateTimeOffset utcNow,
        BusinessRepaymentId? id = null,
        UtangPaymentMethod paymentMethod = UtangPaymentMethod.Cash,
        string? checkNumber = null,
        string? bankName = null,
        DateOnly? checkDate = null,
        string? accountName = null,
        string? reference = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(recordedBy);
        if (connectionId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessRepaymentId,
                "Connection id must be a non-empty GUID.");
        }

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

        return new BusinessRepayment(
            id ?? BusinessRepaymentId.New(),
            sellerOrganizationId,
            buyerOrganizationId,
            connectionId,
            Repayment.NormalizeAmount(amount),
            Repayment.NormalizeOptionalRemarks(remarks),
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

    public static BusinessRepayment Rehydrate(
        BusinessRepaymentId id,
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        Guid connectionId,
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
        string? cancelReason) =>
        new(
            id,
            sellerOrganizationId,
            buyerOrganizationId,
            connectionId,
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
        EnsureActor(reversedBy);
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
        ReversalReason = Repayment.NormalizeReversalReason(reason);
        ReversedBy = reversedBy;
    }

    public bool MarkCleared(Guid clearedBy, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureActor(clearedBy);
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
        EnsureActor(bouncedBy);
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
        EnsureActor(cancelledBy);
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

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentActor,
                "Actor id must be a non-empty GUID.");
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
