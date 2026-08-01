using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository : IProductRepository
{
    private readonly PlatformDbContext _db;

    public ProductRepository(PlatformDbContext db) => _db = db;

    public async Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<Product?> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default)
    {
        var record = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Code == code.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        ProductStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        ListAsync(status, null, CatalogListSortBy.Code, false, skip, take, cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        ProductStatus? status,
        string? search,
        CatalogListSortBy sortBy,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Products.AsNoTracking();
        if (status is not null)
        {
            query = query.Where(p => p.Status == status.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Code.ToLower().Contains(term)
                || p.DisplayName.ToLower().Contains(term));
        }

        query = (sortBy, sortDescending) switch
        {
            (CatalogListSortBy.DisplayName, false) => query.OrderBy(p => p.DisplayName).ThenBy(p => p.Code),
            (CatalogListSortBy.DisplayName, true) => query.OrderByDescending(p => p.DisplayName).ThenBy(p => p.Code),
            (CatalogListSortBy.Status, false) => query.OrderBy(p => p.Status).ThenBy(p => p.Code),
            (CatalogListSortBy.Status, true) => query.OrderByDescending(p => p.Status).ThenBy(p => p.Code),
            (CatalogListSortBy.CreatedAtUtc, false) => query.OrderBy(p => p.CreatedAtUtc).ThenBy(p => p.Code),
            (CatalogListSortBy.CreatedAtUtc, true) => query.OrderByDescending(p => p.CreatedAtUtc).ThenBy(p => p.Code),
            (CatalogListSortBy.UpdatedAtUtc, false) => query.OrderBy(p => p.UpdatedAtUtc).ThenBy(p => p.Code),
            (CatalogListSortBy.UpdatedAtUtc, true) => query.OrderByDescending(p => p.UpdatedAtUtc).ThenBy(p => p.Code),
            (_, true) => query.OrderByDescending(p => p.Code),
            _ => query.OrderBy(p => p.Code)
        };

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CatalogEntityMapper.ToDomain).ToList(), totalCount);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _db.Products.Add(CatalogEntityMapper.ToRecord(product));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        var record = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == product.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        CatalogEntityMapper.ApplyToRecord(product, record);
    }
}
