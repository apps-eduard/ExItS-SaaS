using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class ProductCategoryRepository : IProductCategoryRepository
{
    private readonly PosDbContext _db;

    public ProductCategoryRepository(PosDbContext db) => _db = db;

    public async Task<ProductCategory?> GetByIdAsync(
        PosOrganizationId organizationId,
        ProductCategoryId categoryId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.ProductCategories.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == categoryId.Value && c.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<ProductCategory?> FindActiveByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var active = ProductCategoryStatus.Active.ToString();
        var record = await _db.ProductCategories.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId.Value
                     && c.NormalizedName == normalizedName
                     && c.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<ProductCategory> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ProductCategoryStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductCategories.AsNoTracking()
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

        return (records.Select(CatalogEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<IReadOnlyList<ProductCategory>> ListByIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<ProductCategoryId> categoryIds,
        CancellationToken cancellationToken = default)
    {
        if (categoryIds.Count == 0)
        {
            return [];
        }

        var ids = categoryIds.Select(c => c.Value).Distinct().ToList();
        var records = await _db.ProductCategories.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && ids.Contains(c.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        _db.ProductCategories.Add(CatalogEntityMapper.ToRecord(category));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        var record = await _db.ProductCategories
            .FirstOrDefaultAsync(
                c => c.Id == category.Id.Value && c.OrganizationId == category.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CategoryNotFound,
                "Category was not found.");
        }

        CatalogEntityMapper.ApplyToRecord(category, record);
    }
}
