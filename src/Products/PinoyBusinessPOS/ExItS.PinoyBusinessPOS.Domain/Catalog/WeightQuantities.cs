using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Input unit for weight entry before normalization to the canonical ByWeight base (kilogram).
/// </summary>
public enum WeightInputUnit
{
    Kilogram = 0,
    Gram = 1
}

/// <summary>
/// Small weight-normalization boundary for ByWeight products.
/// Canonical persisted/sale/inventory quantity is always kilograms.
/// Do not scatter <c>/ 1000m</c> across UI or application services.
/// </summary>
public static class WeightQuantities
{
    /// <summary>Matches sale/inventory measured quantity scale (<c>numeric(18,3)</c>).</summary>
    public const int CanonicalDecimals = 3;

    public const decimal MaxKilograms = 999_999.999m;

    /// <summary>
    /// Converts a positive weight input into canonical kilograms with at most
    /// <see cref="CanonicalDecimals"/> decimal places. Over-precision is rejected (not rounded).
    /// Grams divide by 1000 exactly in <see cref="decimal"/>.
    /// </summary>
    public static decimal NormalizeToKilograms(decimal value, WeightInputUnit inputUnit)
    {
        if (value <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWeightQuantity,
                "Weight quantity must be greater than zero.");
        }

        if (!Enum.IsDefined(inputUnit))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWeightInputUnit,
                $"Unsupported weight input unit '{inputUnit}'.");
        }

        var kilograms = inputUnit switch
        {
            WeightInputUnit.Kilogram => value,
            WeightInputUnit.Gram => value / 1000m,
            _ => throw new DomainException(
                DomainErrorCodes.InvalidWeightInputUnit,
                $"Unsupported weight input unit '{inputUnit}'.")
        };

        if (kilograms > MaxKilograms)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWeightQuantity,
                $"Weight quantity must be at most {MaxKilograms} kg.");
        }

        if (!HasAtMostDecimals(kilograms, CanonicalDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWeightQuantity,
                $"Kilogram quantities may have at most {CanonicalDecimals} decimal places.");
        }

        return kilograms;
    }

    public static bool TryParseInputUnit(string? text, out WeightInputUnit unit)
    {
        unit = WeightInputUnit.Kilogram;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed.Equals("kg", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("kilogram", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("kilograms", StringComparison.OrdinalIgnoreCase))
        {
            unit = WeightInputUnit.Kilogram;
            return true;
        }

        if (trimmed.Equals("g", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("gram", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("grams", StringComparison.OrdinalIgnoreCase))
        {
            unit = WeightInputUnit.Gram;
            return true;
        }

        return Enum.TryParse(trimmed, ignoreCase: true, out unit) && Enum.IsDefined(unit);
    }

    private static bool HasAtMostDecimals(decimal value, int decimals) =>
        decimal.Round(value, decimals, MidpointRounding.ToZero) == value;
}
