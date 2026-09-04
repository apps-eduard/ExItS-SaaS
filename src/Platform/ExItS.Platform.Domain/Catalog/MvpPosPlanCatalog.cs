namespace ExItS.Platform.Domain.Catalog;

/// <summary>Stable MVP Plan keys for Pinoy Business POS (PlanCode values).</summary>
public static class MvpPosPlanCodes
{
    public const string Starter = "starter";
    public const string Growth = "growth";
    public const string Pro = "pro";
    public const string ProPlus = "pro-plus";

    /// <summary>Pre-WP10B mid-tier plan code. Remapped to <see cref="Growth"/> by EnsureMvpPosPlans.</summary>
    public const string LegacyBusiness = "business";

    /// <summary>Obsolete alias for <see cref="LegacyBusiness"/> (pre-WP10B).</summary>
    public const string Business = LegacyBusiness;

    public static readonly IReadOnlyList<string> All = [Starter, Growth, Pro, ProPlus];
}

/// <summary>
/// Commercial package limits and feature toggles for Pinoy Business POS
/// (Starter / Growth / Pro / Pro+). DEVELOPMENT placeholder prices — not final launch prices.
/// </summary>
public static class MvpPosPlanCatalog
{
    public sealed record Spec(
        string PlanKey,
        string DisplayName,
        string Description,
        int MaxBranches,
        int MaxActiveStaff,
        int MaxActivePosDevices,
        int MaxActiveBusinessTypes,
        int MaxAreas,
        bool CustomerCreditEnabled,
        bool AdvancedReportsEnabled,
        bool ExportEnabled,
        bool WarehouseEnabled,
        bool CustomerOrderingEnabled,
        bool DeliveryOrdersEnabled,
        bool TrialAllowed,
        int DefaultTrialDays,
        int SortOrder,
        decimal MonthlyPrice,
        decimal AnnualPrice,
        string CurrencyCode = "PHP");

    /// <summary>
    /// DEVELOPMENT/default placeholder PHP prices — not final launch prices.
    /// Feature differentiation reuses existing codes; warehouse/area use dedicated grants.
    /// </summary>
    public static readonly IReadOnlyList<Spec> Plans =
    [
        new(
            MvpPosPlanCodes.Starter,
            "Starter",
            "For one small store",
            MaxBranches: 1,
            MaxActiveStaff: 3,
            MaxActivePosDevices: 1,
            MaxActiveBusinessTypes: 1,
            MaxAreas: 0,
            CustomerCreditEnabled: true,
            AdvancedReportsEnabled: false,
            ExportEnabled: false,
            WarehouseEnabled: false,
            CustomerOrderingEnabled: false,
            DeliveryOrdersEnabled: false,
            TrialAllowed: true,
            DefaultTrialDays: 14,
            SortOrder: 10,
            MonthlyPrice: 299m,
            AnnualPrice: 2990m),
        new(
            MvpPosPlanCodes.Growth,
            "Growth",
            "For growing businesses",
            MaxBranches: 3,
            MaxActiveStaff: 10,
            MaxActivePosDevices: 3,
            MaxActiveBusinessTypes: 3,
            MaxAreas: 0,
            CustomerCreditEnabled: true,
            AdvancedReportsEnabled: false,
            ExportEnabled: false,
            WarehouseEnabled: false,
            CustomerOrderingEnabled: true,
            DeliveryOrdersEnabled: true,
            TrialAllowed: true,
            DefaultTrialDays: 14,
            SortOrder: 20,
            MonthlyPrice: 699m,
            AnnualPrice: 6990m),
        new(
            MvpPosPlanCodes.Pro,
            "Pro",
            "For multi-branch operations",
            MaxBranches: 10,
            MaxActiveStaff: 30,
            MaxActivePosDevices: 10,
            MaxActiveBusinessTypes: 6,
            MaxAreas: 3,
            CustomerCreditEnabled: true,
            AdvancedReportsEnabled: true,
            ExportEnabled: true,
            WarehouseEnabled: true,
            CustomerOrderingEnabled: true,
            DeliveryOrdersEnabled: true,
            TrialAllowed: false,
            DefaultTrialDays: 0,
            SortOrder: 30,
            MonthlyPrice: 1499m,
            AnnualPrice: 14990m),
        new(
            MvpPosPlanCodes.ProPlus,
            "Pro+",
            "For larger operations",
            MaxBranches: 25,
            MaxActiveStaff: 75,
            MaxActivePosDevices: 25,
            MaxActiveBusinessTypes: 12,
            MaxAreas: 10,
            CustomerCreditEnabled: true,
            AdvancedReportsEnabled: true,
            ExportEnabled: true,
            WarehouseEnabled: true,
            CustomerOrderingEnabled: true,
            DeliveryOrdersEnabled: true,
            TrialAllowed: false,
            DefaultTrialDays: 0,
            SortOrder: 40,
            MonthlyPrice: 2499m,
            AnnualPrice: 24990m)
    ];
}
