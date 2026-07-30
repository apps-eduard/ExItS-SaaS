using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// One line on a purchase order. Product name and UOM are snapshotted on submit (Ordered).
/// Unit purchase cost is operational only — not used for COGS or inventory valuation.
/// </summary>
public sealed class PurchaseOrderLine
{
    public const int NameSnapshotMaxLength = 200;
    public const int LineNotesMaxLength = 512;
    public const decimal MaxUnitPurchaseCost = 9_999_999_999.99m;

    public PurchaseOrderLineId Id { get; }
    public PurchaseOrderId PurchaseOrderId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public int LineNumber { get; }
    public string? NameSnapshot { get; private set; }
    public UnitOfMeasure? UomSnapshot { get; private set; }
    public decimal OrderedQty { get; private set; }
    public decimal UnitPurchaseCost { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal ReceivedQty { get; private set; }
    public string? LineNotes { get; private set; }

    public decimal OutstandingQty => OrderedQty - ReceivedQty;

    private PurchaseOrderLine(
        PurchaseOrderLineId id,
        PurchaseOrderId purchaseOrderId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        string? nameSnapshot,
        UnitOfMeasure? uomSnapshot,
        decimal orderedQty,
        decimal unitPurchaseCost,
        decimal lineTotal,
        decimal receivedQty,
        string? lineNotes)
    {
        Id = id;
        PurchaseOrderId = purchaseOrderId;
        OrganizationId = organizationId;
        ProductId = productId;
        LineNumber = lineNumber;
        NameSnapshot = nameSnapshot;
        UomSnapshot = uomSnapshot;
        OrderedQty = orderedQty;
        UnitPurchaseCost = unitPurchaseCost;
        LineTotal = lineTotal;
        ReceivedQty = receivedQty;
        LineNotes = lineNotes;
    }

    internal static PurchaseOrderLine CreateDraft(
        PurchaseOrderId purchaseOrderId,
        PosOrganizationId organizationId,
        int lineNumber,
        PurchaseOrderLineDraft draft,
        PurchaseOrderLineId? id = null)
    {
        EnsurePositiveQty(draft.OrderedQty, uom: null, isDraft: true);
        var cost = NormalizeUnitPurchaseCost(draft.UnitPurchaseCost);
        return new PurchaseOrderLine(
            id ?? PurchaseOrderLineId.New(),
            purchaseOrderId,
            organizationId,
            draft.ProductId,
            lineNumber,
            nameSnapshot: null,
            uomSnapshot: null,
            draft.OrderedQty,
            cost,
            SaleMoney.RoundMoney(cost * draft.OrderedQty),
            receivedQty: 0m,
            NormalizeLineNotes(draft.LineNotes));
    }

    internal static PurchaseOrderLine CreateOrdered(
        PurchaseOrderId purchaseOrderId,
        PosOrganizationId organizationId,
        int lineNumber,
        PurchaseOrderLineSnapshotInput snapshot,
        PurchaseOrderLineId? id = null)
    {
        var name = NormalizeNameSnapshot(snapshot.NameSnapshot);
        var qty = NormalizeQuantity(snapshot.OrderedQty, snapshot.UomSnapshot);
        var cost = NormalizeUnitPurchaseCost(snapshot.UnitPurchaseCost);
        return new PurchaseOrderLine(
            id ?? PurchaseOrderLineId.New(),
            purchaseOrderId,
            organizationId,
            snapshot.ProductId,
            lineNumber,
            name,
            snapshot.UomSnapshot,
            qty,
            cost,
            SaleMoney.RoundMoney(cost * qty),
            receivedQty: 0m,
            NormalizeLineNotes(snapshot.LineNotes));
    }

    internal void UpdateDraft(PurchaseOrderLineDraft draft)
    {
        EnsurePositiveQty(draft.OrderedQty, uom: null, isDraft: true);
        var cost = NormalizeUnitPurchaseCost(draft.UnitPurchaseCost);
        OrderedQty = draft.OrderedQty;
        UnitPurchaseCost = cost;
        LineTotal = SaleMoney.RoundMoney(cost * draft.OrderedQty);
        LineNotes = NormalizeLineNotes(draft.LineNotes);
    }

    internal void FreezeSnapshot(PurchaseOrderLineSnapshotInput snapshot)
    {
        if (snapshot.ProductId != ProductId)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderLine,
                "Product id mismatch when freezing line snapshot.");
        }

        NameSnapshot = NormalizeNameSnapshot(snapshot.NameSnapshot);
        UomSnapshot = snapshot.UomSnapshot;
        var qty = NormalizeQuantity(snapshot.OrderedQty, snapshot.UomSnapshot);
        var cost = NormalizeUnitPurchaseCost(snapshot.UnitPurchaseCost);
        OrderedQty = qty;
        UnitPurchaseCost = cost;
        LineTotal = SaleMoney.RoundMoney(cost * qty);
        LineNotes = NormalizeLineNotes(snapshot.LineNotes);
    }

    internal void ApplyReceipt(decimal receiveQty)
    {
        if (UomSnapshot is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderLine,
                "Cannot receive against a line that has not been ordered.");
        }

        var normalized = NormalizeQuantity(receiveQty, UomSnapshot.Value);
        if (normalized <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Receive quantity must be greater than zero.");
        }

        if (ReceivedQty + normalized > OrderedQty)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOverReceipt,
                $"Receive quantity exceeds outstanding quantity for '{NameSnapshot}'.");
        }

        ReceivedQty += normalized;
    }

    public static PurchaseOrderLine Rehydrate(
        PurchaseOrderLineId id,
        PurchaseOrderId purchaseOrderId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        string? nameSnapshot,
        UnitOfMeasure? uomSnapshot,
        decimal orderedQty,
        decimal unitPurchaseCost,
        decimal lineTotal,
        decimal receivedQty,
        string? lineNotes) =>
        new(
            id,
            purchaseOrderId,
            organizationId,
            productId,
            lineNumber,
            nameSnapshot,
            uomSnapshot,
            orderedQty,
            unitPurchaseCost,
            lineTotal,
            receivedQty,
            lineNotes);

    internal static decimal NormalizeUnitPurchaseCost(decimal cost)
    {
        if (cost < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseUnitCost,
                "Unit purchase cost cannot be negative.");
        }

        if (cost > MaxUnitPurchaseCost)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseUnitCost,
                "Unit purchase cost is too large.");
        }

        return SaleMoney.RoundMoney(cost);
    }

    internal static decimal NormalizeQuantity(decimal quantity, UnitOfMeasure uom) =>
        SaleLine.NormalizeQuantity(quantity, uom);

    private static void EnsurePositiveQty(decimal quantity, UnitOfMeasure? uom, bool isDraft)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderQuantity,
                "Ordered quantity must be greater than zero.");
        }

        if (!isDraft && uom is not null)
        {
            _ = NormalizeQuantity(quantity, uom.Value);
        }
    }

    private static string NormalizeNameSnapshot(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderLine,
                "Product name snapshot is required on submit.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > NameSnapshotMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderLine,
                $"Product name snapshot must be at most {NameSnapshotMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeLineNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > LineNotesMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderNotes,
                $"Line notes must be at most {LineNotesMaxLength} characters.");
        }

        return trimmed;
    }
}
