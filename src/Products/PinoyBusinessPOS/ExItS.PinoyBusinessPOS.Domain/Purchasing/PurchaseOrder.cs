using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
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
    /// <summary>Connected-PO settlement term. Cash default. Not proof of payment.</summary>
    public ConnectedPoPaymentTerm PaymentTerm { get; private set; }

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
        List<PurchaseOrderLine> lines,
        ConnectedPoPaymentTerm paymentTerm = ConnectedPoPaymentTerm.Cash)
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
        PaymentTerm = paymentTerm;
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
        PurchaseOrderId? id = null,
        ConnectedPoPaymentTerm paymentTerm = ConnectedPoPaymentTerm.Cash,
        Guid? createdBy = null)
    {
        SaleMoney.EnsureUtc(utcNow);
        EnsureLines(lines);
        ValidateExpectedDelivery(orderDate, expectedDeliveryDate);
        if (createdBy is { } actor && actor != Guid.Empty)
        {
            SaleMoney.EnsureActor(actor);
        }
        else
        {
            createdBy = null;
        }

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
            orderedBy: createdBy,
            utcNow,
            utcNow,
            poLines,
            paymentTerm);
    }

    public void UpdateDraft(
        SupplierId supplierId,
        DateOnly orderDate,
        IReadOnlyList<PurchaseOrderLineDraft> lines,
        DateTimeOffset utcNow,
        DateOnly? expectedDeliveryDate = null,
        string? supplierReference = null,
        string? notes = null,
        ConnectedPoPaymentTerm? paymentTerm = null)
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
        if (paymentTerm is { } term)
        {
            PaymentTerm = term;
        }

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

        static string LineKey(CatalogProductId? productId, CatalogProductId? supplierProductId) =>
            productId is not null
                ? $"b:{productId.Value:D}"
                : $"s:{supplierProductId!.Value:D}";

        var snapshotByKey = snapshots.ToDictionary(s => LineKey(s.ProductId, s.SupplierProductId));
        foreach (var line in _lines.OrderBy(l => l.LineNumber))
        {
            if (!snapshotByKey.TryGetValue(LineKey(line.ProductId, line.SupplierProductId), out var snapshot))
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

        var lineByProduct = _lines
            .Where(l => l.ProductId is not null)
            .ToDictionary(l => l.ProductId!.Value);
        foreach (var receive in receiveLines)
        {
            if (!lineByProduct.TryGetValue(receive.ProductId.Value, out var line))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Receive line product is not on this purchase order.");
            }

            if (line.NeedsBuyerProductSetup)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Product setup is required before goods can be received.");
            }

            if (receive.ReceiveQty > 0m)
            {
                line.ApplyReceipt(receive.ReceiveQty, receive.SellingMode);
            }

            if (receive.ShortClosedQty > 0m)
            {
                line.ApplyShortClose(receive.ShortClosedQty, receive.SellingMode);
            }

            if (receive.ReceiveQty <= 0m
                && receive.ShortClosedQty <= 0m
                && receive.DamagedQty <= 0m
                && receive.RejectedQty <= 0m)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                    "Each receive line must include good, damaged, rejected, or short-closed quantity.");
            }
        }

        Status = _lines.All(l => l.OutstandingQty <= 0m)
            ? PurchaseOrderStatus.Received
            : PurchaseOrderStatus.PartiallyReceived;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Unwinds quantities posted by a goods receipt and recomputes Ordered / PartiallyReceived / Received.
    /// </summary>
    public void UnwindGoodsReceipt(GoodsReceipt receipt, DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (receipt.PurchaseOrderId != Id || receipt.OrganizationId != OrganizationId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptLine,
                "Goods receipt does not belong to this purchase order.");
        }

        if (Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived or PurchaseOrderStatus.Received))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Only ordered purchase orders can unwind a goods receipt.");
        }

        var lineById = _lines.ToDictionary(l => l.Id.Value);
        foreach (var grnLine in receipt.Lines)
        {
            if (!lineById.TryGetValue(grnLine.PurchaseOrderLineId.Value, out var poLine))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "Goods receipt line does not match a purchase-order line.");
            }

            poLine.ReverseReceipt(grnLine.QuantityReceived, grnLine.ShortClosedQty);
        }

        Status = _lines.All(l => l.OutstandingQty <= 0m)
            ? PurchaseOrderStatus.Received
            : _lines.Any(l => l.ReceivedQty > 0m)
                ? PurchaseOrderStatus.PartiallyReceived
                : PurchaseOrderStatus.Ordered;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// After explicit create/link, bind buyer product onto unlinked connected lines for this supplier product.
    /// </summary>
    public void BindBuyerProductForSupplierProduct(
        CatalogProductId supplierProductId,
        CatalogProductId buyerProductId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        var matched = false;
        foreach (var line in _lines)
        {
            if (line.SupplierProductId != supplierProductId)
            {
                continue;
            }

            line.BindBuyerProduct(buyerProductId);
            matched = true;
        }

        if (!matched)
        {
            return;
        }

        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Caps remaining outstanding to supplier-confirmed quantities without changing OrderedQty.
    /// Reduced/unavailable remainder is short-closed so goods receipt cannot exceed confirmation.
    /// </summary>
    public void AlignOutstandingToConfirmedQuantities(
        IReadOnlyDictionary<Guid, decimal> confirmedQtyByBuyerProductId,
        DateTimeOffset utcNow)
    {
        SaleMoney.EnsureUtc(utcNow);
        if (Status is not (PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderStatusTransition,
                "Only ordered purchase orders can align to confirmed quantities.");
        }

        foreach (var line in _lines)
        {
            if (line.ProductId is null
                || !confirmedQtyByBuyerProductId.TryGetValue(line.ProductId.Value, out var confirmed))
            {
                continue;
            }

            if (confirmed < 0m || confirmed > line.OrderedQty)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                    "Confirmed quantity must be between zero and the original ordered quantity.");
            }

            var allowedRemaining = Math.Max(0m, confirmed - line.ReceivedQty);
            var excess = line.OutstandingQty - allowedRemaining;
            if (excess > 0m)
            {
                line.ApplyShortClose(excess);
            }
        }

        Status = _lines.All(l => l.OutstandingQty <= 0m)
            ? PurchaseOrderStatus.Received
            : _lines.Any(l => l.ReceivedQty > 0m)
                ? PurchaseOrderStatus.PartiallyReceived
                : PurchaseOrderStatus.Ordered;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>True when any line has buyer-closed shortages (Received With Issues signal).</summary>
    public bool HasReceivingIssues => _lines.Any(l => l.HasReceivingIssues);

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
        IReadOnlyList<PurchaseOrderLine> lines,
        ConnectedPoPaymentTerm paymentTerm = ConnectedPoPaymentTerm.Cash) =>
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
            lines.ToList(),
            paymentTerm);

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

        var productIds = lines
            .Where(l => l.ProductId is not null)
            .Select(l => l.ProductId!.Value)
            .ToList();
        if (productIds.Count != productIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderDuplicateProduct,
                "Duplicate products are not allowed on a purchase order.");
        }

        var supplierProductIds = lines
            .Where(l => l.SupplierProductId is not null)
            .Select(l => l.SupplierProductId!.Value)
            .ToList();
        if (supplierProductIds.Count != supplierProductIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderDuplicateProduct,
                "Duplicate supplier products are not allowed on a purchase order.");
        }
    }

    private static void EnsureNoDuplicateProducts(IReadOnlyList<PurchaseOrderLineDraft> lines)
    {
        var productIds = lines
            .Where(l => l.ProductId is not null)
            .Select(l => l.ProductId!.Value)
            .ToList();
        if (productIds.Count != productIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderDuplicateProduct,
                "Duplicate products are not allowed on a purchase order.");
        }

        var supplierProductIds = lines
            .Where(l => l.SupplierProductId is not null)
            .Select(l => l.SupplierProductId!.Value)
            .ToList();
        if (supplierProductIds.Count != supplierProductIds.Distinct().Count())
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOrderDuplicateProduct,
                "Duplicate supplier products are not allowed on a purchase order.");
        }

        foreach (var line in lines)
        {
            if (line.ProductId is null && line.SupplierProductId is null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseOrderLine,
                    "A purchase-order line requires a buyer product or a supplier product identity.");
            }
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
