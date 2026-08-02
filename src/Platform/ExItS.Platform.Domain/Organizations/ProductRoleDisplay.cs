namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// User-facing POS / product-local role labels. Internal codes remain Owner/Manager/Cashier/Viewer.
/// </summary>
public static class ProductRoleDisplay
{
    public const string PosOwner = "POS Owner";
    public const string StoreManager = "Store Manager";
    public const string Cashier = "Cashier";
    public const string ReportingUser = "Reporting User";

    public static string ToDisplayLabel(string? roleCode)
    {
        if (string.IsNullOrWhiteSpace(roleCode))
        {
            return string.Empty;
        }

        return roleCode.Trim() switch
        {
            ProductLocalRoleCodes.Owner or "POS Owner" or "PosOwner" => PosOwner,
            ProductLocalRoleCodes.Manager or "StoreManager" or "Store Manager" => StoreManager,
            ProductLocalRoleCodes.Cashier => Cashier,
            ProductLocalRoleCodes.Viewer or "ReportingUser" or "Reporting User" => ReportingUser,
            _ => roleCode
        };
    }
}
