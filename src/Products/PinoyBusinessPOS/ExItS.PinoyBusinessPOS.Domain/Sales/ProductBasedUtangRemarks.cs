namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Stable remarks and due-date reason text for Product-Based Utang checkout (sale + linked credit).
/// </summary>
public static class ProductBasedUtangRemarks
{
    public static string ForSaleNumber(string saleNumber) =>
        $"Product sale {SaleNumbers.Normalize(saleNumber)}";

    public const string InitialDueDateReason = "Set during Product-Based Utang checkout";
}
