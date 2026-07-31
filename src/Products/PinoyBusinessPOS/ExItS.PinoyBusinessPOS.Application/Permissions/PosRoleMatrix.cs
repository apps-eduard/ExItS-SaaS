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
        UtangCapability.ManageRegisters
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
        UtangCapability.ProcessReturn,
        UtangCapability.ViewRegisters
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
        UtangCapability.ViewRegisters
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
        UtangCapability.ViewRegisters
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
    Utang = 14
}
