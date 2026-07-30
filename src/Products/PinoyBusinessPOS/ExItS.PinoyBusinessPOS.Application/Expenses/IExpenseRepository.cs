using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;

namespace ExItS.PinoyBusinessPOS.Application.Expenses;

public sealed record ExpenseFilter(
    ExpenseStatus? Status = null,
    ExpensePaymentMethod? PaymentMethod = null,
    ExpenseCategoryId? CategoryId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    string? ExpenseNumber = null);

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(
        PosOrganizationId organizationId,
        ExpenseId expenseId,
        CancellationToken cancellationToken = default);

    Task<Expense?> FindByExpenseNumberAsync(
        PosOrganizationId organizationId,
        string expenseNumber,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Expense> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ExpenseFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserves the next org+business-date expense number, creates the expense, and saves in one
    /// transaction under an advisory lock.
    /// </summary>
    Task<Expense> RecordAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, Expense> createExpense,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Expense expense, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Expense>> ListForSummaryAsync(
        PosOrganizationId organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);
}
