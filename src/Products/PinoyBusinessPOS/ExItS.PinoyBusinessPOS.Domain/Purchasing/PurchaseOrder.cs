using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// Organization-owned purchase order. Draft lines are editable; submit freezes product snapshots
/// and allocates a PO number. Receiving is via immutable goods receipts only.
/// </summary>
public sealed class PurchaseOrder
{
    public const int SupplierReferenceMaxLength = 128;
    public const int NotesMaxLength = 512;
    public const int MaxLineCount = 200;

    private readonly List<PurchaseOrderLine> _lines;

    public PurchaseOrderId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public string? PoNumber { get; private set; }
    public SupplierId SupplierId { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public DateOnly OrderDate { get; private set; }
    public DateOnly? ExpectedDeliveryDate { get; private set; }
    public string? SupplierReference { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? OrderedAtUtc { get; private set; }
    public Guid? OrderedBy { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public IReadOnlyList<PurchaseOrderLine> Lines => _lines;

    private PurchaseOrder(
        PurchaseOrderId id,
        PosOrganizationId organizationId,
        string? poNumber,
        SupplierId supplierId,
        PurchaseOrderStatus status,
        DateOnly orderDate,
        DateOnly? expectedDeliveryDate,
        string? supplierReference,
        string? notes,
        DateTimeOffset? orderedAtUtc,
        Guid? orderedBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        List<PurchaseOrderLine> lines)
    {
        Id = id;
        OrganizationId = organizationId;
        PoNumber = poNumber;
        SupplierId = supplierId;
        Status = status;
        OrderDate = orderDate;
        ExpectedDeliveryDate = expectedDeliveryDate;
        SupplierReference = supplierReference;
        Notes = notes;
        OrderedAtUtc = orderedAtUtc;
        OrderedBy = orderedBy;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        _lines = lines;
    }

    public static PurchaseOrder CreateDraft(
        PosOrganizationId organizationId,
        SupplierId supplierId,
        DateOnly orderDate,
        IReadOnlyList<PurchaseOrderLineDraft> lines,
        DateTimeOffset utcNow,
        DateOnly? expectedDeliveryDate = null,
        string? supplierReference = null,
        string? notes = null,
        PurchaseOrderId? id = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureLines(lines);
        ValidateExpectedDelivery(orderDate, expectedDeliveryDate);

        var poId = id ?? PurchaseOrderId.New();
        var poLines = BuildDraftLines(poId, organizationId, lines);

        return new PurchaseOrder(
            poId,
            organizationId,
            poNumber: null,
            supplierId,
            PurchaseOrderStatus.Draft,
            orderDate,
            expectedDeliveryDate,
            NormalizeSupplierReference(supplierReference),
            NormalizeNotes(notes),
            orderedAtUtc: null,
            orderedBy: null,
            utcNow,
            utcNow,
            poLines);
    }

    public void UpdateDraft(
        SupplierId supplierId,
        DateOnly orderDate,
        IReadOnlyList<PurchaseOrderLineDraft> lines,
        DateTimeOffset utcNow,
        DateOnly? expectedDeliveryDate = null,
        string? supplierReference = null,
        string? notes = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureDraft();
        EnsureLines(lines);
        ValidateExpectedDelivery(orderDate, expectedDeliveryDate);

        SupplierId = supplierId;
        OrderDate = orderDate;
        ExpectedDeliveryDate = expectedDeliveryDate;
        SupplierReference = NormalizeSupplierReference(supplierReference);
        Notes = NormalizeNotes(notes);
        ReplaceDraftLines(lines);
        UpdatedAtUtc = utcNow;
    }

    public void Submit(
        string poNumber,
        IReadOnlyList<PurchaseOrderLineSnapshotInput> snapshots,
        Guid orderedBy,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        SaleMoney.EnsureActor(orderedBy);
        EnsureDraft();
        EnsureLines(snapshots);

        if (snapshots.Count != _lines.Count)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderLine,
                "Line snapshot count must match draft lines.");
        }

        var snapshotByProduct = snapshots.ToDictionary(s => s.ProductId.Value);
        foreach (var line in _lines.OrderBy(l => l.LineNumber))
        {
            if (!snapshotByProduct.TryGetValue(line.ProductId.Value, out var snapshot))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Each draft line must have a matching snapshot on submit.");
            }

            line.FreezeSnapshot(snapshot);
        }

        PoNumber = PurchaseOrderNumbers.Normalize(poNumber);
        Status = PurchaseOrderStatus.Ordered;
        OrderedAtUtc = utcNow;
        OrderedBy = orderedBy;
        UpdatedAtUtc = utcNow;
    }

