using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;

namespace ExItS.PinoyBusinessPOS.UnitTests.Expenses;

public sealed class ExpenseDomainTests
{
    private static readonly PosOrganizationId OrgA =
        PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T08:00:00Z");
    private static readonly DateOnly ExpenseDate = new(2026, 7, 30);
    private static readonly ExpenseCategoryId CategoryId = ExpenseCategoryId.New();

    [Fact]
    public void Create_category_defaults_to_active_and_normalizes_name()
    {
        var category = ExpenseCategory.Create(OrgA, "  Utilities  ", Now);

        Assert.Equal("Utilities", category.Name);
        Assert.Equal("UTILITIES", category.NormalizedName);
        Assert.Equal(ExpenseCategoryStatus.Active, category.Status);
    }

    [Fact]
    public void Inactive_category_cannot_be_renamed()
    {
        var category = ExpenseCategory.Create(OrgA, "Rent", Now);
        category.Deactivate(Now);

        var ex = Assert.Throws<DomainException>(() => category.Rename("Lease", Now));
        Assert.Equal(DomainErrorCodes.ExpenseCategoryNotActive, ex.ErrorCode);
    }

    [Fact]
    public void Record_expense_requires_positive_amount_with_at_most_two_decimals()
    {
        var zero = Assert.Throws<DomainException>(() => Record(amount: 0m));
        Assert.Equal(DomainErrorCodes.InvalidExpenseAmount, zero.ErrorCode);

        var precision = Assert.Throws<DomainException>(() => Record(amount: 10.123m));
        Assert.Equal(DomainErrorCodes.InvalidExpenseAmount, precision.ErrorCode);
    }

    [Fact]
    public void Record_cash_expense_rejects_gcash_reference()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Record(paymentMethod: ExpensePaymentMethod.Cash, gcashReference: "REF-1"));
        Assert.Equal(DomainErrorCodes.InvalidExpenseGCashReference, ex.ErrorCode);
    }

    [Fact]
    public void Record_formats_expense_number_and_void_transitions()
    {
        var number = ExpenseNumbers.Format(ExpenseDate, 7);
        Assert.Equal("EXP-20260730-000007", number);

        var expense = Record(expenseNumber: number, amount: 150.50m, paymentMethod: ExpensePaymentMethod.ManualGCash, gcashReference: "gc-9");
        Assert.Equal(ExpenseStatus.Recorded, expense.Status);
        Assert.Equal(150.50m, expense.Amount);
        Assert.Equal("gc-9", expense.GCashReference);

        expense.Void("Wrong amount", Actor, Now);
        Assert.Equal(ExpenseStatus.Voided, expense.Status);
        Assert.Equal("Wrong amount", expense.VoidReason);

        var again = Assert.Throws<DomainException>(() => expense.Void("Again", Actor, Now));
        Assert.Equal(DomainErrorCodes.InvalidExpenseStatusTransition, again.ErrorCode);
    }

    private static Expense Record(
        string? expenseNumber = null,
        ExpensePaymentMethod paymentMethod = ExpensePaymentMethod.Cash,
        decimal amount = 100m,
        string? gcashReference = null) =>
        Expense.Record(
            OrgA,
            expenseNumber ?? ExpenseNumbers.Format(ExpenseDate, 1),
            CategoryId,
            paymentMethod,
            amount,
            "Store supplies",
            ExpenseDate,
            Actor,
            Now,
            payee: "Local supplier",
            gcashReference: gcashReference);
}
