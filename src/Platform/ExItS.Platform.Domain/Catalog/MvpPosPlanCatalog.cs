namespace ExItS.Platform.Domain.Catalog;

/// <summary>Stable MVP Plan keys for Pinoy Business POS (PlanCode values).</summary>
public static class MvpPosPlanCodes
{
    public const string Starter = "starter";
    public const string Growth = "growth";
    public const string Pro = "pro";

    /// <summary>Pre-WP10B mid-tier plan code. Remapped to <see cref="Growth"/> by EnsureMvpPosPlans.</summary>
    public const string LegacyBusiness = "business";

    /// <summary>Obsolete alias for <see cref="LegacyBusiness"/> (pre-WP10B).</summary>
    public const string Business = LegacyBusiness;

    public static readonly IReadOnlyList<string> All = [Starter, Growth, Pro];
}

/// <summary>Commercial package limits and feature toggles for MVP POS plans (WP10B Starter / Growth / Pro).</summary>
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
        bool CustomerCreditEnabled,
        bool AdvancedReportsEnabled,
        bool ExportEnabled,
        bool TrialAllowed,
        int DefaultTrialDays,
        int SortOrder,
        decimal MonthlyPrice,
        decimal AnnualPrice,
        string CurrencyCode = "PHP");

    /// <summary>
    /// DEVELOPMENT/default placeholder PHP prices — not final launch prices.
    /// Feature differentiation uses existing codes only (no fabricated features).
    /// </summary>
    public static readonly IReadOnlyList<Spec> Plans =
    [
        new(
            MvpPosPlanCodes.Starter,
            "Starter",
            "Single-location Pinoy Business POS with one active Business Type",
            MaxBranches: 1,
            MaxActiveStaff: 3,
            MaxActivePosDevices: 1,
            MaxActiveBusinessTypes: 1,
            CustomerCreditEnabled: false,
            AdvancedReportsEnabled: false,
            ExportEnabled: false,
            TrialAllowed: true,
            DefaultTrialDays: 14,
            SortOrder: 10,
            MonthlyPrice: 299m,
            AnnualPrice: 2990m),
        new(
            MvpPosPlanCodes.Growth,
            "Growth",
            "Growing stores with multi-type activation (up to 3 Business Types)",
            MaxBranches: 3,
            MaxActiveStaff: 10,
            MaxActivePosDevices: 3,
            MaxActiveBusinessTypes: 3,
            CustomerCreditEnabled: true,
            AdvancedReportsEnabled: true,
            ExportEnabled: true,
            TrialAllowed: true,
            DefaultTrialDays: 14,
            SortOrder: 20,
            MonthlyPrice: 699m,
            AnnualPrice: 6990m),
        new(
            MvpPosPlanCodes.Pro,
            "Pro",
            "Larger multi-branch Pinoy Business POS (up to 6 Business Types)",
            MaxBranches: 10,
            MaxActiveStaff: 30,
            MaxActivePosDevices: 10,
            MaxActiveBusinessTypes: 6,
            CustomerCreditEnabled: true,
            AdvancedReportsEnabled: true,
            ExportEnabled: true,
            TrialAllowed: false,
            DefaultTrialDays: 0,
            SortOrder: 30,
            MonthlyPrice: 1499m,
            AnnualPrice: 14990m)
    ];
}
