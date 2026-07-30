using ExItS.PinoyBusinessPOS.Domain.Catalog;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>
/// Controlled catalog option sets surfaced to clients and UI. Stable codes only — localized labels
/// belong to the presentation resource files.
/// </summary>
public static class PosCatalogOptions
{
    public static IReadOnlyList<string> UnitOfMeasureCodes { get; } = UnitOfMeasures.Codes;

    public static IReadOnlyList<string> ProductStatuses { get; } =
    [
        nameof(CatalogProductStatus.Active),
        nameof(CatalogProductStatus.Inactive)
    ];

    public static IReadOnlyList<string> CategoryStatuses { get; } =
    [
        nameof(ProductCategoryStatus.Active),
        nameof(ProductCategoryStatus.Inactive)
    ];

    public const string ActiveStatus = nameof(CatalogProductStatus.Active);
    public const string InactiveStatus = nameof(CatalogProductStatus.Inactive);
    public const string DefaultUnitOfMeasureCode = nameof(UnitOfMeasure.Piece);
}
