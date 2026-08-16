using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Application.Permissions;

/// <summary>Authoritative role → capability matrix for P10-WP06.</summary>
public static class PosRoleMatrix
{
    private static readonly UtangCapability[] OwnerCapabilities = Enum.GetValues<UtangCapability>();

    private static readonly HashSet<UtangCapability> StoreManagerCapabilities =
    [
        UtangCapability.EnterPos,
        UtangCapability.ViewDashboard,
        UtangCapability.ViewReports,
        UtangCapability.ViewCatalog,
        UtangCapability.ManageCatalog,
        UtangCapability.ViewSales,
        UtangCapability.CreateSale,
        UtangCapability.VoidSale,
        UtangCapability.ViewCustomersAndHistory,
        UtangCapability.CreateCustomer,
        UtangCapability.EditCustomer,
        UtangCapability.CreateCredit,
        UtangCapability.RecordRepayment,
        UtangCapability.ReverseCredit,
        UtangCapability.ReverseRepayment,
        UtangCapability.MutateDueDate,
        UtangCapability.ViewGenerateStatement,
        UtangCapability.ViewGenerateReceipt,
        UtangCapability.ViewInventory,
        UtangCapability.ManageInventory,
        UtangCapability.ViewExpenses,
        UtangCapability.ManageExpenses,
        UtangCapability.ViewSuppliers,
        UtangCapability.ManageSuppliers,
        UtangCapability.ViewPurchasing,
        UtangCapability.ManagePurchasing,
        UtangCapability.ViewShifts,
        UtangCapability.ManageShifts,
        UtangCapability.ViewReturns,
        UtangCapability.ProcessReturn,
        UtangCapability.ViewPermissions,
        UtangCapability.ViewRegisters,
        UtangCapability.ManageRegisters,
        UtangCapability.ViewOperationalSetup,
        UtangCapability.ViewCustomerOrders,
        UtangCapability.ManageCustomerOrders,
        UtangCapability.PlaceCustomerOrders
    ];

    private static readonly HashSet<UtangCapability> CashierCapabilities =
    [
        UtangCapability.EnterPos,
        UtangCapability.ViewCatalog,
        UtangCapability.ViewSales,
        UtangCapability.CreateSale,
        UtangCapability.CreateCredit,
        UtangCapability.ViewShifts,
        UtangCapability.ManageShifts,
        UtangCapability.ViewReturns,
        UtangCapability.ViewRegisters,
        UtangCapability.ViewOperationalSetup
    ];

    private static readonly HashSet<UtangCapability> InventoryStaffCapabilities =
    [
        UtangCapability.EnterPos,
        UtangCapability.ViewCatalog,
        UtangCapability.ViewInventory,
        UtangCapability.ManageInventory,
        UtangCapability.ViewSuppliers,
        UtangCapability.ViewPurchasing,
        UtangCapability.ManagePurchasing,
        UtangCapability.ViewRegisters,
        UtangCapability.ViewOperationalSetup
    ];

    private static readonly HashSet<UtangCapability> ReportingUserCapabilities =
    [
        UtangCapability.EnterPos,
        UtangCapability.ViewDashboard,
        UtangCapability.ViewReports,
        UtangCapability.ViewSales,
        UtangCapability.ViewInventory,
        UtangCapability.ViewExpenses,
        UtangCapability.ViewSuppliers,
        UtangCapability.ViewPurchasing,
        UtangCapability.ViewShifts,
        UtangCapability.ViewReturns,
        UtangCapability.ViewCustomersAndHistory,
        UtangCapability.ViewGenerateStatement,
        UtangCapability.ViewGenerateReceipt,
        UtangCapability.ViewRegisters,
        UtangCapability.ViewOperationalSetup,
        UtangCapability.ViewCustomerOrders
    ];

