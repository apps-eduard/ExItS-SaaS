namespace ExItS.Platform.Domain.Catalog;

/// <summary>Stable MVP Plan keys for Pinoy Business POS (PlanCode values).</summary>
public static class MvpPosPlanCodes
{
    public const string Starter = "starter";
    public const string Business = "business";
    public const string Pro = "pro";

    public static readonly IReadOnlyList<string> All = [Starter, Business, Pro];
}

/// <summary>Commercial package limits and feature toggles for MVP POS plans.</summary>
public static class MvpPosPlanCatalog
{
    public sealed record Spec(
        string PlanKey,
        string DisplayName,
        string Description,
        int MaxBranches,
        int MaxActiveStaff,
        bool CustomerCreditEnabled,
        bool AdvancedReportsEnabled,
        bool ExportEnabled,
        bool TrialAllowed,
        int DefaultTrialDays,
        int SortOrder,
        decimal MonthlyPrice,
        decimal AnnualPrice,
        string CurrencyCode = "PHP");

    public static readonly IReadOnlyList<Spec> Plans =
    [
        new(
            MvpPosPlanCodes.Starter,
            "Starter",
            "For small single-location stores",
            MaxBranches: 1,
            MaxActiveStaff: 3,
            CustomerCreditEnabled: false,
            AdvancedReportsEnabled: false,
            ExportEnabled: false,
            TrialAllowed: true,
            DefaultTrialDays: 14,
            SortOrder: 10,
            MonthlyPrice: 299m,
            AnnualPrice: 2990m),
        new(
            MvpPosPlanCodes.Business,
            "Business",
            "For growing stores with multiple staff",
            MaxBranches: 3,
            MaxActiveStaff: 15,
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
            "For larger multi-branch businesses",
            MaxBranches: 10,
            MaxActiveStaff: 50,
            CustomerCreditEnabled: true,
            AdvancedReportsEnabled: true,
            ExportEnabled: true,
            TrialAllowed: true,
            DefaultTrialDays: 14,
            SortOrder: 30,
            MonthlyPrice: 1499m,
            AnnualPrice: 14990m)
    ];
}
