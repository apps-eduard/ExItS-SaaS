using ExItS.PinoyBusinessPOS.Application.Catalog;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Catalog;

/// <summary>Editable buying/selling package row for the catalog product form.</summary>
public sealed class ProductUnitDraft
{
    public Guid? UnitId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ShortLabel { get; set; } = string.Empty;
    public decimal? MultiplierToBase { get; set; } = 1m;
    /// <summary>Plain-language alias for form binding (avoids jargon in markup).</summary>
    public decimal? ContainsAmount
    {
        get => MultiplierToBase;
        set => MultiplierToBase = value;
    }
    public decimal? SellingPrice { get; set; }
    public bool AllowsCustomQuantity { get; set; }

    public static ProductUnitDraft DefaultPurchase(string? unitOfMeasure) =>
        new()
        {
            DisplayName = NormalizeLabel(unitOfMeasure),
            MultiplierToBase = 1m
        };

    public static ProductUnitDraft DefaultSell(string? unitOfMeasure, decimal? sellingPrice) =>
        new()
        {
            DisplayName = NormalizeLabel(unitOfMeasure),
            MultiplierToBase = 1m,
            SellingPrice = sellingPrice
        };

    public static ProductUnitDraft FromDto(PosCatalogProductUnitDto unit) =>
        new()
        {
            UnitId = unit.UnitId == Guid.Empty ? null : unit.UnitId,
            DisplayName = unit.DisplayName,
            ShortLabel = unit.ShortLabel,
            MultiplierToBase = unit.MultiplierToBase,
            SellingPrice = unit.SellingPrice,
            AllowsCustomQuantity = unit.AllowsCustomQuantity
        };

    public static void EnsureDefaults(
        IList<ProductUnitDraft> purchaseUnits,
        IList<ProductUnitDraft> sellUnits,
        string? unitOfMeasure,
        decimal? sellingPrice)
    {
        if (purchaseUnits.Count == 0)
        {
            purchaseUnits.Add(DefaultPurchase(unitOfMeasure));
        }

        if (sellUnits.Count == 0)
        {
            sellUnits.Add(DefaultSell(unitOfMeasure, sellingPrice));
        }
    }

    public static bool SuggestsAdvancedPackages(IEnumerable<PosCatalogProductUnitDto>? units) =>
        units is not null
        && units.Any(u => u.IsActive && u.MultiplierToBase != 1m);

    public static IReadOnlyList<PosCatalogProductUnitInput> ToInputs(
        IList<ProductUnitDraft> purchaseUnits,
        IList<ProductUnitDraft> sellUnits)
    {
        var result = new List<PosCatalogProductUnitInput>();
        var sort = 0;
        foreach (var draft in purchaseUnits)
        {
            result.Add(ToInput(draft, "Purchase", sort++, sellingPrice: null, allowsCustom: false));
        }

        sort = 0;
        foreach (var draft in sellUnits)
        {
            result.Add(ToInput(
                draft,
                "Sell",
                sort++,
                draft.SellingPrice,
                draft.AllowsCustomQuantity));
        }

        return result;
    }

    private static PosCatalogProductUnitInput ToInput(
        ProductUnitDraft draft,
        string kind,
        int sortOrder,
        decimal? sellingPrice,
        bool allowsCustom)
    {
        var name = string.IsNullOrWhiteSpace(draft.DisplayName)
            ? "Unit"
            : draft.DisplayName.Trim();
        var shortLabel = ResolveShortLabel(draft.ShortLabel, name);
        var multiplier = draft.MultiplierToBase is > 0m ? draft.MultiplierToBase.Value : 1m;
        return new PosCatalogProductUnitInput(
            kind,
            name,
            shortLabel,
            multiplier,
            sellingPrice,
            allowsCustom,
            sortOrder,
            draft.UnitId);
    }

    private static string ResolveShortLabel(string? shortLabel, string displayName)
    {
        var value = string.IsNullOrWhiteSpace(shortLabel) ? displayName : shortLabel.Trim();
        return value.Length <= 16 ? value : value[..16];
    }

    private static string NormalizeLabel(string? unitOfMeasure) =>
        string.IsNullOrWhiteSpace(unitOfMeasure) ? "Piece" : unitOfMeasure.Trim();
}
