using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>Pure helpers converting between product units and the authoritative base inventory unit.</summary>
public static class ProductUnitConversion
{
    /// <summary>Converts a quantity entered in a product unit into base inventory quantity.</summary>
    public static decimal ToBaseQuantity(decimal enteredQty, decimal multiplierToBase)
    {
        EnsureValidMultiplier(multiplierToBase);
        return enteredQty * multiplierToBase;
    }

    /// <summary>
    /// Converts a purchase-unit cost into a per-base-unit cost (cost ÷ multiplier),
    /// rounded to 2 decimal places with <see cref="MidpointRounding.AwayFromZero"/>.
    /// </summary>
    public static decimal ToBaseUnitCost(decimal purchaseUnitCost, decimal multiplierToBase)
    {
        EnsureValidMultiplier(multiplierToBase);
        return SaleMoney.RoundMoney(purchaseUnitCost / multiplierToBase);
    }

    public static void EnsureValidMultiplier(decimal multiplierToBase)
    {
        if (multiplierToBase <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitMultiplier,
                "Unit multiplier to base must be greater than zero.");
        }
    }
}