    public void Cancel(DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status is PurchaseOrderStatus.Cancelled)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Purchase order is already cancelled.");
        }

        if (Status is PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Received)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Purchase orders with receipts cannot be cancelled.");
        }

        if (_lines.Any(l => l.ReceivedQty > 0m))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Purchase orders with received quantity cannot be cancelled.");
        }

        Status = PurchaseOrderStatus.Cancelled;
        UpdatedAtUtc = utcNow;
    }

    public void ApplyReceiptLines(IReadOnlyList<PurchaseOrderReceiveLineDraft> receiveLines, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Only ordered purchase orders can receive goods.");
        }

        if (receiveLines is null || receiveLines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseReceiveRequiresLines,
                "At least one receive line is required.");
        }

        var lineByProduct = _lines.ToDictionary(l => l.ProductId.Value);
        foreach (var receive in receiveLines)
        {
            if (!lineByProduct.TryGetValue(receive.ProductId.Value, out var line))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Receive line product is not on this purchase order.");
            }

            line.ApplyReceipt(receive.ReceiveQty);
        }

        Status = _lines.All(l => l.OutstandingQty <= 0m)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;
        UpdatedAtUtc = utcNow;
    }

    public static PurchaseOrder Rehydrate(
        PurchaseOrderId id,
        PosOrganizationId organizationId,
        string? poNumber,
        SupplierId supplierId,
        PurchaseOrderStatus status,
        DateOnly orderDate,
        DateOnly? expectedDeliveryDate,
        string? supplierReference,
        string? notes,
        DateTimeOffset? orderedAtUtc,
        Guid? orderedBy,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        IReadOnlyList<PurchaseOrderLine> lines) =>
        new(
            id,
            organizationId,
            poNumber,
            supplierId,
            status,
            orderDate,
            expectedDeliveryDate,
            supplierReference,
            notes,
            orderedAtUtc,
            orderedBy,
            createdAtUtc,
            updatedAtUtc,
            lines.ToList());

    private void EnsureDraft()
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Only draft purchase orders can be edited.");
        }
    }

    private void ReplaceDraftLines(IReadOnlyList<PurchaseOrderLineDraft> lines)
    {
        _lines.Clear();
        _lines.AddRange(BuildDraftLines(Id, OrganizationId, lines));
    }

    private static List<PurchaseOrderLine> BuildDraftLines(
        PurchaseOrderId poId,
        PosOrganizationId organizationId,
        IReadOnlyList<PurchaseOrderLineDraft> lines)
    {
        EnsureNoDuplicateProducts(lines);
        var result = new List<PurchaseOrderLine>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            result.Add(PurchaseOrderLine.CreateDraft(poId, organizationId, i + 1, lines[i]));
        }

        return result;
    }

    private static void EnsureLines(IReadOnlyList<PurchaseOrderLineDraft> lines)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderRequiresLines,
                "A purchase order must contain at least one line.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderRequiresLines,
                $"A purchase order may contain at most {MaxLineCount} lines.");
        }

        EnsureNoDuplicateProducts(lines);
    }

    private static void EnsureLines(IReadOnlyList<PurchaseOrderLineSnapshotInput> lines)
    {
        if (lines is null || lines.Count == 0)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderRequiresLines,
                "A purchase order must contain at least one line.");
        }

        if (lines.Count > MaxLineCount)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderRequiresLines,
                $"A purchase order may contain at most {MaxLineCount} lines.");
        }

        var productIds = lines.Select(l => l.ProductId.Value).ToList();
        if (productIds.Count != productIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderDuplicateProduct,
                "Duplicate products are not allowed on a purchase order.");
        }
    }

    private static void EnsureNoDuplicateProducts(IReadOnlyList<PurchaseOrderLineDraft> lines)
    {
        var productIds = lines.Select(l => l.ProductId.Value).ToList();
        if (productIds.Count != productIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderDuplicateProduct,
                "Duplicate products are not allowed on a purchase order.");
        }
    }

    private static void ValidateExpectedDelivery(DateOnly orderDate, DateOnly? expectedDeliveryDate)
    {
        if (expectedDeliveryDate is not null && expectedDeliveryDate.Value < orderDate)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseExpectedDeliveryDate,
                "Expected delivery date cannot be before order date.");
        }
    }

    private static string? NormalizeSupplierReference(string? value) =>
        NormalizeOptionalText(value, SupplierReferenceMaxLength, DomainErrorCodes.InvalidPurchaseSupplierReference, "Supplier reference");

    private static string? NormalizeNotes(string? value) =>
        NormalizeOptionalText(value, NotesMaxLength, DomainErrorCodes.InvalidPurchaseOrderNotes, "Notes");

    private static string? NormalizeOptionalText(string? value, int maxLength, string errorCode, string label)
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
}
