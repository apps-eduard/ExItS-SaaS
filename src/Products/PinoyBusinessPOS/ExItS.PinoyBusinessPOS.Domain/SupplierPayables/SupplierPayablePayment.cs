namespace ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

/// <summary>
/// Immutable posted payment against a supplier payable. Only created via
/// <see cref="SupplierPayable.ApplyPayment"/> after the payable exists.
/// Receipt PaidNow is NOT a payment row — it is stored as PaidAtReceiptAmount on the payable
/// (PAID_AT_RECEIPT vs POSTED_PAYMENTS).
/// </summary>
public sealed class SupplierPayablePayment
{
    public const int ReferenceMaxLength = 128;
    public const int NotesMaxLength = 512;

    public SupplierPayablePaymentId Id { get; }
    public SupplierPayableId PayableId { get; }
    public decimal Amount { get; }
    public SupplierPayablePaymentMethod PaymentMethod { get; }
    public string? Reference { get; }
    public string? Notes { get; }
    public DateTimeOffset PaidAtUtc { get; }
    public Guid RecordedBy { get; }
    public DateTimeOffset RecordedAtUtc { get; }

    private SupplierPayablePayment(
        SupplierPayablePaymentId id,
        SupplierPayableId payableId,
        decimal amount,
        SupplierPayablePaymentMethod paymentMethod,
        string? reference,
        string? notes,
        DateTimeOffset paidAtUtc,
        Guid recordedBy,
        DateTimeOffset recordedAtUtc)
    {
        Id = id;
        PayableId = payableId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        Reference = reference;
        Notes = notes;
        PaidAtUtc = paidAtUtc;
        RecordedBy = recordedBy;
        RecordedAtUtc = recordedAtUtc;
    }

    internal static SupplierPayablePayment Create(
        SupplierPayableId payableId,
        decimal amount,
        SupplierPayablePaymentMethod paymentMethod,
        Guid recordedBy,
        DateTimeOffset utcNow,
        DateTimeOffset? paidAtUtc = null,
        string? reference = null,
        string? notes = null,
        SupplierPayablePaymentId? id = null)
    {
        SupplierPayableMoney.EnsureUtc(utcNow);
        SupplierPayableMoney.EnsureActor(recordedBy);

        var paidAt = paidAtUtc ?? utcNow;
        SupplierPayableMoney.EnsureUtc(paidAt);

        return new SupplierPayablePayment(
            id ?? SupplierPayablePaymentId.New(),
            payableId,
            SupplierPayableMoney.NormalizePositiveAmount(
                amount,
                Domain.Common.DomainErrorCodes.InvalidSupplierPayablePaymentAmount,
                "Payment amount"),
            paymentMethod,
            NormalizeOptionalText(reference, ReferenceMaxLength, Domain.Common.DomainErrorCodes.InvalidSupplierPayablePaymentReference, "Payment reference"),
            NormalizeOptionalText(notes, NotesMaxLength, Domain.Common.DomainErrorCodes.InvalidSupplierPayablePaymentNotes, "Payment notes"),
            paidAt,
            recordedBy,
            utcNow);
    }

    public static SupplierPayablePayment Rehydrate(
        SupplierPayablePaymentId id,
        SupplierPayableId payableId,
        decimal amount,
        SupplierPayablePaymentMethod paymentMethod,
        string? reference,
        string? notes,
        DateTimeOffset paidAtUtc,
        Guid recordedBy,
        DateTimeOffset recordedAtUtc) =>
        new(id, payableId, amount, paymentMethod, reference, notes, paidAtUtc, recordedBy, recordedAtUtc);

    private static string? NormalizeOptionalText(string? value, int maxLength, string errorCode, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new Domain.Common.DomainException(
                errorCode,
                $"{fieldLabel} must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
