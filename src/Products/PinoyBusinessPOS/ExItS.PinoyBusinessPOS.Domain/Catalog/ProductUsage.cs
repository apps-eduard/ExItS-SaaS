using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Composition flags describing how a catalog product participates in buy / sell / kitchen /
/// production flows. Flags are authoritative; <see cref="PresetCode"/> is an optional UX hint.
/// </summary>
public sealed class ProductUsageCapabilities
{
    public const string BuyAndSellCode = "BuyAndSell";
    public const string BulkCode = "Bulk";
    public const string IngredientCode = "Ingredient";
    public const string MadeProductCode = "MadeProduct";
    public const string IngredientAndSellableCode = "IngredientAndSellable";

    public bool CanBePurchased { get; }
    public bool CanBeSold { get; }
    public bool CanBeUsedAsIngredient { get; }
    public bool IsProduced { get; }

    /// <summary>Stable preset string for UI defaults (e.g. BuyAndSell, Bulk). Not authoritative.</summary>
    public string? PresetCode { get; }

    private ProductUsageCapabilities(
        bool canBePurchased,
        bool canBeSold,
        bool canBeUsedAsIngredient,
        bool isProduced,
        string? presetCode)
    {
        CanBePurchased = canBePurchased;
        CanBeSold = canBeSold;
        CanBeUsedAsIngredient = canBeUsedAsIngredient;
        IsProduced = isProduced;
        PresetCode = presetCode;
    }

    public static ProductUsageCapabilities BuyAndSell { get; } =
        new(true, true, false, false, BuyAndSellCode);

    /// <summary>Buy in bulk packages, sell smaller base quantities — same usage flags as BuyAndSell.</summary>
    public static ProductUsageCapabilities BuyInBulkSellSmaller { get; } =
        new(true, true, false, false, BulkCode);

    public static ProductUsageCapabilities Ingredient { get; } =
        new(true, false, true, false, IngredientCode);

    public static ProductUsageCapabilities MadeProduct { get; } =
        new(false, true, false, true, MadeProductCode);

    public static ProductUsageCapabilities IngredientAndSellable { get; } =
        new(true, true, true, false, IngredientAndSellableCode);

    public static ProductUsageCapabilities Create(
        bool canBePurchased,
        bool canBeSold,
        bool canBeUsedAsIngredient,
        bool isProduced,
        string? presetCode = null)
    {
        var usage = new ProductUsageCapabilities(
            canBePurchased,
            canBeSold,
            canBeUsedAsIngredient,
            isProduced,
            NormalizePresetCode(presetCode));
        usage.EnsureValid();
        return usage;
    }

    public static ProductUsageCapabilities FromPreset(string presetCode) =>
        NormalizePresetCode(presetCode) switch
        {
            BuyAndSellCode => BuyAndSell,
            BulkCode => BuyInBulkSellSmaller,
            IngredientCode => Ingredient,
            MadeProductCode => MadeProduct,
            IngredientAndSellableCode => IngredientAndSellable,
            _ => throw new DomainException(
                DomainErrorCodes.InvalidProductUsage,
                $"Unknown usage preset '{presetCode}'.")
        };

    public void EnsureValid()
    {
        if (!CanBeSold && !CanBeUsedAsIngredient && !IsProduced)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUsage,
                "A product must be sellable, usable as an ingredient, and/or produced.");
        }
    }

    public static string? NormalizePresetCode(string? presetCode)
    {
        if (string.IsNullOrWhiteSpace(presetCode))
        {
            return null;
        }

        var trimmed = presetCode.Trim();
        return trimmed.Length > 64 ? trimmed[..64] : trimmed;
    }
}
