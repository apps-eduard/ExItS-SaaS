using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

/// <summary>
/// Organization-scoped supplier payable arising from a posted purchase receipt (ADR-023).
/// <para>
/// PAID_AT_RECEIPT vs POSTED_PAYMENTS: receipt PaidNow settles into
/// <see cref="PaidAtReceiptAmount"/> (and aggregate <see cref="PaidAmount"/>) without creating a
/// <see cref="SupplierPayablePayment"/> row. Later "Record Payment" actions create payment children.
/// Receipt reversal may void the payable only when there are zero payment children.
/// </para>
/// </summary>
public sealed class SupplierPayable
{
    public const int VoidReasonMaxLength = 512;

    private readonly List<SupplierPayablePayment> _payments;

    public SupplierPayableId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public SupplierId SupplierId { get; }
    public SupplierPayableSourceType SourceType { get; }
    public Guid SourceId { get; }
    public decimal OriginalAmount { get; }
    /// <summary>Immutable snapshot of amount settled at receipt post (not a payment row).</summary>
    public decimal PaidAtReceiptAmount { get; }
    /// <summary>Aggregate settled = PaidAtReceiptAmount + sum of posted payments.</summary>
    public decimal PaidAmount { get; private set; }
    public decimal Balance { get; private set; }
    public SupplierPayableStatus Status { get; private set; }
    public DateOnly? DueDate { get; }
    /// <summary>Method used for the paid-at-receipt portion when PaidAtReceiptAmount &gt; 0.</summary>
    public SupplierPayablePaymentMethod? PaymentMethodAtReceipt { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public Guid CreatedBy { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public Guid? VoidedBy { get; private set; }
    public string? VoidReason { get; private set; }

    public IReadOnlyList<SupplierPayablePayment> Payments => _payments;

    /// <summary>True when any later posted <see cref="SupplierPayablePayment"/> rows exist.</summary>
    public bool HasPostedPayments => _payments.Count > 0;

    private SupplierPayable(
        SupplierPayableId id,
        PosOrganizationId organizationId,
        SupplierId supplierId,
        SupplierPayableSourceType sourceType,
        Guid sourceId,
        decimal originalAmount,
        decimal paidAtReceiptAmount,
        decimal paidAmount,
        decimal balance,
        SupplierPayableStatus status,
        DateOnly? dueDate,
        SupplierPayablePaymentMethod? paymentMethodAtReceipt,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? voidedAtUtc,
        Guid? voidedBy,
        string? voidReason,
        List<SupplierPayablePayment> payments)
    {
        Id = id;
        OrganizationId = organizationId;
        SupplierId = supplierId;
        SourceType = sourceType;
        SourceId = sourceId;
        OriginalAmount = originalAmount;
        PaidAtReceiptAmount = paidAtReceiptAmount;
        PaidAmount = paidAmount;
        Balance = balance;
        Status = status;
        DueDate = dueDate;
        PaymentMethodAtReceipt = paymentMethodAtReceipt;
        CreatedAtUtc = createdAtUtc;
        CreatedBy = createdBy;
        UpdatedAtUtc = updatedAtUtc;
        VoidedAtUtc = voidedAtUtc;
        VoidedBy = voidedBy;
        VoidReason = voidReason;
        _payments = payments;
    }

    /// <summary>
    /// Creates a payable from a posted receipt. FULLY_PAID_RECEIPT_POLICY=A:
    /// always create; Status=Paid when paidAtReceipt >= original, else Open/PartiallyPaid.
    /// Default paid-at-receipt is the full original when <paramref name="paidNow"/> is null.
    /// </summary>
    public static SupplierPayable Create(
        PosOrganizationId organizationId,
        SupplierId supplierId,
        SupplierPayableSourceType sourceType,
        Guid sourceId,
        decimal originalAmount,
        Guid createdBy,
        DateTimeOffset utcNow,
        decimal? paidNow = null,
        DateOnly? dueDate = null,
        SupplierPayablePaymentMethod? paymentMethodAtReceipt = null,
        SupplierPayableId? id = null)
    {
        SupplierPayableMoney.EnsureUtc(utcNow);
        SupplierPayableMoney.EnsureActor(createdBy);

        if (sourceId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierPayableSourceId,
                "Source receipt id is required.");
        }

        var original = SupplierPayableMoney.NormalizePositiveAmount(
            originalAmount,
            DomainErrorCodes.InvalidSupplierPayableAmount,
            "Original amount");

        var paidAtReceipt = SupplierPayableMoney.NormalizeNonNegativeAmount(
            paidNow ?? original,
            DomainErrorCodes.InvalidSupplierPayablePaidAtReceipt,
            "Paid-at-receipt amount");

        if (paidAtReceipt > original)
        {
            throw new DomainException(
                DomainErrorCodes.SupplierPayableOverpayNotAllowed,
                "Paid-at-receipt amount cannot exceed the receipt total.");
        }

        SupplierPayablePaymentMethod? methodAtReceipt = null;
        if (paidAtReceipt > 0m)
        {
            methodAtReceipt = paymentMethodAtReceipt ?? SupplierPayablePaymentMethod.Cash;
        }
        else if (paymentMethodAtReceipt is not null)
        {
            methodAtReceipt = paymentMethodAtReceipt;
        }

        var balance = SupplierPayableMoney.RoundMoney(original - paidAtReceipt);
        var status = ResolveStatus(paidAtReceipt, balance);

        return new SupplierPayable(
            id ?? SupplierPayableId.New(),
            organizationId,
            supplierId,
            sourceType,
            sourceId,
            original,
            paidAtReceipt,
            paidAtReceipt,
            balance,
            status,
            dueDate,
            methodAtReceipt,
            utcNow,
            createdBy,
            utcNow,
            null,
            null,
            null,
            []);
    }

    public static SupplierPayable Rehydrate(
        SupplierPayableId id,
        PosOrganizationId organizationId,
        SupplierId supplierId,
        SupplierPayableSourceType sourceType,
        Guid sourceId,
        decimal originalAmount,
        decimal paidAtReceiptAmount,
        decimal paidAmount,
        decimal balance,
        SupplierPayableStatus status,
        DateOnly? dueDate,
        SupplierPayablePaymentMethod? paymentMethodAtReceipt,
        DateTimeOffset createdAtUtc,
        Guid createdBy,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? voidedAtUtc,
        Guid? voidedBy,
        string? voidReason,
        IReadOnlyList<SupplierPayablePayment>? payments = null) =>
        new(
            id,
            organizationId,
            supplierId,
            sourceType,
            sourceId,
            originalAmount,
            paidAtReceiptAmount,
            paidAmount,
            balance,
            status,
            dueDate,
            paymentMethodAtReceipt,
            createdAtUtc,
            createdBy,
            updatedAtUtc,
            voidedAtUtc,
            voidedBy,
            voidReason,
            payments?.ToList() ?? []);

    /// <summary>
    /// Records a later settlement payment. OVERPAY_ALLOWED=NO — amount must not exceed balance.
    /// Does not change inventory, unit cost, or receipt line costs.
    /// </summary>
    public SupplierPayablePayment ApplyPayment(
        decimal amount,
        SupplierPayablePaymentMethod paymentMethod,
        Guid recordedBy,
        DateTimeOffset utcNow,
        DateTimeOffset? paidAtUtc = null,
        string? reference = null,
        string? notes = null,
        SupplierPayablePaymentId? paymentId = null)
    {
        SupplierPayableMoney.EnsureUtc(utcNow);
        SupplierPayableMoney.EnsureActor(recordedBy);

        if (Status is SupplierPayableStatus.Voided or SupplierPayableStatus.Paid)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierPayableStatusTransition,
                Status == SupplierPayableStatus.Voided
                    ? "Cannot record a payment on a voided payable."
                    : "Payable is already fully paid.");
        }

