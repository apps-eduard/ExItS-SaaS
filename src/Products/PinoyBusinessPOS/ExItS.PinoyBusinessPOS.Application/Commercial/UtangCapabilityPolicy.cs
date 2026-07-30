namespace ExItS.PinoyBusinessPOS.Application.Commercial;

/// <summary>Server-side feature codes for Utang commercial authorization (Platform entitlement codes).</summary>
public static class PosFeatureCodes
{
    public const string CustomerCreditView = "customer-credit-view";
    public const string CustomerCreditRepay = "customer-credit-repay";
    public const string CustomerCreditCreate = "customer-credit-create";
    public const string StoreCatalogView = "store-catalog-view";
    public const string StoreCatalogManage = "store-catalog-manage";
    public const string StoreSalesView = "store-sales-view";
    public const string StoreSalesCreate = "store-sales-create";
    public const string StoreSalesVoid = "store-sales-void";
    public const string StoreInventoryView = "store-inventory-view";
    public const string StoreInventoryManage = "store-inventory-manage";
    public const string StoreExpensesView = "store-expenses-view";
    public const string StoreExpensesManage = "store-expenses-manage";
    public const string StoreDashboardView = "store-dashboard-view";
    public const string StoreReportsView = "store-reports-view";
    public const string StoreSuppliersView = "store-suppliers-view";
    public const string StoreSuppliersManage = "store-suppliers-manage";
}

/// <summary>Subscription status names mirrored from Platform (string-stable for headers/session).</summary>
public static class PosSubscriptionStatuses
{
    public const string Trialing = "Trialing";
    public const string Active = "Active";
    public const string GracePeriod = "GracePeriod";
    public const string PastDue = "PastDue";
    public const string Cancelled = "Cancelled";
    public const string Expired = "Expired";
    public const string Suspended = "Suspended";
}

/// <summary>Utang operations gated by the P6-WP05 capability matrix.</summary>
public enum UtangCapability
{
    EnterPos = 0,
    ViewCustomersAndHistory = 1,
    CreateCustomer = 2,
    EditCustomer = 3,
    CreateCredit = 4,
    RecordRepayment = 5,
    ReverseCredit = 6,
    ReverseRepayment = 7,
    MutateDueDate = 8,
    ViewGenerateStatement = 9,
    ViewGenerateReceipt = 10,
    ViewCatalog = 11,
    ManageCatalog = 12,
    ViewSales = 13,
    CreateSale = 14,
    VoidSale = 15,
    ViewInventory = 16,
    ManageInventory = 17,
    ViewExpenses = 18,
    ManageExpenses = 19,
    ViewDashboard = 20,
    ViewReports = 21,
    ViewSuppliers = 22,
    ManageSuppliers = 23
}

/// <summary>
/// Authoritative PinoyBusinessPOS Utang capability matrix (P6-WP05).
/// Centralized outside Razor. Both product entry and feature grants must pass for protected ops.
/// Missing/stale/unknown/invalid entitlement denies every protected capability.
/// </summary>
public static class UtangCapabilityPolicy
{
    public static bool IsFullCommercialState(string? subscriptionStatus) =>
        Normalize(subscriptionStatus) is PosSubscriptionStatuses.Trialing
            or PosSubscriptionStatuses.Active
            or PosSubscriptionStatuses.GracePeriod;

    public static bool IsContinuityState(string? subscriptionStatus) =>
        Normalize(subscriptionStatus) is PosSubscriptionStatuses.PastDue
            or PosSubscriptionStatuses.Cancelled
            or PosSubscriptionStatuses.Expired;

    public static bool IsSuspended(string? subscriptionStatus) =>
        string.Equals(Normalize(subscriptionStatus), PosSubscriptionStatuses.Suspended, StringComparison.Ordinal);

