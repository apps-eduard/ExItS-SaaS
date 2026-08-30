using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Durable direct purchase / stock receipt document (no purchase order). Stock is applied
/// atomically on create for tracked products. Correction is void-only (compensating
/// reversal movements); the original receipt is never deleted.
/// </summary>
public sealed class DirectPurchaseReceipt
{
    public const int SourceNameMaxLength = 128;
    public const int ReferenceNumberMaxLength = 128;
    public const int NotesMaxLength = 512;
    public const int IdempotencyKeyMaxLength = 128;
    public const int VoidReasonMaxLength = 512;

    private readonly List<DirectPurchaseReceiptLine> _lines;

    public DirectPurchaseReceiptId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string ReceiptNumber { get; }
    public DateOnly PurchaseDate { get; }
    public SupplierId? SupplierId { get; }
    public string? SourceNameSnapshot { get; }
    public string? ReferenceNumber { get; }
    public string? Notes { get; }
    public decimal TotalCost { get; }
    public Guid CreatedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public string? IdempotencyKey { get; }
    public DirectPurchaseReceiptStatus Status { get; private set; }
    public DateTimeOffset? VoidedAtUtc { get; private set; }
    public Guid? VoidedByUserId { get; private set; }
    public string? VoidReason { get; private set; }

    public IReadOnlyList<DirectPurchaseReceiptLine> Lines => _lines;

    private DirectPurchaseReceipt(
        DirectPurchaseReceiptId id,
        PosOrganizationId organizationId,
        string receiptNumber,
        DateOnly purchaseDate,
        SupplierId? supplierId,
        string? sourceNameSnapshot,
        string? referenceNumber,
        string? notes,
        decimal totalCost,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        string? idempotencyKey,
        List<DirectPurchaseReceiptLine> lines,
        DirectPurchaseReceiptStatus status = DirectPurchaseReceiptStatus.Posted,
        DateTimeOffset? voidedAtUtc = null,
        Guid? voidedByUserId = null,
        string? voidReason = null)
    {
        Id = id;
        OrganizationId = organizationId;
        ReceiptNumber = receiptNumber;
        PurchaseDate = purchaseDate;
        SupplierId = supplierId;
        SourceNameSnapshot = sourceNameSnapshot;
        ReferenceNumber = referenceNumber;
        Notes = notes;
        TotalCost = totalCost;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        IdempotencyKey = idempotencyKey;
        Status = status;
        VoidedAtUtc = voidedAtUtc;
        VoidedByUserId = voidedByUserId;
        VoidReason = voidReason;
        _lines = lines;
    }

    public static DirectPurchaseReceipt Create(
        PosOrganizationId organizationId,
        string receiptNumber,
        DateOnly purchaseDate,
        IReadOnlyList<DirectPurchaseReceiptLineDraft> lines,
        Guid createdByUserId,
        DateTimeOffset utcNow,
        SupplierId? supplierId = null,
        string? sourceName = null,
        string? referenceNumber = null,
        string? notes = null,
        string? idempotencyKey = null,
        DirectPurchaseReceiptId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(createdByUserId);

        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.DirectPurchaseRequiresLines,
                "At least one direct purchase line is required.");
        }

        var receiptId = id ?? DirectPurchaseReceiptId.New();
        var built = new List<DirectPurchaseReceiptLine>(lines.Count);
        var lineNumber = 1;
        foreach (var draft in lines)
        {
            built.Add(DirectPurchaseReceiptLine.Create(receiptId, organizationId, lineNumber++, draft));
        }

        var total = SaleMoney.RoundMoney(built.Sum(l => l.LineTotal));
        return new DirectPurchaseReceipt(
            receiptId,
            organizationId,
            DirectPurchaseReceiptNumbers.Normalize(receiptNumber),
            purchaseDate,
            supplierId,
            NormalizeSourceName(sourceName),
            NormalizeOptional(referenceNumber, ReferenceNumberMaxLength, DomainErrorCodes.InvalidDirectPurchaseReference, "Reference number"),
            NormalizeOptional(notes, NotesMaxLength, DomainErrorCodes.InvalidDirectPurchaseNotes, "Notes"),
            total,
            createdByUserId,
            utcNow,
            NormalizeIdempotencyKey(idempotencyKey),
            built,
            DirectPurchaseReceiptStatus.Posted);
    }

    /// <summary>
    /// Marks the document voided. Inventory reversal is applied by the use case.
    /// </summary>
    public void Void(DateTimeOffset utcNow, Guid actorId, string reason)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(actorId);

        if (Status == DirectPurchaseReceiptStatus.Voided)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptStatusTransition,
                "Direct purchase receipt is already voided.");
        }

        if (Status != DirectPurchaseReceiptStatus.Posted)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptStatusTransition,
                "Only a posted direct purchase receipt can be voided.");
        }

        VoidReason = NormalizeVoidReason(reason);
        Status = DirectPurchaseReceiptStatus.Voided;
        VoidedAtUtc = utcNow;
        VoidedByUserId = actorId;
    }

    public static DirectPurchaseReceipt Rehydrate(
        DirectPurchaseReceiptId id,
        PosOrganizationId organizationId,
        string receiptNumber,
        DateOnly purchaseDate,
        SupplierId? supplierId,
        string? sourceNameSnapshot,
        string? referenceNumber,
        string? notes,
        decimal totalCost,
        Guid createdByUserId,
        DateTimeOffset createdAtUtc,
        string? idempotencyKey,
        IReadOnlyList<DirectPurchaseReceiptLine> lines,
        DirectPurchaseReceiptStatus status = DirectPurchaseReceiptStatus.Posted,
        DateTimeOffset? voidedAtUtc = null,
        Guid? voidedByUserId = null,
        string? voidReason = null) =>
        new(
            id,
            organizationId,
            receiptNumber,
            purchaseDate,
            supplierId,
            sourceNameSnapshot,
            referenceNumber,
            notes,
            totalCost,
            createdByUserId,
            createdAtUtc,
            idempotencyKey,
            lines.ToList(),
            status,
            voidedAtUtc,
            voidedByUserId,
            voidReason);

    private static string NormalizeVoidReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseVoidReason,
                "A void reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > VoidReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseVoidReason,
                $"Void reason must be at most {VoidReasonMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeSourceName(string? sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return null;
        }

        var trimmed = sourceName.Trim();
        if (trimmed.Length > SourceNameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseSourceName,
                $"Source name must be at most {SourceNameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string errorCode, string label)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{label} must be at most {maxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var trimmed = key.Trim();
        if (trimmed.Length > IdempotencyKeyMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseIdempotencyKey,
                $"Idempotency key must be at most {IdempotencyKeyMaxLength} characters.");
        }

        return trimmed;
    }
}
