using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// One line on a purchase order. Product name and UOM are snapshotted on submit (Ordered).
/// OrderedQty / ReceivedQty remain in purchase-unit terms. Multiplier converts to base inventory qty.
/// Unit purchase cost is operational only — not used for COGS or inventory valuation.
/// </summary>
public sealed class PurchaseOrderLine
{
    public const int NameSnapshotMaxLength = 200;
    public const int PurchaseUnitNameSnapshotMaxLength = 64;
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
    /// <summary>Buyer-closed shortage that will not be received later.</summary>
    public decimal ClosedShortQty { get; private set; }
    public string? LineNotes { get; private set; }
    public ProductUnitId? PurchaseUnitId { get; private set; }
    public string? PurchaseUnitNameSnapshot { get; private set; }
    public decimal MultiplierToBaseSnapshot { get; private set; }

    public decimal OutstandingQty => OrderedQty - ReceivedQty - ClosedShortQty;
    public bool HasReceivingIssues => ClosedShortQty > 0m;

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
        string? lineNotes,
        ProductUnitId? purchaseUnitId,
        string? purchaseUnitNameSnapshot,
        decimal multiplierToBaseSnapshot,
        decimal closedShortQty = 0m)
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
        ClosedShortQty = closedShortQty;
        LineNotes = lineNotes;
        PurchaseUnitId = purchaseUnitId;
        PurchaseUnitNameSnapshot = purchaseUnitNameSnapshot;
        MultiplierToBaseSnapshot = multiplierToBaseSnapshot;
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
        var multiplier = CatalogProductUnit.NormalizeMultiplier(draft.MultiplierToBaseSnapshot);
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
            NormalizeLineNotes(draft.LineNotes),
            draft.PurchaseUnitId,
            NormalizePurchaseUnitName(draft.PurchaseUnitNameSnapshot),
            multiplier);
    }

    internal static PurchaseOrderLine CreateOrdered(
        PurchaseOrderId purchaseOrderId,
        PosOrganizationId organizationId,
        int lineNumber,
        PurchaseOrderLineSnapshotInput snapshot,
        PurchaseOrderLineId? id = null)
    {
        var name = NormalizeNameSnapshot(snapshot.NameSnapshot);
        if (snapshot.SellingMode == SellingMode.ByWeight)
        {
            SellingModes.EnsureCompatible(snapshot.SellingMode, snapshot.UomSnapshot);
        }

        var qty = NormalizeQuantity(snapshot.OrderedQty, snapshot.UomSnapshot, snapshot.SellingMode);
        var cost = NormalizeUnitPurchaseCost(snapshot.UnitPurchaseCost);
        var multiplier = CatalogProductUnit.NormalizeMultiplier(snapshot.MultiplierToBaseSnapshot);
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
            NormalizeLineNotes(snapshot.LineNotes),
            snapshot.PurchaseUnitId,
            NormalizePurchaseUnitName(snapshot.PurchaseUnitNameSnapshot),
            multiplier);
    }

    internal void UpdateDraft(PurchaseOrderLineDraft draft)
    {
        EnsurePositiveQty(draft.OrderedQty, uom: null, isDraft: true);
        var cost = NormalizeUnitPurchaseCost(draft.UnitPurchaseCost);
        OrderedQty = draft.OrderedQty;
        UnitPurchaseCost = cost;
        LineTotal = SaleMoney.RoundMoney(cost * draft.OrderedQty);
        LineNotes = NormalizeLineNotes(draft.LineNotes);
        PurchaseUnitId = draft.PurchaseUnitId;
        PurchaseUnitNameSnapshot = NormalizePurchaseUnitName(draft.PurchaseUnitNameSnapshot);
        MultiplierToBaseSnapshot = CatalogProductUnit.NormalizeMultiplier(draft.MultiplierToBaseSnapshot);
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
        if (snapshot.SellingMode == SellingMode.ByWeight)
        {
            SellingModes.EnsureCompatible(snapshot.SellingMode, snapshot.UomSnapshot);
        }

        var qty = NormalizeQuantity(snapshot.OrderedQty, snapshot.UomSnapshot, snapshot.SellingMode);
        var cost = NormalizeUnitPurchaseCost(snapshot.UnitPurchaseCost);
        OrderedQty = qty;
        UnitPurchaseCost = cost;
        LineTotal = SaleMoney.RoundMoney(cost * qty);
        LineNotes = NormalizeLineNotes(snapshot.LineNotes);
        PurchaseUnitId = snapshot.PurchaseUnitId;
        PurchaseUnitNameSnapshot = NormalizePurchaseUnitName(snapshot.PurchaseUnitNameSnapshot);
        MultiplierToBaseSnapshot = CatalogProductUnit.NormalizeMultiplier(snapshot.MultiplierToBaseSnapshot);
    }

    internal void ApplyReceipt(decimal receiveQty, SellingMode sellingMode = SellingMode.PerItem)
    {
        if (UomSnapshot is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderLine,
                "Cannot receive against a line that has not been ordered.");
        }

        var normalized = NormalizeQuantity(receiveQty, UomSnapshot.Value, sellingMode);
        if (normalized <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseReceiveQuantity,
                "Receive quantity must be greater than zero.");
        }

        if (ReceivedQty + ClosedShortQty + normalized > OrderedQty)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOverReceipt,
                $"Receive quantity exceeds outstanding quantity for '{NameSnapshot}'.");
        }

        ReceivedQty += normalized;
    }

    internal void ApplyShortClose(decimal shortQty, SellingMode sellingMode = SellingMode.PerItem)
    {
        if (UomSnapshot is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderLine,
                "Cannot close shortage against a line that has not been ordered.");
        }

        if (shortQty <= 0m)
        {
            return;
        }

        var normalized = NormalizeQuantity(shortQty, UomSnapshot.Value, sellingMode);
        if (ReceivedQty + ClosedShortQty + normalized > OrderedQty)
        {
            throw new DomainException(
                DomainErrorCodes.PurchaseOverReceipt,
                $"Short-closed quantity exceeds outstanding quantity for '{NameSnapshot}'.");
        }

        ClosedShortQty += normalized;
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
        string? lineNotes,
        ProductUnitId? purchaseUnitId = null,
        string? purchaseUnitNameSnapshot = null,
        decimal multiplierToBaseSnapshot = 1m,
        decimal closedShortQty = 0m) =>
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
            lineNotes,
            purchaseUnitId,
            purchaseUnitNameSnapshot,
            multiplierToBaseSnapshot,
            closedShortQty);

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

    internal static decimal NormalizeQuantity(
        decimal quantity,
        UnitOfMeasure uom,
        SellingMode sellingMode = SellingMode.PerItem) =>
        SaleLine.NormalizeQuantity(quantity, uom, sellingMode);

    private static void EnsurePositiveQty(
        decimal quantity,
        UnitOfMeasure? uom,
        bool isDraft,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderQuantity,
                "Ordered quantity must be greater than zero.");
        }

        if (!isDraft && uom is not null)
        {
            _ = NormalizeQuantity(quantity, uom.Value, sellingMode);
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

    private static string? NormalizePurchaseUnitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();
        return trimmed.Length > PurchaseUnitNameSnapshotMaxLength
            ? trimmed[..PurchaseUnitNameSnapshotMaxLength]
            : trimmed;
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
