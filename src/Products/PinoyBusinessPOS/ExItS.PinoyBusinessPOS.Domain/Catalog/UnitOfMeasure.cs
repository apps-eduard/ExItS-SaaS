using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Controlled retail unit-of-measure set for the Basic Store catalog. Enum member names are the
/// stable persistence codes; localized labels live in the UI resource files only.
/// </summary>
public enum UnitOfMeasure
{
    Piece = 0,
    Pack = 1,
    Box = 2,
    Bottle = 3,
    Can = 4,
    Sachet = 5,
    Kilogram = 6,
    Gram = 7,
    Liter = 8,
    Milliliter = 9,
    Meter = 10
}

public static class UnitOfMeasures
{
    public const int CodeMaxLength = 32;

    /// <summary>Stable persistence codes in canonical display order.</summary>
    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(UnitOfMeasure.Piece),
        nameof(UnitOfMeasure.Pack),
        nameof(UnitOfMeasure.Box),
        nameof(UnitOfMeasure.Bottle),
        nameof(UnitOfMeasure.Can),
        nameof(UnitOfMeasure.Sachet),
        nameof(UnitOfMeasure.Kilogram),
        nameof(UnitOfMeasure.Gram),
        nameof(UnitOfMeasure.Liter),
        nameof(UnitOfMeasure.Milliliter),
        nameof(UnitOfMeasure.Meter)
    ];

    public static string ToCode(UnitOfMeasure unit) => unit.ToString();

    public static bool TryParse(string? code, out UnitOfMeasure unit)
    {
        unit = UnitOfMeasure.Piece;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmed = code.Trim();
        var match = Codes.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        unit = Enum.Parse<UnitOfMeasure>(match, ignoreCase: false);
        return true;
    }

    public static UnitOfMeasure Parse(string? code)
    {
        if (!TryParse(code, out var unit))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUnitOfMeasure,
                $"Unit of measure must be one of: {string.Join(", ", Codes)}.");
        }

        return unit;
    }
}
