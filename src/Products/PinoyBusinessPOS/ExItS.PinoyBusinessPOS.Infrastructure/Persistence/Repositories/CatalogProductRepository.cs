using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class CatalogProductRepository : ICatalogProductRepository
{
    private readonly PosDbContext _db;

    public CatalogProductRepository(PosDbContext db) => _db = db;

    public async Task<CatalogProduct?> GetByIdAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProducts.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == productId.Value && p.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<CatalogProduct?> FindByNormalizedSkuAsync(
        PosOrganizationId organizationId,
        string normalizedSku,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProducts.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.OrganizationId == organizationId.Value && p.NormalizedSku == normalizedSku,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<CatalogProduct?> FindByBarcodeAsync(
        PosOrganizationId organizationId,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProducts.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.OrganizationId == organizationId.Value && p.Barcode == barcode,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(
        PosOrganizationId organizationId,
        Guid platformGlobalProductId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProducts.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.OrganizationId == organizationId.Value
                     && p.PlatformGlobalProductId == platformGlobalProductId,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> platformGlobalProductIds,
        CancellationToken cancellationToken = default)
    {
        if (platformGlobalProductIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var ids = platformGlobalProductIds.Where(id => id != Guid.Empty).Distinct().ToList();
        var found = await _db.CatalogProducts.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value
                        && p.PlatformGlobalProductId != null
                        && ids.Contains(p.PlatformGlobalProductId.Value))
            .Select(p => p.PlatformGlobalProductId!.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return found.ToHashSet();
    }

    public async Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<CatalogProductId> productIds,
        CancellationToken cancellationToken = default)
    {
        if (productIds.Count == 0)
        {
            return [];
        }

        var ids = productIds.Select(p => p.Value).Distinct().ToList();
        var records = await _db.CatalogProducts.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value && ids.Contains(p.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        CatalogProductFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CatalogProducts.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value);

        if (filter.Status is not null)
        {
            var statusName = filter.Status.Value.ToString();
            query = query.Where(p => p.Status == statusName);
        }

        if (filter.CategoryId is not null)
        {
            var categoryId = filter.CategoryId.Value;
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (filter.UnitOfMeasure is not null)
        {
            var unitCode = UnitOfMeasures.ToCode(filter.UnitOfMeasure.Value);
            query = query.Where(p => p.UnitOfMeasure == unitCode);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            var upper = term.ToUpperInvariant();
            var digits = new string(term.Where(char.IsAsciiDigit).ToArray());

            query = query.Where(p =>
                p.Name.ToUpper().Contains(upper)
                || (p.NormalizedSku != null && p.NormalizedSku.Contains(upper))
                || (digits.Length > 0 && p.Barcode != null && p.Barcode.Contains(digits)));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CatalogEntityMapper.ToDomain).ToList(), total);
    }

    public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
    {
        _db.CatalogProducts.Add(CatalogEntityMapper.ToRecord(product));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProducts
            .FirstOrDefaultAsync(
                p => p.Id == product.Id.Value && p.OrganizationId == product.OrganizationId.Value,
                cancellationToken)
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
