namespace ExItS.PinoyBusinessPOS.Domain.Permissions;

/// <summary>Product-local POS operational role (not a Platform role).</summary>
public enum PosRole
{
    Owner = 0,
    Admin = 1,
    StoreManager = 2,
    Cashier = 3,
    InventoryStaff = 4,
    ReportingUser = 5
}

public static class PosRoleCodes
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string StoreManager = "StoreManager";
    public const string Cashier = "Cashier";
    public const string InventoryStaff = "InventoryStaff";
    public const string ReportingUser = "ReportingUser";

    public static string ToCode(PosRole role) => role switch
    {
        PosRole.Owner => Owner,
        PosRole.Admin => Admin,
        PosRole.StoreManager => StoreManager,
        PosRole.Cashier => Cashier,
        PosRole.InventoryStaff => InventoryStaff,
        PosRole.ReportingUser => ReportingUser,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    public static string ToDisplayName(PosRole role) => role switch
    {
        PosRole.StoreManager => "Store Manager",
        PosRole.InventoryStaff => "Inventory Staff",
        PosRole.ReportingUser => "Reporting User",
        _ => ToCode(role)
    };

    public static bool TryParse(string? value, out PosRole role)
    {
        role = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim())
        {
            case Owner:
                role = PosRole.Owner;
                return true;
            case Admin:
                role = PosRole.Admin;
                return true;
            case StoreManager:
                role = PosRole.StoreManager;
                return true;
            case Cashier:
                role = PosRole.Cashier;
                return true;
            case InventoryStaff:
                role = PosRole.InventoryStaff;
                return true;
            case ReportingUser:
                role = PosRole.ReportingUser;
                return true;
            default:
                return false;
        }
    }
}
