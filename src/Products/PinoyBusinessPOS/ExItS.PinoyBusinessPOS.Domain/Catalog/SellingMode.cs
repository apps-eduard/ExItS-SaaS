using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// How a merchant catalog product is sold. Orthogonal to Business Type and to <see cref="UnitOfMeasure"/>.
/// ByWeight products use <see cref="UnitOfMeasure.Kilogram"/> as the canonical inventory/price unit.
/// SellingPrice for ByWeight means price per kilogram.
/// </summary>
public enum SellingMode
{
    PerItem = 0,
    ByWeight = 1
}

public static class SellingModes
{
    public static readonly IReadOnlyList<string> Codes =
    [
        nameof(SellingMode.PerItem),
        nameof(SellingMode.ByWeight)
    ];

    public static string ToCode(SellingMode mode) => mode.ToString();

    public static bool TryParse(string? text, out SellingMode mode)
    {
        mode = SellingMode.PerItem;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return Enum.TryParse(text.Trim(), ignoreCase: true, out mode) && Enum.IsDefined(mode);
    }

    public static SellingMode Parse(string? text, SellingMode defaultWhenBlank = SellingMode.PerItem)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return defaultWhenBlank;
        }

        if (!TryParse(text, out var mode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSellingMode,
                $"SellingMode must be one of: {string.Join(", ", Codes)}.");
        }

        return mode;
    }

    public static void EnsureCompatible(SellingMode mode, UnitOfMeasure unit)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSellingMode,
                $"Unrecognized selling mode '{mode}'.");
        }

        if (mode == SellingMode.ByWeight && unit != UnitOfMeasure.Kilogram)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSellingModeUnit,
                "ByWeight products must use UnitOfMeasure = Kilogram (canonical weight base).");
        }
    }
}
