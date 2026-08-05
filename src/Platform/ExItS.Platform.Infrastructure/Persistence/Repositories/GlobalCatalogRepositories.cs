using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Infrastructure.Persistence.GlobalCatalog;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

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
        BusinessType? businessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
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

        if (businessType is not null)
        {
            var typeText = businessType.Value.ToString();
            query = query.Where(c => c.BusinessTypes.Any(b => b.BusinessType == typeText));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(c => c.Name.ToLower().Contains(term));
        }

        query = query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(GlobalCatalogEntityMapper.ToDomain).ToList(), totalCount);
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
        BusinessType? businessType,
        string? search,
        string? barcode,
        string? sku,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
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

        if (businessType is not null)
        {
            var typeText = businessType.Value.ToString();
            query = query.Where(p => p.BusinessTypes.Any(b => b.BusinessType == typeText));
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            var normalized = GlobalCatalogRules.NormalizeBarcode(barcode);
            query = query.Where(p => p.Barcode == normalized);
        }

        if (!string.IsNullOrWhiteSpace(sku))
        {
            var normalized = GlobalCatalogRules.NormalizeSku(sku);
            query = query.Where(p => p.Sku == normalized);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term)
                || (p.Sku != null && p.Sku.ToLower().Contains(term))
                || (p.Barcode != null && p.Barcode.ToLower().Contains(term)));
        }

        query = query.OrderBy(p => p.Name).ThenBy(p => p.Id);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(GlobalCatalogEntityMapper.ToDomain).ToList(), totalCount);
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
        BusinessType? primaryBusinessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CatalogTemplates.AsNoTracking().Include(t => t.Products).AsQueryable();

        if (status is not null)
        {
            var statusText = status.Value.ToString();
            query = query.Where(t => t.Status == statusText);
        }

        if (primaryBusinessType is not null)
        {
            var typeText = primaryBusinessType.Value.ToString();
            query = query.Where(t => t.PrimaryBusinessType == typeText);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(t =>
                t.Name.ToLower().Contains(term)
                || t.Slug.ToLower().Contains(term));
        }

        query = query.OrderBy(t => t.Name).ThenBy(t => t.Id);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query.Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
        return (records.Select(GlobalCatalogEntityMapper.ToDomain).ToList(), totalCount);
    }

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
