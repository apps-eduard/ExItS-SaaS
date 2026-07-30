using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;

namespace ExItS.PinoyBusinessPOS.Application.Expenses;

public interface IExpenseCategoryRepository
{
    Task<ExpenseCategory?> GetByIdAsync(
        PosOrganizationId organizationId,
        ExpenseCategoryId categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>Finds an Active category by normalized name. Inactive names are reusable.</summary>
    Task<ExpenseCategory?> FindActiveByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ExpenseCategory> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ExpenseCategoryStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(ExpenseCategory category, CancellationToken cancellationToken = default);

    Task UpdateAsync(ExpenseCategory category, CancellationToken cancellationToken = default);
}
