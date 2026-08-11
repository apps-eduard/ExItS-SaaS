using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Snapshot input for one checkout line. Online checkout resolves price/mode/name from the live
/// catalog. Offline cash sync supplies immutable snapshots that the application validates without
/// live-catalog re-pricing. Quantity for ByWeight products is already canonical kilograms.
/// </summary>
public sealed record SaleLineDraft(
    CatalogProductId ProductId,
    string NameSnapshot,
    string? SkuSnapshot,
    string? BarcodeSnapshot,
    UnitOfMeasure UnitOfMeasureSnapshot,
    decimal UnitPrice,
    decimal Quantity,
    SellingMode SellingModeSnapshot = SellingMode.PerItem);

/// <summary>
/// One immutable line of a recorded sale. Product name, SKU, barcode, unit of measure, selling mode,
/// and unit price are snapshotted at checkout so later catalog edits never rewrite history. No stock
/// movement, tax, discount, or line-level void exists in this scope.
/// </summary>
public sealed class SaleLine
{
    public const int NameSnapshotMaxLength = 200;
    public const int SkuSnapshotMaxLength = 64;
    public const int BarcodeSnapshotMaxLength = 14;
    public const decimal MaxUnitPrice = 9_999_999_999.99m;
    public const decimal MaxQuantity = 999_999.999m;

    public SaleLineId Id { get; }
    public SaleId SaleId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }

    /// <summary>1-based position of the line inside its sale. Gives reads a stable order.</summary>
    public int LineNumber { get; }

    public string NameSnapshot { get; }
    public string? SkuSnapshot { get; }
    public string? BarcodeSnapshot { get; }
    public UnitOfMeasure UnitOfMeasureSnapshot { get; }
    public SellingMode SellingModeSnapshot { get; }
    public decimal UnitPrice { get; }
    public decimal Quantity { get; }
    public decimal LineTotal { get; }

    private SaleLine(
        SaleLineId id,
        SaleId saleId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        string nameSnapshot,
        string? skuSnapshot,
        string? barcodeSnapshot,
        UnitOfMeasure unitOfMeasureSnapshot,
        SellingMode sellingModeSnapshot,
        decimal unitPrice,
        decimal quantity,
        decimal lineTotal)
    {
        Id = id;
        SaleId = saleId;
        OrganizationId = organizationId;
        ProductId = productId;
        LineNumber = lineNumber;
        NameSnapshot = nameSnapshot;
        SkuSnapshot = skuSnapshot;
        BarcodeSnapshot = barcodeSnapshot;
        UnitOfMeasureSnapshot = unitOfMeasureSnapshot;
        SellingModeSnapshot = sellingModeSnapshot;
        UnitPrice = unitPrice;
        Quantity = quantity;
        LineTotal = lineTotal;
    }

    internal static SaleLine Create(
        SaleId saleId,
        PosOrganizationId organizationId,
        int lineNumber,
        SaleLineDraft draft,
        SaleLineId? id = null)
    {
        if (draft.SellingModeSnapshot == SellingMode.ByWeight)
        {
            SellingModes.EnsureCompatible(draft.SellingModeSnapshot, draft.UnitOfMeasureSnapshot);
        }

        var quantity = NormalizeQuantity(
            draft.Quantity,
            draft.UnitOfMeasureSnapshot,
            draft.SellingModeSnapshot);
        var unitPrice = NormalizeUnitPrice(draft.UnitPrice);

        return new SaleLine(
            id ?? SaleLineId.New(),
            saleId,
            organizationId,
            draft.ProductId,
            lineNumber,
            NormalizeNameSnapshot(draft.NameSnapshot),
            NormalizeOptionalSnapshot(draft.SkuSnapshot, SkuSnapshotMaxLength),
            NormalizeOptionalSnapshot(draft.BarcodeSnapshot, BarcodeSnapshotMaxLength),
            draft.UnitOfMeasureSnapshot,
            draft.SellingModeSnapshot,
            unitPrice,
            quantity,
            SaleMoney.RoundMoney(unitPrice * quantity));
    }

    public static SaleLine Rehydrate(
        SaleLineId id,
        SaleId saleId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        string nameSnapshot,
        string? skuSnapshot,
        string? barcodeSnapshot,
        UnitOfMeasure unitOfMeasureSnapshot,
        decimal unitPrice,
        decimal quantity,
        decimal lineTotal,
        SellingMode sellingModeSnapshot = SellingMode.PerItem) =>
        new(
            id,
            saleId,
            organizationId,
            productId,
            lineNumber,
            nameSnapshot,
            skuSnapshot,
            barcodeSnapshot,
            unitOfMeasureSnapshot,
            sellingModeSnapshot,
            unitPrice,
            quantity,
            lineTotal);

    /// <summary>
    /// Validates a sold quantity. SellingMode is authoritative for ByWeight (canonical kg, ≤3 dp).
    /// PerItem keeps UOM rules: countable whole units; measured UOMs admit ≤3 dp (historical).
    /// </summary>
    public static decimal NormalizeQuantity(
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleLineQuantity,
                "Quantity must be greater than zero.");
        }

        if (quantity > MaxQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleLineQuantity,
                $"Quantity must be at most {MaxQuantity}.");
        }

        var maxDecimals = SaleMoney.MaxQuantityDecimals(unitOfMeasure, sellingMode);
        if (!SaleMoney.HasAtMostDecimals(quantity, maxDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleLineQuantity,
                maxDecimals == 0
                    ? $"{unitOfMeasure} is sold in whole units, so the quantity cannot have decimal places."
                    : sellingMode == SellingMode.ByWeight
                        ? $"ByWeight quantities are kilograms and may have at most {maxDecimals} decimal places."
                        : $"{unitOfMeasure} quantities may have at most {maxDecimals} decimal places.");
        }

        return quantity;
    }

    public static decimal NormalizeUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleLineUnitPrice,
                "Unit price cannot be negative.");
        }

        if (unitPrice > MaxUnitPrice)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleLineUnitPrice,
                "Unit price is too large.");
        }

        if (!SaleMoney.HasAtMostDecimals(unitPrice, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleLineUnitPrice,
                "Unit price must have at most 2 decimal places.");
        }

        return unitPrice;
    }

    private static string NormalizeNameSnapshot(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleLineNameSnapshot,
                "A product name snapshot is required for every sale line.");
        }

        var trimmed = name.Trim();
        return trimmed.Length > NameSnapshotMaxLength
            ? trimmed[..NameSnapshotMaxLength]
            : trimmed;
    }

    private static string? NormalizeOptionalSnapshot(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
