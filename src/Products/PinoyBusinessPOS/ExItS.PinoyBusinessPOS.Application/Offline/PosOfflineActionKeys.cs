namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>Stable action keys for mixed-page online-required operations.</summary>
public static class PosOfflineActionKeys
{
    public const string SwitchOrganization = "org.switch";
    public const string SwitchToPersonal = "org.switch_to_personal";
    public const string CatalogImport = "catalog.import";
    public const string CatalogManage = "catalog.manage";
    public const string PermissionsManage = "permissions.manage";
    public const string SaleNonCashPayment = "sale.payment.non_cash";
    public const string ReportsView = "reports.view";
    public const string InventoryManage = "inventory.manage";
    public const string PurchasingManage = "purchasing.manage";
    public const string ExpensesManage = "expenses.manage";
    public const string SuppliersManage = "suppliers.manage";
    public const string RegistersManage = "registers.manage";
    public const string ShiftsManage = "shifts.manage";
    public const string SetupManage = "setup.manage";
    public const string StaffManage = "org.staff.manage";
    public const string SubscriptionView = "org.subscription";
    public const string CustomerLedger = "customers.ledger";
    public const string CustomerStatement = "customers.statement";
    public const string CustomerOverdue = "customers.overdue";
    public const string SaleHistory = "sales.history";

    public const string PersonalInvite = "personal.invite";
    public const string PersonalLinkUser = "personal.link_user";
    public const string PersonalStartBusiness = "personal.start_business";
    public const string PersonalContactCreate = "personal.contact.create";
    public const string PersonalLentCreate = "personal.lent.create";
    public const string PersonalBorrowedCreate = "personal.borrowed.create";
    public const string PersonalEntryRecord = "personal.entry.record";
}
