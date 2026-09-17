using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Authoritative rule: CanBeUsedAsIngredient requires InventoryAccount.IsTracked.
/// Tracking lives on the inventory account — not a duplicate catalog column.
/// </summary>
public static class IngredientInventoryTracking
{
    public const string RequiresTrackedMessage = "Ingredients must use tracked inventory.";

    public static ApplicationResult Validate(bool canBeUsedAsIngredient, bool isTracked)
    {
        if (canBeUsedAsIngredient && !isTracked)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.IngredientRequiresTrackedInventory,
                RequiresTrackedMessage);
        }

        return ApplicationResult.Success();
    }

    public static string UntrackedProductionMaterialMessage(string productName) =>
        $"'{productName}' cannot be used in production because inventory tracking is disabled.";

    public static string UntrackedProductionOutputMessage(string productName) =>
        $"'{productName}' cannot be produced because inventory tracking is disabled.";

    /// <summary>
    /// Creates or enables a tracked inventory account at opening quantity 0 (no stock movement).
    /// </summary>
    public static async Task<ApplicationResult> EnsureTrackedAsync(
        IInventoryRepository inventory,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        UnitOfMeasure unitOfMeasure,
        SellingMode sellingMode,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var account = await inventory
            .GetByProductIdAsync(organizationId, productId, cancellationToken)
            .ConfigureAwait(false);
        if (account is { IsTracked: true })
        {
            return ApplicationResult.Success();
        }

        try
        {
            if (account is null)
            {
                account = InventoryAccount.CreateUntracked(organizationId, productId, utcNow);
                account.Enable(
                    openingQuantity: 0m,
                    unitOfMeasure,
                    actorId: Guid.Empty,
                    utcNow,
                    hasOpeningStockAlready: false,
                    sellingMode: sellingMode);
                await inventory.AddAccountAsync(account, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                account.Enable(
                    openingQuantity: 0m,
                    unitOfMeasure,
                    actorId: Guid.Empty,
                    utcNow,
                    hasOpeningStockAlready: false,
                    sellingMode: sellingMode);
                await inventory.UpdateAccountAsync(account, cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult.Success();
        }
        catch (DomainException ex)
        {
            return ApplicationResult.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
