using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>
/// Cashier weight-entry helpers for ByWeight products. Canonical quantity is always kilograms via
/// <see cref="WeightQuantities"/> — do not reimplement gram conversion in UI code.
/// </summary>
public static class WeightEntry
{
    public const string UnitKilogram = "kg";
    public const string UnitGram = "g";

    public static bool IsByWeight(string? sellingMode) =>
        string.Equals(sellingMode, nameof(SellingMode.ByWeight), StringComparison.OrdinalIgnoreCase);

    public static bool TryNormalize(
        decimal? rawValue,
        string? unitCode,
        out decimal kilograms,
        out string? errorCode)
    {
        kilograms = 0m;
        errorCode = null;

        if (rawValue is null || rawValue <= 0m)
        {
            errorCode = "zero";
            return false;
        }

        if (!WeightQuantities.TryParseInputUnit(unitCode, out var unit))
        {
            errorCode = "unit";
            return false;
        }

        try
        {
            kilograms = WeightQuantities.NormalizeToKilograms(rawValue.Value, unit);
            return true;
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.InvalidWeightQuantity)
        {
            errorCode = ex.Message.Contains("decimal", StringComparison.OrdinalIgnoreCase)
                ? "precision"
                : "invalid";
            return false;
        }
        catch (DomainException)
        {
            errorCode = "invalid";
            return false;
        }
    }

    /// <summary>Formats canonical kg for cart display (trim trailing zeros, keep up to 3 dp).</summary>
    public static string FormatKilograms(decimal kilograms) =>
        kilograms.ToString("0.###");
}
