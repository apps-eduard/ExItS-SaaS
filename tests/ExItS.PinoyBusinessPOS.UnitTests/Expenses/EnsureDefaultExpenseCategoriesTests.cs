using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;

namespace ExItS.PinoyBusinessPOS.UnitTests.Expenses;

public sealed class EnsureDefaultExpenseCategoriesTests
{
    private static readonly Guid OrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-05T08:00:00Z");

    [Fact]
    public async Task Execute_seeds_five_default_active_categories()
    {
        var repo = new FakeCategoryRepository();
        var ensure = new EnsureDefaultExpenseCategories(repo, new FakeUow(), new FixedClock(Now));

        await ensure.ExecuteAsync(OrgId);

        Assert.Equal(5, repo.Items.Count);
        Assert.Equal(
            EnsureDefaultExpenseCategories.DefaultNames.OrderBy(n => n, StringComparer.Ordinal),
            repo.Items.Select(c => c.Name).OrderBy(n => n, StringComparer.Ordinal));
        Assert.All(repo.Items, c => Assert.Equal(ExpenseCategoryStatus.Active, c.Status));
    }

    [Fact]
    public async Task Execute_is_idempotent_and_skips_existing_active_names()
    {
        var repo = new FakeCategoryRepository();
        var ensure = new EnsureDefaultExpenseCategories(repo, new FakeUow(), new FixedClock(Now));
        await repo.AddAsync(ExpenseCategory.Create(PosOrganizationId.From(OrgId), "Rent", Now));
        await repo.AddAsync(ExpenseCategory.Create(PosOrganizationId.From(OrgId), "Custom", Now));

        await ensure.ExecuteAsync(OrgId);
        await ensure.ExecuteAsync(OrgId);

        Assert.Equal(6, repo.Items.Count);
        Assert.Contains(repo.Items, c => c.Name == "Custom");
        foreach (var name in EnsureDefaultExpenseCategories.DefaultNames)
        {
            Assert.Single(repo.Items, c =>
                string.Equals(c.NormalizedName, ExpenseCategory.NormalizeForLookup(name), StringComparison.Ordinal)
                && c.Status == ExpenseCategoryStatus.Active);
        }
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow => utcNow;
    }

    private sealed class FakeUow : IPosUnitOfWork
    {
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken = default) =>
            action(cancellationToken);
    }

    private sealed class FakeCategoryRepository : IExpenseCategoryRepository
    {
        public List<ExpenseCategory> Items { get; } = [];

        public Task<ExpenseCategory?> GetByIdAsync(
            PosOrganizationId organizationId,
            ExpenseCategoryId categoryId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(c =>
                c.OrganizationId == organizationId && c.Id == categoryId));

        public Task<ExpenseCategory?> FindActiveByNormalizedNameAsync(
            PosOrganizationId organizationId,
            string normalizedName,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(c =>
                c.OrganizationId == organizationId
                && c.Status == ExpenseCategoryStatus.Active
                && string.Equals(c.NormalizedName, normalizedName, StringComparison.Ordinal)));

        public Task<(IReadOnlyList<ExpenseCategory> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId,
            ExpenseCategoryStatus? status,
            string? search,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            var query = Items.Where(c => c.OrganizationId == organizationId);
            if (status is not null)
            {
                query = query.Where(c => c.Status == status);
            }

            var list = query.Skip(skip).Take(take).ToList();
            return Task.FromResult(((IReadOnlyList<ExpenseCategory>)list, query.Count()));
        }

        public Task<IReadOnlyList<ExpenseCategory>> ListByIdsAsync(
            PosOrganizationId organizationId,
            IReadOnlyCollection<ExpenseCategoryId> categoryIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExpenseCategory>>(
                Items.Where(c => c.OrganizationId == organizationId && categoryIds.Contains(c.Id)).ToList());

        public Task AddAsync(ExpenseCategory category, CancellationToken cancellationToken = default)
        {
            Items.Add(category);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ExpenseCategory category, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
