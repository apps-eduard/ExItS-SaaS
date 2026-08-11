using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// Selling-mode / unit invariants for Platform global products.
/// ByWeight products are priced and inventoried in kilograms (canonical).
/// </summary>
public static class ProductSellingModes
{
    public static readonly IReadOnlyList<string> Codes =
    [
        nameof(ProductSellingMode.PerItem),
        nameof(ProductSellingMode.ByWeight)
    ];

    public static string ToCode(ProductSellingMode mode) => mode.ToString();

    public static bool TryParse(string? text, out ProductSellingMode mode)
    {
        mode = ProductSellingMode.PerItem;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return Enum.TryParse(text.Trim(), ignoreCase: true, out mode) && Enum.IsDefined(mode);
    }

    public static ProductSellingMode Parse(string? text, ProductSellingMode defaultWhenBlank = ProductSellingMode.PerItem)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return defaultWhenBlank;
        }

        if (!TryParse(text, out var mode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductSellingMode,
                $"SellingMode must be one of: {string.Join(", ", Codes)}.");
        }

        return mode;
    }

    public static void EnsureCompatible(ProductSellingMode mode, ProductUnit unit)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductSellingMode,
                $"Unrecognized selling mode '{mode}'.");
        }

        if (mode == ProductSellingMode.ByWeight && unit != ProductUnit.Kilogram)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductSellingModeUnit,
                "ByWeight products must use Unit = Kilogram (canonical weight base).");
        }
    }
}