    public static bool HasFeature(IEnumerable<string>? enabledFeatureCodes, string featureCode)
    {
        if (enabledFeatureCodes is null || string.IsNullOrWhiteSpace(featureCode))
        {
            return false;
        }

        return enabledFeatureCodes.Any(c =>
            string.Equals(c, featureCode, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsAllowed(
        UtangCapability capability,
        string? subscriptionStatus,
        IEnumerable<string>? enabledFeatureCodes)
    {
        var status = Normalize(subscriptionStatus);
        if (string.IsNullOrEmpty(status) || IsSuspended(status))
        {
            return false;
        }

        var grants = enabledFeatureCodes?.ToArray() ?? [];

        return capability switch
        {
            UtangCapability.EnterPos => CanEnter(status, grants),

            UtangCapability.ViewCustomersAndHistory
                or UtangCapability.ViewGenerateStatement
                or UtangCapability.ViewGenerateReceipt
                or UtangCapability.ReverseCredit =>
                CanEnter(status, grants) && HasFeature(grants, PosFeatureCodes.CustomerCreditView),

            UtangCapability.RecordRepayment =>
                CanEnter(status, grants) && HasFeature(grants, PosFeatureCodes.CustomerCreditRepay),

            UtangCapability.ReverseRepayment =>
                IsFullCommercialState(status)
                && HasFeature(grants, PosFeatureCodes.CustomerCreditRepay),

            UtangCapability.CreateCustomer
                or UtangCapability.EditCustomer
                or UtangCapability.CreateCredit
                or UtangCapability.MutateDueDate =>
                IsFullCommercialState(status)
                && HasFeature(grants, PosFeatureCodes.CustomerCreditCreate),

            UtangCapability.ViewCatalog =>
                CanEnter(status, grants) && HasFeature(grants, PosFeatureCodes.StoreCatalogView),

            UtangCapability.ManageCatalog =>
                IsFullCommercialState(status)
                && HasFeature(grants, PosFeatureCodes.StoreCatalogManage),

            UtangCapability.ViewSales =>
                CanEnter(status, grants) && HasFeature(grants, PosFeatureCodes.StoreSalesView),

            UtangCapability.CreateSale =>
                IsFullCommercialState(status)
                && HasFeature(grants, PosFeatureCodes.StoreSalesCreate),

            UtangCapability.VoidSale =>
                IsFullCommercialState(status)
                && HasFeature(grants, PosFeatureCodes.StoreSalesVoid),

            UtangCapability.ViewInventory =>
                CanEnter(status, grants) && HasFeature(grants, PosFeatureCodes.StoreInventoryView),

            UtangCapability.ManageInventory =>
                IsFullCommercialState(status)
                && HasFeature(grants, PosFeatureCodes.StoreInventoryManage),

            UtangCapability.ViewExpenses =>
                CanEnter(status, grants) && HasFeature(grants, PosFeatureCodes.StoreExpensesView),

            UtangCapability.ManageExpenses =>
                IsFullCommercialState(status)
                && HasFeature(grants, PosFeatureCodes.StoreExpensesManage),

            // Continuity/read: available in PastDue/Cancelled/Expired when granted (not Suspended).
            UtangCapability.ViewDashboard =>
                CanEnter(status, grants) && HasFeature(grants, PosFeatureCodes.StoreDashboardView),

            UtangCapability.ViewReports =>
                CanEnter(status, grants) && HasFeature(grants, PosFeatureCodes.StoreReportsView),

            UtangCapability.ViewSuppliers =>
                CanEnter(status, grants) && HasFeature(grants, PosFeatureCodes.StoreSuppliersView),

            UtangCapability.ManageSuppliers =>
                IsFullCommercialState(status)
                && HasFeature(grants, PosFeatureCodes.StoreSuppliersManage),

            _ => false
        };
    }

    public static bool CanEnter(string? subscriptionStatus, IEnumerable<string>? enabledFeatureCodes)
    {
        var status = Normalize(subscriptionStatus);
        if (string.IsNullOrEmpty(status) || IsSuspended(status))
        {
            return false;
        }

        var grants = enabledFeatureCodes?.ToArray() ?? [];

        return status switch
        {
            PosSubscriptionStatuses.Trialing
                or PosSubscriptionStatuses.Active
                or PosSubscriptionStatuses.GracePeriod => true,
            PosSubscriptionStatuses.PastDue
                or PosSubscriptionStatuses.Cancelled
                or PosSubscriptionStatuses.Expired =>
                HasFeature(grants, PosFeatureCodes.CustomerCreditView)
                || HasFeature(grants, PosFeatureCodes.CustomerCreditRepay)
                || HasFeature(grants, PosFeatureCodes.StoreCatalogView)
                || HasFeature(grants, PosFeatureCodes.StoreCatalogManage)
                || HasFeature(grants, PosFeatureCodes.StoreSalesView)
                || HasFeature(grants, PosFeatureCodes.StoreInventoryView)
                || HasFeature(grants, PosFeatureCodes.StoreExpensesView)
                || HasFeature(grants, PosFeatureCodes.StoreDashboardView)
                || HasFeature(grants, PosFeatureCodes.StoreReportsView)
                || HasFeature(grants, PosFeatureCodes.StoreSuppliersView)
                || HasFeature(grants, PosFeatureCodes.StoreSuppliersManage),
            _ => false
        };
    }

    public static IReadOnlyList<string> DefaultDevelopmentGrants { get; } =
    [
        PosFeatureCodes.CustomerCreditView,
        PosFeatureCodes.CustomerCreditRepay,
        PosFeatureCodes.CustomerCreditCreate,
        PosFeatureCodes.StoreCatalogView,
        PosFeatureCodes.StoreCatalogManage,
        PosFeatureCodes.StoreSalesView,
        PosFeatureCodes.StoreSalesCreate,
        PosFeatureCodes.StoreSalesVoid,
        PosFeatureCodes.StoreInventoryView,
        PosFeatureCodes.StoreInventoryManage,
        PosFeatureCodes.StoreExpensesView,
        PosFeatureCodes.StoreExpensesManage,
        PosFeatureCodes.StoreDashboardView,
        PosFeatureCodes.StoreReportsView,
        PosFeatureCodes.StoreSuppliersView,
        PosFeatureCodes.StoreSuppliersManage
    ];

    private static string Normalize(string? subscriptionStatus) =>
        string.IsNullOrWhiteSpace(subscriptionStatus) ? string.Empty : subscriptionStatus.Trim();
}

/// <summary>Resolved commercial access for the current POS request/session.</summary>
public sealed record PosCommercialAccess(
    string? SubscriptionStatus,
    IReadOnlyList<string> EnabledFeatureCodes,
    bool IsKnown)
{
    public static PosCommercialAccess Unknown { get; } = new(null, [], IsKnown: false);

    public static PosCommercialAccess DevelopmentDefault { get; } =
        new(PosSubscriptionStatuses.Active, UtangCapabilityPolicy.DefaultDevelopmentGrants, IsKnown: true);

    public bool Allows(UtangCapability capability) =>
        IsKnown && UtangCapabilityPolicy.IsAllowed(capability, SubscriptionStatus, EnabledFeatureCodes);
}

public interface IPosCommercialAccessAccessor
{
    PosCommercialAccess Current { get; set; }
}

public sealed class PosCommercialAccessAccessor : IPosCommercialAccessAccessor
{
    public PosCommercialAccess Current { get; set; } = PosCommercialAccess.Unknown;
}

public static class CommercialAccessGuard
{
    public static Application.Common.ApplicationResult Require(
        IPosCommercialAccessAccessor accessor,
        UtangCapability capability)
    {
        var access = accessor.Current;
        if (!access.IsKnown)
        {
            return Application.Common.ApplicationResult.Failure(
                Application.Common.ApplicationErrorCodes.CommercialAccessUnknown,
                "Commercial entitlement context is missing, stale, or invalid.");
        }

        if (!access.Allows(capability))
        {
            return Application.Common.ApplicationResult.Failure(
                Application.Common.ApplicationErrorCodes.CommercialCapabilityDenied,
                $"Capability '{capability}' is not permitted for subscription '{access.SubscriptionStatus ?? "unknown"}'.");
        }

        return Application.Common.ApplicationResult.Success();
    }

    public static Application.Common.ApplicationResult<T> DenyUnknown<T>() =>
        Application.Common.ApplicationResult<T>.Failure(
            Application.Common.ApplicationErrorCodes.CommercialAccessUnknown,
            "Commercial entitlement context is missing, stale, or invalid.");

    public static Application.Common.ApplicationResult<T> DenyCapability<T>(UtangCapability capability, PosCommercialAccess access) =>
        Application.Common.ApplicationResult<T>.Failure(
            Application.Common.ApplicationErrorCodes.CommercialCapabilityDenied,
            $"Capability '{capability}' is not permitted for subscription '{access.SubscriptionStatus ?? "unknown"}'.");
}
