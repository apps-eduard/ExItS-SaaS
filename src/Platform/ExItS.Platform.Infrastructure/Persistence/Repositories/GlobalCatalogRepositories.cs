using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Infrastructure.Persistence.GlobalCatalog;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class BusinessTypeRepository : IBusinessTypeRepository
{
    private readonly PlatformDbContext _db;

    public BusinessTypeRepository(PlatformDbContext db) => _db = db;

    public async Task<BusinessType?> GetByIdAsync(
        BusinessTypeId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BusinessTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : GlobalCatalogEntityMapper.ToDomain(record);
    }

    public async Task<BusinessType?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim();
        var record = await _db.BusinessTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Code.ToLower() == normalized.ToLower(), cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : GlobalCatalogEntityMapper.ToDomain(record);
    }

    public async Task<BusinessType?> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BusinessTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.NormalizedName == normalizedName, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : GlobalCatalogEntityMapper.ToDomain(record);
    }

    public async Task<bool> ExistsWithCodeAsync(
        string code,
        BusinessTypeId? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = code.Trim();
        var query = _db.BusinessTypes.AsNoTracking()
            .Where(b => b.Code.ToLower() == normalized.ToLower());
        if (excludingId is not null)
        {
            query = query.Where(b => b.Id != excludingId.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithNameAsync(
        string name,
        BusinessTypeId? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToUpperInvariant();
        var query = _db.BusinessTypes.AsNoTracking()
            .Where(b => b.NormalizedName == normalized);
        if (excludingId is not null)
        {
            query = query.Where(b => b.Id != excludingId.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<BusinessType> Items, int TotalCount)> ListAsync(
        BusinessTypeStatus? status,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        BusinessTypeListSortBy sortBy = BusinessTypeListSortBy.SortOrder,
        bool sortDescending = false)
    {
        var query = _db.BusinessTypes.AsNoTracking().AsQueryable();

        if (status is not null)
        {
            var statusText = status.Value.ToString();
            query = query.Where(b => b.Status == statusText);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(b =>
                b.Name.ToLower().Contains(term)
                || b.Code.ToLower().Contains(term));
        }

        query = ApplySort(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(GlobalCatalogEntityMapper.ToDomain).ToList(), totalCount);
    }

    private static IQueryable<BusinessTypeRecord> ApplySort(
        IQueryable<BusinessTypeRecord> query,
        BusinessTypeListSortBy sortBy,
        bool sortDescending) =>
        sortBy switch
        {
            BusinessTypeListSortBy.Name => sortDescending
                ? query.OrderByDescending(b => b.Name).ThenBy(b => b.Id)
                : query.OrderBy(b => b.Name).ThenBy(b => b.Id),
            BusinessTypeListSortBy.Code => sortDescending
                ? query.OrderByDescending(b => b.Code).ThenBy(b => b.Id)
                : query.OrderBy(b => b.Code).ThenBy(b => b.Id),
            BusinessTypeListSortBy.Status => sortDescending
                ? query.OrderByDescending(b => b.Status).ThenBy(b => b.Id)
                : query.OrderBy(b => b.Status).ThenBy(b => b.Id),
            BusinessTypeListSortBy.UpdatedAtUtc => sortDescending
                ? query.OrderByDescending(b => b.UpdatedAtUtc).ThenBy(b => b.Id)
                : query.OrderBy(b => b.UpdatedAtUtc).ThenBy(b => b.Id),
            BusinessTypeListSortBy.CreatedAtUtc => sortDescending
                ? query.OrderByDescending(b => b.CreatedAtUtc).ThenBy(b => b.Id)
                : query.OrderBy(b => b.CreatedAtUtc).ThenBy(b => b.Id),
            _ => sortDescending
                ? query.OrderByDescending(b => b.SortOrder).ThenByDescending(b => b.Name).ThenBy(b => b.Id)
                : query.OrderBy(b => b.SortOrder).ThenBy(b => b.Name).ThenBy(b => b.Id)
        };

    public async Task<IReadOnlyList<BusinessType>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var idArray = ids.Distinct().ToArray();
        var records = await _db.BusinessTypes
            .AsNoTracking()
            .Where(b => idArray.Contains(b.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(GlobalCatalogEntityMapper.ToDomain).ToList();
    }

    public async Task<bool> IsReferencedAsync(
        BusinessTypeId id,
        CancellationToken cancellationToken = default)
    {
        var usedByCategory = await _db.GlobalCategoryBusinessTypes
            .AsNoTracking()
            .AnyAsync(b => b.BusinessTypeId == id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (usedByCategory)
        {
            return true;
        }

        var usedByProduct = await _db.GlobalProductBusinessTypes
            .AsNoTracking()
            .AnyAsync(b => b.BusinessTypeId == id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (usedByProduct)
        {
            return true;
        }

        return await _db.CatalogTemplates
            .AsNoTracking()
            .AnyAsync(t => t.PrimaryBusinessTypeId == id.Value, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task AddAsync(BusinessType businessType, CancellationToken cancellationToken = default)
    {
        _db.BusinessTypes.Add(GlobalCatalogEntityMapper.ToRecord(businessType));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(BusinessType businessType, CancellationToken cancellationToken = default)
    {
        var record = await _db.BusinessTypes
            .FirstOrDefaultAsync(b => b.Id == businessType.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.BusinessTypeNotFound,
                "Business type was not found.");
        }

        GlobalCatalogEntityMapper.ApplyToRecord(businessType, record);
    }
}

internal sealed class GlobalCategoryRepository : IGlobalCategoryRepository
{
    private readonly PlatformDbContext _db;

    public GlobalCategoryRepository(PlatformDbContext db) => _db = db;

    public async Task<GlobalCategory?> GetByIdAsync(
        GlobalCategoryId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.GlobalCategories
            .AsNoTracking()
            .Include(c => c.BusinessTypes)
            .FirstOrDefaultAsync(c => c.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : GlobalCatalogEntityMapper.ToDomain(record);
    }

    public async Task<bool> ExistsWithNameUnderParentAsync(
        string name,
        GlobalCategoryId? parentId,
        GlobalCategoryId? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = name.Trim().ToUpperInvariant();
        var query = _db.GlobalCategories.AsNoTracking()
            .Where(c => c.NormalizedName == normalized);

        query = parentId is null
            ? query.Where(c => c.ParentId == null)
            : query.Where(c => c.ParentId == parentId.Value);

        if (excludingId is not null)
        {
            query = query.Where(c => c.Id != excludingId.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<GlobalCategory> Items, int TotalCount)> ListAsync(
        GlobalCategoryStatus? status,
        GlobalCategoryId? parentId,
        Guid? businessTypeId,
        string? businessTypeCode,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        GlobalCategoryListSortBy sortBy = GlobalCategoryListSortBy.SortOrder,
        bool sortDescending = false)
    {
        var query = _db.GlobalCategories.AsNoTracking().Include(c => c.BusinessTypes).AsQueryable();

        if (status is not null)
        {
            var statusText = status.Value.ToString();
            query = query.Where(c => c.Status == statusText);
        }

        if (parentId is not null)
        {
            query = query.Where(c => c.ParentId == parentId.Value);
        }

        query = await ApplyBusinessTypeFilterAsync(query, businessTypeId, businessTypeCode, cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(c => c.Name.ToLower().Contains(term));
        }

        query = ApplyCategorySort(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(GlobalCatalogEntityMapper.ToDomain).ToList(), totalCount);
    }

    private async Task<IQueryable<GlobalCategoryRecord>> ApplyBusinessTypeFilterAsync(
        IQueryable<GlobalCategoryRecord> query,
        Guid? businessTypeId,
        string? businessTypeCode,
        CancellationToken cancellationToken)
    {
        Guid? resolvedId = businessTypeId;
        if (resolvedId is null && !string.IsNullOrWhiteSpace(businessTypeCode))
        {
            var code = businessTypeCode.Trim();
            resolvedId = await _db.BusinessTypes.AsNoTracking()
                .Where(b => b.Code.ToLower() == code.ToLower())
                .Select(b => (Guid?)b.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (resolvedId is null)
            {
                return query.Where(_ => false);
            }
        }

        if (resolvedId is not null)
        {
            var id = resolvedId.Value;
            query = query.Where(c => c.BusinessTypes.Any(b => b.BusinessTypeId == id));
        }

        return query;
    }

    private static IQueryable<GlobalCategoryRecord> ApplyCategorySort(
        IQueryable<GlobalCategoryRecord> query,
        GlobalCategoryListSortBy sortBy,
        bool sortDescending) =>
        sortBy switch
        {
            GlobalCategoryListSortBy.Name => sortDescending
                ? query.OrderByDescending(c => c.Name).ThenBy(c => c.Id)
                : query.OrderBy(c => c.Name).ThenBy(c => c.Id),
            GlobalCategoryListSortBy.Status => sortDescending
                ? query.OrderByDescending(c => c.Status).ThenBy(c => c.Id)
                : query.OrderBy(c => c.Status).ThenBy(c => c.Id),
            GlobalCategoryListSortBy.UpdatedAtUtc => sortDescending
                ? query.OrderByDescending(c => c.UpdatedAtUtc).ThenBy(c => c.Id)
                : query.OrderBy(c => c.UpdatedAtUtc).ThenBy(c => c.Id),
            GlobalCategoryListSortBy.CreatedAtUtc => sortDescending
                ? query.OrderByDescending(c => c.CreatedAtUtc).ThenBy(c => c.Id)
                : query.OrderBy(c => c.CreatedAtUtc).ThenBy(c => c.Id),
            _ => sortDescending
                ? query.OrderByDescending(c => c.SortOrder).ThenByDescending(c => c.Name).ThenBy(c => c.Id)
                : query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ThenBy(c => c.Id)
        };

    public async Task<IReadOnlyList<GlobalCategory>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var idArray = ids.Distinct().ToArray();
        var records = await _db.GlobalCategories
            .AsNoTracking()
            .Include(c => c.BusinessTypes)
            .Where(c => idArray.Contains(c.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(GlobalCatalogEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<GlobalCategory>> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.GlobalCategories
            .AsNoTracking()
            .Include(c => c.BusinessTypes)
            .Where(c => c.NormalizedName == normalizedName)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(GlobalCatalogEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(GlobalCategory category, CancellationToken cancellationToken = default)
    {
        _db.GlobalCategories.Add(GlobalCatalogEntityMapper.ToRecord(category));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(GlobalCategory category, CancellationToken cancellationToken = default)
    {
        var record = await _db.GlobalCategories
            .Include(c => c.BusinessTypes)
            .FirstOrDefaultAsync(c => c.Id == category.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.GlobalCategoryNotFound,
                "Category was not found.");
        }

        GlobalCatalogEntityMapper.ApplyToRecord(category, record);
    }
}

internal sealed class GlobalProductRepository : IGlobalProductRepository
{
    private readonly PlatformDbContext _db;

    public GlobalProductRepository(PlatformDbContext db) => _db = db;

    public async Task<GlobalProduct?> GetByIdAsync(
        GlobalProductId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.GlobalProducts
            .AsNoTracking()
            .Include(p => p.BusinessTypes)
            .FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : GlobalCatalogEntityMapper.ToDomain(record);
    }

    public async Task<bool> ExistsWithBarcodeAsync(
        string barcode,
        GlobalProductId? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.GlobalProducts.AsNoTracking().Where(p => p.Barcode == barcode);
        if (excludingId is not null)
        {
            query = query.Where(p => p.Id != excludingId.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsWithSkuAsync(
        string sku,
        GlobalProductId? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.GlobalProducts.AsNoTracking().Where(p => p.Sku == sku);
        if (excludingId is not null)
        {
            query = query.Where(p => p.Id != excludingId.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<GlobalProduct> Items, int TotalCount)> ListAsync(
        GlobalProductStatus? status,
        GlobalCategoryId? categoryId,
        Guid? businessTypeId,
        string? businessTypeCode,
        string? search,
        string? barcode,
        string? sku,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<Guid>? excludeProductIds = null,
        GlobalProductListSortBy sortBy = GlobalProductListSortBy.Name,
        bool sortDescending = false)
    {
        var query = _db.GlobalProducts.AsNoTracking().Include(p => p.BusinessTypes).AsQueryable();

        if (status is not null)
        {
            var statusText = status.Value.ToString();
            query = query.Where(p => p.Status == statusText);
        }

        if (categoryId is not null)
        {
            query = query.Where(p => p.GlobalCategoryId == categoryId.Value);
        }

        query = await ApplyBusinessTypeFilterAsync(query, businessTypeId, businessTypeCode, cancellationToken)
            .ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            var normalized = GlobalCatalogRules.NormalizeOptionalFilterCode(
                barcode,
                GlobalCatalogRules.BarcodeMaxLength,
                DomainErrorCodes.InvalidGlobalProductBarcode);
            if (normalized is not null)
            {
                query = query.Where(p => p.Barcode == normalized);
            }
        }

        if (!string.IsNullOrWhiteSpace(sku))
        {
            var normalized = GlobalCatalogRules.NormalizeOptionalFilterCode(
                sku,
                GlobalCatalogRules.SkuMaxLength,
                DomainErrorCodes.InvalidGlobalProductSku);
            if (normalized is not null)
            {
                query = query.Where(p => p.Sku == normalized);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term)
                || (p.Sku != null && p.Sku.ToLower().Contains(term))
                || (p.Barcode != null && p.Barcode.ToLower().Contains(term))
                || (p.Brand != null && p.Brand.ToLower().Contains(term)));
        }

        if (excludeProductIds is { Count: > 0 })
        {
            var excluded = excludeProductIds.ToArray();
            query = query.Where(p => !excluded.Contains(p.Id));
        }

        query = ApplyProductSort(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(GlobalCatalogEntityMapper.ToDomain).ToList(), totalCount);
    }

    private async Task<IQueryable<GlobalProductRecord>> ApplyBusinessTypeFilterAsync(
        IQueryable<GlobalProductRecord> query,
        Guid? businessTypeId,
        string? businessTypeCode,
        CancellationToken cancellationToken)
    {
        Guid? resolvedId = businessTypeId;
        if (resolvedId is null && !string.IsNullOrWhiteSpace(businessTypeCode))
        {
            var code = businessTypeCode.Trim();
            resolvedId = await _db.BusinessTypes.AsNoTracking()
                .Where(b => b.Code.ToLower() == code.ToLower())
                .Select(b => (Guid?)b.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (resolvedId is null)
            {
                return query.Where(_ => false);
            }
        }

        if (resolvedId is not null)
        {
            var id = resolvedId.Value;
            query = query.Where(p => p.BusinessTypes.Any(b => b.BusinessTypeId == id));
        }

        return query;
    }

    private IQueryable<GlobalProductRecord> ApplyProductSort(
        IQueryable<GlobalProductRecord> query,
        GlobalProductListSortBy sortBy,
        bool sortDescending)
    {
        if (sortBy == GlobalProductListSortBy.Category)
        {
            var joined = query
                .GroupJoin(
                    _db.GlobalCategories,
                    p => p.GlobalCategoryId,
                    c => c.Id,
                    (p, cats) => new { Product = p, CategoryName = cats.Select(c => c.Name).FirstOrDefault() });

            return sortDescending
                ? joined.OrderByDescending(x => x.CategoryName ?? string.Empty)
                    .ThenBy(x => x.Product.Id)
                    .Select(x => x.Product)
                : joined.OrderBy(x => x.CategoryName ?? string.Empty)
                    .ThenBy(x => x.Product.Id)
                    .Select(x => x.Product);
        }

        return sortBy switch
        {
            GlobalProductListSortBy.CostPrice => sortDescending
                ? query.OrderByDescending(p => p.CostPrice ?? decimal.MinValue).ThenBy(p => p.Id)
                : query.OrderBy(p => p.CostPrice ?? decimal.MaxValue).ThenBy(p => p.Id),
            GlobalProductListSortBy.SellingPrice => sortDescending
                ? query.OrderByDescending(p => p.SellingPrice ?? decimal.MinValue).ThenBy(p => p.Id)
                : query.OrderBy(p => p.SellingPrice ?? decimal.MaxValue).ThenBy(p => p.Id),
            GlobalProductListSortBy.Sku => sortDescending
                ? query.OrderByDescending(p => p.Sku ?? string.Empty).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Sku ?? string.Empty).ThenBy(p => p.Id),
            GlobalProductListSortBy.Barcode => sortDescending
                ? query.OrderByDescending(p => p.Barcode ?? string.Empty).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Barcode ?? string.Empty).ThenBy(p => p.Id),
            GlobalProductListSortBy.Brand => sortDescending
                ? query.OrderByDescending(p => p.Brand ?? string.Empty).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Brand ?? string.Empty).ThenBy(p => p.Id),
            GlobalProductListSortBy.Unit => sortDescending
                ? query.OrderByDescending(p => p.Unit).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Unit).ThenBy(p => p.Id),
            GlobalProductListSortBy.Status => sortDescending
                ? query.OrderByDescending(p => p.Status).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Status).ThenBy(p => p.Id),
            GlobalProductListSortBy.UpdatedAtUtc => sortDescending
                ? query.OrderByDescending(p => p.UpdatedAtUtc).ThenBy(p => p.Id)
                : query.OrderBy(p => p.UpdatedAtUtc).ThenBy(p => p.Id),
            GlobalProductListSortBy.CreatedAtUtc => sortDescending
                ? query.OrderByDescending(p => p.CreatedAtUtc).ThenBy(p => p.Id)
                : query.OrderBy(p => p.CreatedAtUtc).ThenBy(p => p.Id),
            _ => sortDescending
                ? query.OrderByDescending(p => p.Name).ThenBy(p => p.Id)
                : query.OrderBy(p => p.Name).ThenBy(p => p.Id)
        };
    }

    public async Task<IReadOnlyList<GlobalProduct>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var idArray = ids.Distinct().ToArray();
        var records = await _db.GlobalProducts
            .AsNoTracking()
            .Include(p => p.BusinessTypes)
            .Where(p => idArray.Contains(p.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(GlobalCatalogEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(GlobalProduct product, CancellationToken cancellationToken = default)
    {
        _db.GlobalProducts.Add(GlobalCatalogEntityMapper.ToRecord(product));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(GlobalProduct product, CancellationToken cancellationToken = default)
    {
        var record = await _db.GlobalProducts
            .Include(p => p.BusinessTypes)
            .FirstOrDefaultAsync(p => p.Id == product.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.GlobalProductNotFound,
                "Product was not found.");
        }

        GlobalCatalogEntityMapper.ApplyToRecord(product, record);
    }
}

internal sealed class CatalogTemplateRepository : ICatalogTemplateRepository
{
    private readonly PlatformDbContext _db;

    public CatalogTemplateRepository(PlatformDbContext db) => _db = db;

    public async Task<CatalogTemplate?> GetByIdAsync(
        CatalogTemplateId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogTemplates
            .AsNoTracking()
            .Include(t => t.Products)
            .FirstOrDefaultAsync(t => t.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : GlobalCatalogEntityMapper.ToDomain(record);
    }

    public async Task<bool> ExistsWithSlugAsync(
        string slug,
        CatalogTemplateId? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CatalogTemplates.AsNoTracking().Where(t => t.Slug == slug);
        if (excludingId is not null)
        {
            query = query.Where(t => t.Id != excludingId.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<CatalogTemplate> Items, int TotalCount)> ListAsync(
        CatalogTemplateStatus? status,
        Guid? primaryBusinessTypeId,
        string? primaryBusinessTypeCode,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        CatalogTemplateListSortBy sortBy = CatalogTemplateListSortBy.Name,
        bool sortDescending = false)
    {
        var query = _db.CatalogTemplates.AsNoTracking().Include(t => t.Products).AsQueryable();

        if (status is not null)
        {
            var statusText = status.Value.ToString();
            query = query.Where(t => t.Status == statusText);
        }

        Guid? resolvedId = primaryBusinessTypeId;
        if (resolvedId is null && !string.IsNullOrWhiteSpace(primaryBusinessTypeCode))
        {
            var code = primaryBusinessTypeCode.Trim();
            resolvedId = await _db.BusinessTypes.AsNoTracking()
                .Where(b => b.Code.ToLower() == code.ToLower())
                .Select(b => (Guid?)b.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (resolvedId is null)
            {
                query = query.Where(_ => false);
            }
        }

        if (resolvedId is not null)
        {
            var id = resolvedId.Value;
            query = query.Where(t => t.PrimaryBusinessTypeId == id);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(t =>
                t.Name.ToLower().Contains(term)
                || t.Slug.ToLower().Contains(term));
        }

        query = ApplyTemplateSort(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(GlobalCatalogEntityMapper.ToDomain).ToList(), totalCount);
    }

    private static IQueryable<CatalogTemplateRecord> ApplyTemplateSort(
        IQueryable<CatalogTemplateRecord> query,
        CatalogTemplateListSortBy sortBy,
        bool sortDescending) =>
        sortBy switch
        {
            CatalogTemplateListSortBy.Slug => sortDescending
                ? query.OrderByDescending(t => t.Slug).ThenBy(t => t.Id)
                : query.OrderBy(t => t.Slug).ThenBy(t => t.Id),
            CatalogTemplateListSortBy.Status => sortDescending
                ? query.OrderByDescending(t => t.Status).ThenBy(t => t.Id)
                : query.OrderBy(t => t.Status).ThenBy(t => t.Id),
            CatalogTemplateListSortBy.PrimaryBusinessType => sortDescending
                ? query.OrderByDescending(t => t.PrimaryBusinessTypeId).ThenBy(t => t.Id)
                : query.OrderBy(t => t.PrimaryBusinessTypeId).ThenBy(t => t.Id),
            CatalogTemplateListSortBy.UpdatedAtUtc => sortDescending
                ? query.OrderByDescending(t => t.UpdatedAtUtc).ThenBy(t => t.Id)
                : query.OrderBy(t => t.UpdatedAtUtc).ThenBy(t => t.Id),
            CatalogTemplateListSortBy.CreatedAtUtc => sortDescending
                ? query.OrderByDescending(t => t.CreatedAtUtc).ThenBy(t => t.Id)
                : query.OrderBy(t => t.CreatedAtUtc).ThenBy(t => t.Id),
            CatalogTemplateListSortBy.ProductCount => sortDescending
                ? query.OrderByDescending(t => t.Products.Count).ThenBy(t => t.Id)
                : query.OrderBy(t => t.Products.Count).ThenBy(t => t.Id),
            _ => sortDescending
                ? query.OrderByDescending(t => t.Name).ThenBy(t => t.Id)
                : query.OrderBy(t => t.Name).ThenBy(t => t.Id)
        };

    public Task AddAsync(CatalogTemplate template, CancellationToken cancellationToken = default)
    {
        _db.CatalogTemplates.Add(GlobalCatalogEntityMapper.ToRecord(template));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(CatalogTemplate template, CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogTemplates
            .Include(t => t.Products)
            .FirstOrDefaultAsync(t => t.Id == template.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CatalogTemplateNotFound,
                "Template was not found.");
        }

        GlobalCatalogEntityMapper.ApplyToRecord(template, record);
        SyncProducts(template, record);
    }

    /// <summary>
    /// Reconciles composition rows against the tracked collection, matching on
    /// <c>GlobalProductId</c> (the unique pair within a template). Rows are added and removed
    /// through the set so their tracked state is explicit: a composition row carries a
    /// domain-assigned key, which EF would otherwise read as an existing row and update.
    /// </summary>
    private void SyncProducts(CatalogTemplate template, CatalogTemplateRecord record)
    {
        var desired = template.Products.ToDictionary(p => p.GlobalProductId.Value);

        foreach (var existing in record.Products.ToList())
        {
            if (desired.Remove(existing.GlobalProductId, out var assigned))
            {
                GlobalCatalogEntityMapper.ApplyToRecord(assigned, existing);
            }
            else
            {
                _db.CatalogTemplateProducts.Remove(existing);
            }
        }

        foreach (var assigned in desired.Values)
        {
            _db.CatalogTemplateProducts.Add(GlobalCatalogEntityMapper.ToRecord(record.Id, assigned));
        }
    }
}

internal sealed class CatalogImportJobRepository : ICatalogImportJobRepository
{
    private readonly PlatformDbContext _db;

    public CatalogImportJobRepository(PlatformDbContext db) => _db = db;

    public async Task<CatalogImportJob?> GetByIdAsync(
        CatalogImportJobId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogImportJobs
            .AsNoTracking()
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : GlobalCatalogEntityMapper.ToDomain(record);
    }

    public async Task<CatalogImportJob?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogImportJobs
            .AsNoTracking()
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.IdempotencyKey == idempotencyKey, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : GlobalCatalogEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<CatalogImportJob> Items, int TotalCount)> ListAsync(
        CatalogImportJobStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CatalogImportJobs.AsNoTracking().AsQueryable();
        if (status is not null)
        {
            var statusText = status.Value.ToString();
            query = query.Where(j => j.Status == statusText);
        }

        query = query.OrderByDescending(j => j.CreatedAtUtc).ThenBy(j => j.Id);
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .Include(j => j.Items)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(GlobalCatalogEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<IReadOnlyList<CatalogImportErrorDto>> ListErrorsAsync(
        CatalogImportJobId id,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var failed = CatalogImportItemStatus.Failed.ToString();
        var skipped = CatalogImportItemStatus.Skipped.ToString();
        var records = await _db.CatalogImportItems
            .AsNoTracking()
            .Where(i => i.CatalogImportJobId == id.Value
                        && (i.Status == failed || i.Status == skipped))
            .OrderBy(i => i.RowNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(r => new CatalogImportErrorDto(
                r.Id,
                r.RowNumber,
                r.Name,
                r.Sku,
                r.Barcode,
                r.Status,
                r.ErrorCode,
                r.ErrorMessage))
            .ToList();
    }

    public async Task<CatalogImportJob?> ClaimNextAsync(
        DateTimeOffset utcNow,
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default)
    {
        var queued = CatalogImportJobStatus.Queued.ToString();
        var processing = CatalogImportJobStatus.Processing.ToString();
        var staleBefore = utcNow - staleAfter;

        var record = await _db.CatalogImportJobs
            .Include(j => j.Items)
            .Where(j => j.Status == queued
                        || (j.Status == processing
                            && (j.LastHeartbeatAtUtc == null || j.LastHeartbeatAtUtc < staleBefore)))
            .OrderBy(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : GlobalCatalogEntityMapper.ToDomain(record);
    }

    public Task AddAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
    {
        _db.CatalogImportJobs.Add(GlobalCatalogEntityMapper.ToRecord(job));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogImportJobs
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == job.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CatalogImportJobNotFound,
                "Import job was not found.");
        }

        GlobalCatalogEntityMapper.ApplyToRecord(job, record);
    }
}
