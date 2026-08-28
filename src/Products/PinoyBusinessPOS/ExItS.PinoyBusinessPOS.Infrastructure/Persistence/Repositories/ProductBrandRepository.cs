using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class ProductBrandRepository : IProductBrandRepository
{
    private readonly PosDbContext _db;

    public ProductBrandRepository(PosDbContext db) => _db = db;

    public async Task<ProductBrand?> GetByIdAsync(
        PosOrganizationId organizationId,
        ProductBrandId brandId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.ProductBrands.AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.Id == brandId.Value && b.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<ProductBrand?> FindActiveByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var active = ProductBrandStatus.Active.ToString();
        var record = await _db.ProductBrands.AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.OrganizationId == organizationId.Value
                     && b.NormalizedName == normalizedName
                     && b.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<ProductBrand> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ProductBrandStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductBrands.AsNoTracking()
            .Where(b => b.OrganizationId == organizationId.Value);

        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(b => b.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToUpperInvariant();
            query = query.Where(b => b.NormalizedName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(b => b.Name)
            .ThenBy(b => b.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CatalogEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<IReadOnlyList<ProductBrand>> ListByIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<ProductBrandId> brandIds,
        CancellationToken cancellationToken = default)
    {
        if (brandIds.Count == 0)
        {
            return [];
        }

        var ids = brandIds.Select(b => b.Value).Distinct().ToList();
        var records = await _db.ProductBrands.AsNoTracking()
            .Where(b => b.OrganizationId == organizationId.Value && ids.Contains(b.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(ProductBrand brand, CancellationToken cancellationToken = default)
    {
        _db.ProductBrands.Add(CatalogEntityMapper.ToRecord(brand));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ProductBrand brand, CancellationToken cancellationToken = default)
    {
        var record = await _db.ProductBrands
            .FirstOrDefaultAsync(
                b => b.Id == brand.Id.Value && b.OrganizationId == brand.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.BrandNotFound,
                "Brand was not found.");
        }

        CatalogEntityMapper.ApplyToRecord(brand, record);
    }
}
