using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class ExpenseCategoryRepository : IExpenseCategoryRepository
{
    private readonly PosDbContext _db;

    public ExpenseCategoryRepository(PosDbContext db) => _db = db;

    public async Task<ExpenseCategory?> GetByIdAsync(
        PosOrganizationId organizationId,
        ExpenseCategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.ExpenseCategories.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == categoryId.Value && c.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ExpenseEntityMapper.ToDomain(record);
    }

    public async Task<ExpenseCategory?> FindActiveByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var active = ExpenseCategoryStatus.Active.ToString();
        var record = await _db.ExpenseCategories.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId.Value
                     && c.NormalizedName == normalizedName
                     && c.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ExpenseEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<ExpenseCategory> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ExpenseCategoryStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ExpenseCategories.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value);

        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(c => c.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(c => c.NormalizedName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(ExpenseEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<IReadOnlyList<ExpenseCategory>> ListByIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<ExpenseCategoryId> categoryIds,
        CancellationToken cancellationToken = default)
    {
        if (categoryIds.Count == 0)
        {
            return [];
        }

        var ids = categoryIds.Select(c => c.Value).Distinct().ToList();
        var records = await _db.ExpenseCategories.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && ids.Contains(c.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(ExpenseEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(ExpenseCategory category, CancellationToken cancellationToken = default)
    {
        _db.ExpenseCategories.Add(ExpenseEntityMapper.ToRecord(category));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ExpenseCategory category, CancellationToken cancellationToken = default)
    {
        var record = await _db.ExpenseCategories
            .FirstOrDefaultAsync(
                c => c.Id == category.Id.Value && c.OrganizationId == category.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ExpenseCategoryNotFound,
                "Expense category was not found.");
        }

        ExpenseEntityMapper.ApplyToRecord(category, record);
    }
}
