namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// User-facing POS / product-local role labels. Internal codes remain Owner/Manager/Cashier/Viewer.
/// </summary>
public static class ProductRoleDisplay
{
    public const string PosOwner = "POS Owner";
    public const string StoreManager = "Manager";
    public const string Cashier = "Cashier";
    public const string InventoryStaff = "Inventory Staff";
    public const string ReportingUser = "Reporting User";

    public static string ToDisplayLabel(string? roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return string.Empty;
        }

        return ProductLocalRoleCodes.NormalizeCatalogCode(roleCode) switch
        {
            ProductLocalRoleCodes.Owner or "POS Owner" or "PosOwner" => PosOwner,
            ProductLocalRoleCodes.Manager or "StoreManager" or "Store Manager" => StoreManager,
            ProductLocalRoleCodes.Cashier => Cashier,
            ProductLocalRoleCodes.InventoryStaff => InventoryStaff,
            ProductLocalRoleCodes.ReportingUser or ProductLocalRoleCodes.Viewer or "Reporting User" =>
                ReportingUser,
            _ => roleCode.Trim()
        };
    }
}