        var payment = SupplierPayablePayment.Create(
            Id,
            amount,
            paymentMethod,
            recordedBy,
            utcNow,
            paidAtUtc,
            reference,
            notes,
            paymentId);

        if (payment.Amount > Balance)
        {
            throw new DomainException(
                DomainErrorCodes.SupplierPayableOverpayNotAllowed,
                "Payment amount cannot exceed the outstanding balance.");
        }

        _payments.Add(payment);
        PaidAmount = SupplierPayableMoney.RoundMoney(PaidAmount + payment.Amount);
        Balance = SupplierPayableMoney.RoundMoney(OriginalAmount - PaidAmount);
        Status = ResolveStatus(PaidAmount, Balance);
        UpdatedAtUtc = utcNow;
        return payment;
    }

    /// <summary>
    /// Voids the payable. Allowed only when there are zero <see cref="SupplierPayablePayment"/>
    /// children (PaidAtReceipt alone is OK). Used by receipt reversal when no posted payments exist.
    /// </summary>
    public void Void(string reason, Guid voidedBy, DateTimeOffset utcNow)
    {
        SupplierPayableMoney.EnsureUtc(utcNow);
        SupplierPayableMoney.EnsureActor(voidedBy);

        if (Status == SupplierPayableStatus.Voided)
        {
            return;
        }

        if (HasPostedPayments)
        {
            throw new DomainException(
                DomainErrorCodes.SupplierPayableVoidBlockedByPayments,
                "Cannot void a supplier payable that already has recorded payments.");
        }

        var normalizedReason = NormalizeVoidReason(reason);
        Status = SupplierPayableStatus.Voided;
        VoidedAtUtc = utcNow;
        VoidedBy = voidedBy;
        VoidReason = normalizedReason;
        UpdatedAtUtc = utcNow;
    }

    public static string NormalizeVoidReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierPayableVoidReason,
                "A void reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > VoidReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierPayableVoidReason,
                $"Void reason must be at most {VoidReasonMaxLength} characters.");
        }

        return trimmed;
    }

    private static SupplierPayableStatus ResolveStatus(decimal paidAmount, decimal balance)
    {
        if (balance == 0m)
        {
            return SupplierPayableStatus.Paid;
        }

        return paidAmount > 0m ? SupplierPayableStatus.PartiallyPaid : SupplierPayableStatus.Open;
    }
}
