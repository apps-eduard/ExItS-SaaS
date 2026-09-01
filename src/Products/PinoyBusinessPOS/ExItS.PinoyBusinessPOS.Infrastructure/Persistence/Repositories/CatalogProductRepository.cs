using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
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

    public async Task<CatalogProduct?> FindByNormalizedNameAsync(
        PosOrganizationId organizationId,
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogProducts.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.OrganizationId == organizationId.Value && p.NormalizedName == normalizedName,
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
        var query = ApplyFilter(_db.CatalogProducts.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value), filter);

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

    public async Task<IReadOnlyList<Guid>> ListIdsAsync(
        PosOrganizationId organizationId,
        CatalogProductFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_db.CatalogProducts.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value), filter);

        return await query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(take)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var statusName = CatalogProductStatus.Active.ToString();
        var standardScope = CatalogProductScopes.ToCode(CatalogProductScope.OrganizationStandard);
        var baseQuery = _db.CatalogProducts.AsNoTracking()
            .Where(p =>
                p.OrganizationId == organizationId.Value
                && p.Status == statusName
                && p.Scope == standardScope);

        var total = await baseQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var available = await baseQuery
            .CountAsync(p => p.CanExposeToConnectedBuyers, cancellationToken)
            .ConfigureAwait(false);
        return (total, available, total - available);
    }

    public async Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(
        PosOrganizationId organizationId,
        CatalogProductFilter filter,
        CancellationToken cancellationToken = default)
    {
        // Facets ignore the category filter so the picker can show alternate categories.
        var facetFilter = filter with { CategoryId = null, UncategorizedOnly = false };
        var query = ApplyFilter(_db.CatalogProducts.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value), facetFilter);

        var rows = await query
            .GroupBy(p => p.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.CategoryId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.Select(x => ((Guid?)x.CategoryId, x.Count)).ToList();
    }

    private IQueryable<CatalogProductRecord> ApplyFilter(
        IQueryable<CatalogProductRecord> query,
        CatalogProductFilter filter)
    {
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
        else if (filter.UncategorizedOnly)
        {
            query = query.Where(p => p.CategoryId == null);
        }

        if (filter.BrandId is not null)
        {
            var brandId = filter.BrandId.Value;
            query = query.Where(p => p.BrandId == brandId);
        }

        if (filter.UnitOfMeasure is not null)
        {
            var unitCode = UnitOfMeasures.ToCode(filter.UnitOfMeasure.Value);
            query = query.Where(p => p.UnitOfMeasure == unitCode);
        }

        if (filter.CanExposeToConnectedBuyers is not null)
        {
            var canExpose = filter.CanExposeToConnectedBuyers.Value;
            query = query.Where(p => p.CanExposeToConnectedBuyers == canExpose);
        }

        if (filter.CanBeSold is not null)
        {
            var canBeSold = filter.CanBeSold.Value;
            query = query.Where(p => p.CanBeSold == canBeSold);
        }

        if (filter.Scope is not null)
        {
            var scopeCode = CatalogProductScopes.ToCode(filter.Scope.Value);
            query = query.Where(p => p.Scope == scopeCode);
        }

        if (filter.OriginBranchId is not null)
        {
            var origin = filter.OriginBranchId.Value;
            query = query.Where(p => p.OriginBranchId == origin);
        }

        var standardScope = CatalogProductScopes.ToCode(CatalogProductScope.OrganizationStandard);
        var localScope = CatalogProductScopes.ToCode(CatalogProductScope.BranchLocal);

        if (filter.CommerciallyOfferedAtBranch)
        {
            if (filter.ActingBranchId is null || filter.ActingBranchId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "ActingBranchId is required when CommerciallyOfferedAtBranch is true.");
            }

            var branchId = filter.ActingBranchId.Value;
            // Offered = (Standard OR Local@origin) AND no explicit IsOffered=false row for this branch.
            query = query.Where(p =>
                (p.Scope == standardScope
                 || (p.Scope == localScope && p.OriginBranchId == branchId))
                && !_db.BranchProductAvailabilities.Any(a =>
                    a.OrganizationId == p.OrganizationId
                    && a.BranchId == branchId
                    && a.ProductId == p.Id
                    && !a.IsOffered));
        }
        else if (filter.RestrictBranchLocalToActingBranch)
        {
            if (filter.ActingBranchId is null || filter.ActingBranchId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "ActingBranchId is required when RestrictBranchLocalToActingBranch is true.");
            }

            var branchId = filter.ActingBranchId.Value;
            query = query.Where(p =>
                p.Scope != localScope || p.OriginBranchId == branchId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            var upper = term.ToUpperInvariant();
            var digits = new string(term.Where(char.IsAsciiDigit).ToArray());

            query = query.Where(p =>
                p.Name.ToUpper().Contains(upper)
                || (p.NormalizedSku != null && p.NormalizedSku.Contains(upper))
                || (digits.Length > 0 && p.Barcode != null && p.Barcode.Contains(digits))
                || (p.BrandId != null && _db.ProductBrands.Any(b =>
                    b.Id == p.BrandId
                    && b.OrganizationId == p.OrganizationId
                    && b.NormalizedName.Contains(upper))));
        }

        return query;
    }

    public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default)
    {
        _db.CatalogProducts.Add(CatalogEntityMapper.ToRecord(product));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default)
    {
        // Prefer the change-tracker copy so create/stage flows that Add then Update before
        // SaveChangesAsync still apply domain mutations (EF queries alone miss Added entities).
        var record = _db.CatalogProducts.Local.FirstOrDefault(
            p => p.Id == product.Id.Value && p.OrganizationId == product.OrganizationId.Value);

        if (record is null)
        {
            record = await _db.CatalogProducts
                .FirstOrDefaultAsync(
                    p => p.Id == product.Id.Value && p.OrganizationId == product.OrganizationId.Value,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found.");
        }

        CatalogEntityMapper.ApplyToRecord(product, record);
    }
}
