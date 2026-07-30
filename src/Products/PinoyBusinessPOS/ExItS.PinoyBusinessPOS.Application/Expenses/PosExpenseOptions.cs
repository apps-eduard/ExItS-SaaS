using ExItS.PinoyBusinessPOS.Domain.Expenses;

namespace ExItS.PinoyBusinessPOS.Application.Expenses;

/// <summary>
/// Controlled expense option sets surfaced to clients and UI. Stable codes only — localized labels
/// belong to the presentation resource files.
/// </summary>
public static class PosExpenseOptions
{
    public static IReadOnlyList<string> PaymentMethodCodes { get; } = ExpensePaymentMethods.Codes;

    public static IReadOnlyList<string> ExpenseStatuses { get; } =
    [
        nameof(ExpenseStatus.Recorded),
        nameof(ExpenseStatus.Voided)
    ];

    public const string RecordedStatus = nameof(ExpenseStatus.Recorded);
    public const string VoidedStatus = nameof(ExpenseStatus.Voided);
    public const string CashPaymentMethod = nameof(ExpensePaymentMethod.Cash);
    public const string ManualGCashPaymentMethod = nameof(ExpensePaymentMethod.ManualGCash);

    public const int DescriptionMaxLength = Expense.DescriptionMaxLength;
    public const int PayeeMaxLength = Expense.PayeeMaxLength;
    public const int GCashReferenceMaxLength = Expense.GCashReferenceMaxLength;
    public const int VoidReasonMaxLength = Expense.VoidReasonMaxLength;
    public const int CategoryNameMaxLength = ExpenseCategory.NameMaxLength;

    /// <summary>Client-side preview rounding. Matches the server rule exactly (2dp, away from zero).</summary>
    public static decimal RoundMoney(decimal amount) => ExpenseMoney.RoundMoney(amount);
}
