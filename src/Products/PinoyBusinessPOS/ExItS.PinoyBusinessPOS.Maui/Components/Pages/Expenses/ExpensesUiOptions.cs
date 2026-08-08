using ExItS.DesignSystem.Components.Primitives;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Expenses;

/// <summary>
/// Localized labels for the controlled expense option sets. Stable codes come from the Application
/// layer; only the display text is localized here.
/// </summary>
internal static class ExpensesUiOptions
{
    public const string ActiveCategoryStatus = "Active";
    public const string InactiveCategoryStatus = "Inactive";

    public static IReadOnlyList<SelectOption> PaymentMethods(IStringLocalizer<PosResources> localizer) =>
        PosExpenseOptions.PaymentMethodCodes
            .Select(code => new SelectOption(code, PaymentMethodLabel(localizer, code)))
            .ToList();

    public static string PaymentMethodLabel(IStringLocalizer<PosResources> localizer, string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : localizer[$"Expenses_Payment_{code}"].Value;

    public static string StatusLabel(IStringLocalizer<PosResources> localizer, string? status) =>
        string.Equals(status, PosExpenseOptions.VoidedStatus, StringComparison.Ordinal)
            ? localizer["Expenses_Status_Voided"].Value
            : localizer["Expenses_Status_Recorded"].Value;

    public static string CategoryStatusLabel(IStringLocalizer<PosResources> localizer, string? status) =>
        string.Equals(status, ActiveCategoryStatus, StringComparison.Ordinal)
            ? localizer["Expenses_CategoryStatus_Active"].Value
            : localizer["Expenses_CategoryStatus_Inactive"].Value;

    public static IReadOnlyList<SelectOption> StatusFilters(IStringLocalizer<PosResources> localizer) =>
    [
        new(string.Empty, localizer["Expenses_Filter_AllStatuses"].Value),
        new(PosExpenseOptions.RecordedStatus, localizer["Expenses_Status_Recorded"].Value),
        new(PosExpenseOptions.VoidedStatus, localizer["Expenses_Status_Voided"].Value)
    ];

    public static IReadOnlyList<SelectOption> PaymentMethodFilters(IStringLocalizer<PosResources> localizer)
    {
        var options = new List<SelectOption>
        {
            new(string.Empty, localizer["Expenses_Filter_AllPayments"].Value)
        };
        options.AddRange(PaymentMethods(localizer));
        return options;
    }

    public static IReadOnlyList<SelectOption> CategoryStatusFilters(IStringLocalizer<PosResources> localizer) =>
    [
        new(string.Empty, localizer["Expenses_Category_Filter_All"].Value),
        new(ActiveCategoryStatus, localizer["Expenses_CategoryStatus_Active"].Value),
        new(InactiveCategoryStatus, localizer["Expenses_CategoryStatus_Inactive"].Value)
    ];

    public static IReadOnlyList<SelectOption> CategoryChoices(
        IStringLocalizer<PosResources> localizer,
        IEnumerable<PosExpenseCategoryDto> categories)
    {
        var options = new List<SelectOption>
        {
            new(string.Empty, localizer["Expenses_SelectCategory"].Value)
        };
        options.AddRange(categories.Select(c => new SelectOption(c.CategoryId.ToString("D"), c.Name)));
        return options;
    }
}
