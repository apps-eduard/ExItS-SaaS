using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Snapshot input for one checkout line. Online checkout resolves price/mode/name from the live
/// catalog. Offline cash sync supplies immutable snapshots that the application validates without
/// live-catalog re-pricing. Quantity for ByWeight products is already canonical kilograms.
/// When selling-unit conversion is present, <see cref="Quantity"/> is the base inventory quantity
/// (or may be ignored when <see cref="EnteredQuantity"/> + multiplier compute it).
/// </summary>
public sealed record SaleLineDraft(
    CatalogProductId ProductId,
    string NameSnapshot,
    string? SkuSnapshot,
    string? BarcodeSnapshot,
    UnitOfMeasure UnitOfMeasureSnapshot,
    decimal UnitPrice,
    decimal Quantity,
    SellingMode SellingModeSnapshot = SellingMode.PerItem,
    ProductUnitId? SellingUnitId = null,
    string? SellingUnitNameSnapshot = null,
    decimal? EnteredQuantity = null,
    decimal? MultiplierToBaseSnapshot = null);

/// <summary>
/// One immutable line of a recorded sale. Product name, SKU, barcode, unit of measure, selling mode,
/// and unit price are snapshotted at checkout so later catalog edits never rewrite history. No stock
/// movement, tax, discount, or line-level void exists in this scope.
/// <see cref="Quantity"/> is always base inventory quantity. When conversion snapshots are present,
/// <see cref="UnitPrice"/> is the price per selling unit and LineTotal = RoundMoney(UnitPrice × EnteredQuantity).
/// </summary>
public sealed class SaleLine
{
    public const int NameSnapshotMaxLength = 200;
    public const int SkuSnapshotMaxLength = 64;
    public const int BarcodeSnapshotMaxLength = 14;
    public const int SellingUnitNameSnapshotMaxLength = 64;
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

    /// <summary>Base inventory quantity (authoritative for stock). Always in base UOM terms.</summary>
    public decimal Quantity { get; }

    public decimal LineTotal { get; }

    public ProductUnitId? SellingUnitId { get; }
    public string? SellingUnitNameSnapshot { get; }

    /// <summary>Quantity entered in the selling unit. Null for legacy lines where Quantity is both.</summary>
    public decimal? EnteredQuantity { get; }

    /// <summary>Multiplier from selling unit to base. Null/absent with legacy lines; treat as 1.</summary>
    public decimal? MultiplierToBaseSnapshot { get; }

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
        decimal lineTotal,
        ProductUnitId? sellingUnitId,
        string? sellingUnitNameSnapshot,
        decimal? enteredQuantity,
        decimal? multiplierToBaseSnapshot)
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
        SellingUnitId = sellingUnitId;
        SellingUnitNameSnapshot = sellingUnitNameSnapshot;
        EnteredQuantity = enteredQuantity;
        MultiplierToBaseSnapshot = multiplierToBaseSnapshot;
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

        var unitPrice = NormalizeUnitPrice(draft.UnitPrice);
        var usesConversion = UsesSellingUnitConversion(draft);

        decimal quantity;
        decimal lineTotal;
        decimal? enteredQuantity;
        decimal? multiplierSnapshot;

        if (usesConversion)
        {
            var multiplier = CatalogProductUnit.NormalizeMultiplier(draft.MultiplierToBaseSnapshot ?? 1m);
            var entered = NormalizeEnteredQuantity(
                draft.EnteredQuantity!.Value,
                multiplier,
                draft.UnitOfMeasureSnapshot,
                draft.SellingModeSnapshot);
            quantity = NormalizeQuantity(
                ProductUnitConversion.ToBaseQuantity(entered, multiplier),
                draft.UnitOfMeasureSnapshot,
                draft.SellingModeSnapshot);
            lineTotal = SaleMoney.RoundMoney(unitPrice * entered);
            enteredQuantity = entered;
            multiplierSnapshot = multiplier;
        }
        else
        {
            quantity = NormalizeQuantity(
                draft.Quantity,
                draft.UnitOfMeasureSnapshot,
                draft.SellingModeSnapshot);
            lineTotal = SaleMoney.RoundMoney(unitPrice * quantity);
            enteredQuantity = draft.EnteredQuantity;
            multiplierSnapshot = draft.MultiplierToBaseSnapshot;
        }

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
            lineTotal,
            draft.SellingUnitId,
            NormalizeOptionalSnapshot(draft.SellingUnitNameSnapshot, SellingUnitNameSnapshotMaxLength),
            enteredQuantity,
            multiplierSnapshot);
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
        SellingMode sellingModeSnapshot = SellingMode.PerItem,
        ProductUnitId? sellingUnitId = null,
        string? sellingUnitNameSnapshot = null,
        decimal? enteredQuantity = null,
        decimal? multiplierToBaseSnapshot = null) =>
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
            lineTotal,
            sellingUnitId,
            sellingUnitNameSnapshot,
            enteredQuantity,
            multiplierToBaseSnapshot);

    /// <summary>
    /// Conversion pricing applies when entered qty + multiplier are present and the multiplier is not
    /// 1:1, or a selling-unit pack/custom unit id is snapshotted.
    /// </summary>
    internal static bool UsesSellingUnitConversion(SaleLineDraft draft)
    {
        if (draft.EnteredQuantity is null)
        {
            return false;
        }

        var multiplier = draft.MultiplierToBaseSnapshot ?? 1m;
        return multiplier != 1m || draft.SellingUnitId is not null;
    }

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

    private static decimal NormalizeEnteredQuantity(
        decimal enteredQuantity,
        decimal multiplierToBase,
        UnitOfMeasure baseUnitOfMeasure,
        SellingMode sellingMode)
    {
        if (multiplierToBase == 1m)
        {
            return NormalizeQuantity(enteredQuantity, baseUnitOfMeasure, sellingMode);
        }

        // Entered quantity is in selling-pack terms (e.g. bags), not base UOM.
        if (enteredQuantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleLineQuantity,
                "Entered quantity must be greater than zero.");
        }

        if (enteredQuantity > MaxQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleLineQuantity,
                $"Entered quantity must be at most {MaxQuantity}.");
        }

        if (!SaleMoney.HasAtMostDecimals(enteredQuantity, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleLineQuantity,
                $"Entered quantity may have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        return enteredQuantity;
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
