using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// Buyer-owned classification: how the business uses this catalog product.
/// Maps onto <see cref="ProductUsageCapabilities"/> flags (authoritative for sell/purchase/kitchen).
/// </summary>
public enum ProductBusinessUsage
{
    /// <summary>For resale (UI) — buy and sell the same product; domain value remains Resale. Eligible for POS sell floor when Active and other rules pass.</summary>
    Resale = 0,

    /// <summary>Ingredient / raw material — purchased and used to produce another product; not sold to customers as a finished resale item.</summary>
    Ingredient = 1,

    /// <summary>Internal use — business consumption; not sold to customers.</summary>
    InternalUse = 2,

    /// <summary>Made or prepared by the business (maps to <see cref="ProductUsageCapabilities.MadeProduct"/>).</summary>
    ProducedItem = 3,
}

/// <summary>Stable API/storage strings for <see cref="ProductBusinessUsage"/>.</summary>
public static class ProductBusinessUsages
{
    public const string Resale = "Resale";
    public const string Ingredient = "Ingredient";
    public const string InternalUse = "InternalUse";
    public const string ProducedItem = "ProducedItem";

    public static string ToCode(ProductBusinessUsage usage) =>
        usage switch
        {
            ProductBusinessUsage.Resale => Resale,
            ProductBusinessUsage.Ingredient => Ingredient,
            ProductBusinessUsage.InternalUse => InternalUse,
            ProductBusinessUsage.ProducedItem => ProducedItem,
            _ => throw new DomainException(
                DomainErrorCodes.InvalidProductUsage,
                $"Unknown business usage '{usage}'.")
        };

    public static bool TryParse(string? value, out ProductBusinessUsage usage)
    {
        usage = ProductBusinessUsage.Resale;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim())
        {
            case Resale:
                usage = ProductBusinessUsage.Resale;
                return true;
            case Ingredient:
                usage = ProductBusinessUsage.Ingredient;
                return true;
            case InternalUse:
                usage = ProductBusinessUsage.InternalUse;
                return true;
            case ProducedItem:
            case ProductUsageCapabilities.MadeProductCode:
                usage = ProductBusinessUsage.ProducedItem;
                return true;
            default:
                return false;
        }
    }

    public static ProductBusinessUsage ParseRequired(string? value)
    {
        if (!TryParse(value, out var usage))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUsage,
                $"Unknown business usage '{value}'. Expected Resale, Ingredient, InternalUse, or ProducedItem.");
        }

        return usage;
    }

    /// <summary>
    /// Derives business usage from authoritative capability flags.
    /// Produced / MadeProduct classifies as <see cref="ProductBusinessUsage.ProducedItem"/> before Resale.
    /// Non-sellable ingredients classify as Ingredient; otherwise InternalUse.
    /// </summary>
    public static ProductBusinessUsage Classify(ProductUsageCapabilities capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        if (capabilities.IsProduced
            || string.Equals(
                capabilities.PresetCode,
                ProductUsageCapabilities.MadeProductCode,
                StringComparison.Ordinal))
        {
            return ProductBusinessUsage.ProducedItem;
        }

        if (capabilities.CanBeSold)
        {
            return ProductBusinessUsage.Resale;
        }

        if (capabilities.CanBeUsedAsIngredient
            || string.Equals(
                capabilities.PresetCode,
                ProductUsageCapabilities.IngredientCode,
                StringComparison.Ordinal))
        {
            return ProductBusinessUsage.Ingredient;
        }

        return ProductBusinessUsage.InternalUse;
    }

    public static ProductUsageCapabilities ToCapabilities(ProductBusinessUsage usage) =>
        usage switch
        {
            ProductBusinessUsage.Resale => ProductUsageCapabilities.BuyAndSell,
            ProductBusinessUsage.Ingredient => ProductUsageCapabilities.Ingredient,
            ProductBusinessUsage.InternalUse => ProductUsageCapabilities.InternalUse,
            ProductBusinessUsage.ProducedItem => ProductUsageCapabilities.MadeProduct,
            _ => throw new DomainException(
                DomainErrorCodes.InvalidProductUsage,
                $"Unknown business usage '{usage}'.")
        };
}

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
    public const string InternalUseCode = "InternalUse";

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

    /// <summary>Purchased for business consumption; not sold and not used as a production ingredient.</summary>
    public static ProductUsageCapabilities InternalUse { get; } =
        new(true, false, false, false, InternalUseCode);

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
            InternalUseCode => InternalUse,
            // Business-usage aliases accepted for API convenience.
            ProductBusinessUsages.Resale => BuyAndSell,
            ProductBusinessUsages.ProducedItem => MadeProduct,
            _ => throw new DomainException(
                DomainErrorCodes.InvalidProductUsage,
                $"Unknown usage preset '{presetCode}'.")
        };

    public void EnsureValid()
    {
        if (CanBePurchased || CanBeSold || CanBeUsedAsIngredient || IsProduced)
        {
            return;
        }

        throw new DomainException(
            DomainErrorCodes.InvalidProductUsage,
            "A product must be purchasable, sellable, usable as an ingredient, and/or produced.");
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