    public static bool Allows(PosRole role, UtangCapability capability) => role switch
    {
        PosRole.Owner => true,
        PosRole.Admin => true,
        PosRole.StoreManager => StoreManagerCapabilities.Contains(capability),
        PosRole.Cashier => CashierCapabilities.Contains(capability),
        PosRole.InventoryStaff => InventoryStaffCapabilities.Contains(capability),
        PosRole.ReportingUser => ReportingUserCapabilities.Contains(capability),
        _ => false
    };

    /// <summary>
    /// Platform Organization Owner/Administrator management projection for Org Web.
    /// Full (Owner) or day-to-day (Administrator) management — never automatic checkout.
    /// </summary>
    public static bool AllowsOrganizationManagement(bool isExactOwner, UtangCapability capability)
    {
        if (capability is UtangCapability.CreateSale or UtangCapability.EnterPos)
        {
            return false;
        }

        if (isExactOwner)
        {
            return true;
        }

        return StoreManagerCapabilities.Contains(capability)
            && capability is not UtangCapability.CreateSale;
    }

    public static IReadOnlyList<UtangCapability> OrganizationManagementCapabilities(bool isExactOwner)
    {
        if (isExactOwner)
        {
            return OwnerCapabilities
                .Where(c => c is not UtangCapability.CreateSale and not UtangCapability.EnterPos)
                .OrderBy(c => c)
                .ToArray();
        }

        return StoreManagerCapabilities
            .Where(c => c is not UtangCapability.CreateSale and not UtangCapability.EnterPos)
            .OrderBy(c => c)
            .ToArray();
    }

    public static bool AllowsOrganizationManagementReport(bool isExactOwner, PosOperationalReportKind kind) =>
        isExactOwner || AllowsReport(PosRole.StoreManager, kind);

    public static IReadOnlyList<UtangCapability> CapabilitiesFor(PosRole role) => role switch
    {
        PosRole.Owner or PosRole.Admin => OwnerCapabilities,
        PosRole.StoreManager => StoreManagerCapabilities.OrderBy(c => c).ToArray(),
        PosRole.Cashier => CashierCapabilities.OrderBy(c => c).ToArray(),
        PosRole.InventoryStaff => InventoryStaffCapabilities.OrderBy(c => c).ToArray(),
        PosRole.ReportingUser => ReportingUserCapabilities.OrderBy(c => c).ToArray(),
        _ => []
    };

    /// <summary>Which operational report families a role may access.</summary>
    public static bool AllowsReport(PosRole role, PosOperationalReportKind kind) => role switch
    {
        PosRole.Owner or PosRole.Admin or PosRole.StoreManager or PosRole.ReportingUser => true,
        PosRole.Cashier => kind is PosOperationalReportKind.ShiftSummary or PosOperationalReportKind.CashVariance,
        PosRole.InventoryStaff => kind is PosOperationalReportKind.InventoryStatus
            or PosOperationalReportKind.InventoryMovements
            or PosOperationalReportKind.StockCountVariance
            or PosOperationalReportKind.PurchasingSummary
            or PosOperationalReportKind.PurchaseOutstanding,
        _ => false
    };

    public static bool CanAssignRole(PosRole assignerRole, PosRole targetRole) => assignerRole switch
    {
        PosRole.Owner => true,
        PosRole.Admin => targetRole is PosRole.Admin
            or PosRole.StoreManager
            or PosRole.Cashier
            or PosRole.InventoryStaff
            or PosRole.ReportingUser,
        _ => false
    };

    public static bool CanManageAssignments(PosRole role) =>
        role is PosRole.Owner or PosRole.Admin;
}

public enum PosOperationalReportKind
{
    Overview = 0,
    SalesSummary = 1,
    SalesByPayment = 2,
    SalesByProduct = 3,
    Returns = 4,
    ShiftSummary = 5,
    CashVariance = 6,
    InventoryStatus = 7,
    InventoryMovements = 8,
    StockCountVariance = 9,
    PurchasingSummary = 10,
    PurchaseOutstanding = 11,
    SupplierPurchasing = 12,
    Expenses = 13,
    Utang = 14,
    SalesByCashier = 15
}
