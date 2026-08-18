using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

/// <summary>
/// Authoritative Personal storefront availability presentation. Uses orderable stock
/// (<see cref="InventoryAccount.AvailableQuantity"/> = OnHand − Reserved).
/// </summary>
public static class CustomerStorefrontAvailability
{
    /// <summary>Customer-facing low-stock threshold. Independent of merchant reorder level.</summary>
    public const decimal LowStockThreshold = 5m;

    public const string Untracked = "Untracked";
    public const string InStock = "InStock";
    public const string LowStock = "LowStock";
    public const string OutOfStock = "OutOfStock";

    public static StorefrontAvailabilitySnapshot FromAccount(InventoryAccount? account)
    {
        if (account is null || !account.IsTracked)
        {
            return new StorefrontAvailabilitySnapshot(
                IsAvailable: true,
                TracksInventory: false,
                AvailableQuantity: null,
                Status: Untracked);
        }

        var qty = account.AvailableQuantity;
        return FromTrackedQuantity(qty);
    }

    public static StorefrontAvailabilitySnapshot FromTrackedQuantity(decimal qty)
    {
        if (qty <= 0m)
        {
            return new StorefrontAvailabilitySnapshot(false, true, qty, OutOfStock);
        }

        if (qty <= LowStockThreshold)
        {
            return new StorefrontAvailabilitySnapshot(true, true, qty, LowStock);
        }

        return new StorefrontAvailabilitySnapshot(true, true, qty, InStock);
    }

    public static bool CanIncrement(CustomerStorefrontProductDto product, decimal currentQuantity)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (!product.IsAvailable || product.UnitPrice <= 0m)
        {
            return false;
        }

        if (!product.TracksInventory || product.AvailableQuantity is null)
        {
            return true;
        }

        return currentQuantity < product.AvailableQuantity.Value;
    }
}

public readonly record struct StorefrontAvailabilitySnapshot(
    bool IsAvailable,
    bool TracksInventory,
    decimal? AvailableQuantity,
    string Status);
