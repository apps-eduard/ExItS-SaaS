using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlanRepository : IPlanRepository
{
    private readonly PlatformDbContext _db;

    public PlanRepository(PlatformDbContext db) => _db = db;

    public async Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<Plan?> GetByProductAndCodeAsync(
        ProductCode productCode,
        PlanCode planCode,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.ProductCode == productCode.Value && p.Code == planCode.Value,
                cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<Plan>> ListByProductAsync(
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.Plans
            .AsNoTracking()
            .Where(p => p.ProductCode == productCode.Value)
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<Plan> Items, int TotalCount)> ListAsync(
        ProductCode? productCode,
        PlanStatus? status,
        string? search,
        CatalogListSortBy sortBy,
        bool sortDescending,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Plans.AsNoTracking();
        if (productCode is not null)
        {
            query = query.Where(p => p.ProductCode == productCode.Value);
        }

        if (status is not null)
        {
            query = query.Where(p => p.Status == status.Value.ToString());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Code.ToLower().Contains(term)
                || p.DisplayName.ToLower().Contains(term)
                || p.ProductCode.ToLower().Contains(term));
        }

        query = (sortBy, sortDescending) switch
        {
            (CatalogListSortBy.DisplayName, false) => query.OrderBy(p => p.DisplayName).ThenBy(p => p.Code),
            (CatalogListSortBy.DisplayName, true) => query.OrderByDescending(p => p.DisplayName).ThenBy(p => p.Code),
            (CatalogListSortBy.Status, false) => query.OrderBy(p => p.Status).ThenBy(p => p.Code),
            (CatalogListSortBy.Status, true) => query.OrderByDescending(p => p.Status).ThenBy(p => p.Code),
            (CatalogListSortBy.ProductCode, false) => query.OrderBy(p => p.ProductCode).ThenBy(p => p.Code),
            (CatalogListSortBy.ProductCode, true) => query.OrderByDescending(p => p.ProductCode).ThenBy(p => p.Code),
            (CatalogListSortBy.ProductDisplayName, false) =>
                query.OrderBy(p => _db.Products.Where(pr => pr.Code == p.ProductCode).Select(pr => pr.DisplayName).FirstOrDefault())
                    .ThenBy(p => p.Code),
            (CatalogListSortBy.ProductDisplayName, true) =>
                query.OrderByDescending(p => _db.Products.Where(pr => pr.Code == p.ProductCode).Select(pr => pr.DisplayName).FirstOrDefault())
                    .ThenBy(p => p.Code),
            (CatalogListSortBy.SortOrder, false) => query.OrderBy(p => p.SortOrder).ThenBy(p => p.Code),
            (CatalogListSortBy.SortOrder, true) => query.OrderByDescending(p => p.SortOrder).ThenBy(p => p.Code),
            (CatalogListSortBy.CreatedAtUtc, false) => query.OrderBy(p => p.CreatedAtUtc).ThenBy(p => p.Code),
            (CatalogListSortBy.CreatedAtUtc, true) => query.OrderByDescending(p => p.CreatedAtUtc).ThenBy(p => p.Code),
            (CatalogListSortBy.UpdatedAtUtc, false) => query.OrderBy(p => p.UpdatedAtUtc).ThenBy(p => p.Code),
            (CatalogListSortBy.UpdatedAtUtc, true) => query.OrderByDescending(p => p.UpdatedAtUtc).ThenBy(p => p.Code),
            (_, true) => query.OrderByDescending(p => p.Code).ThenBy(p => p.ProductCode),
            _ => query.OrderBy(p => p.Code).ThenBy(p => p.ProductCode)
        };

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CatalogEntityMapper.ToDomain).ToList(), totalCount);
    }

    public Task AddAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        _db.Plans.Add(CatalogEntityMapper.ToRecord(plan));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default)
    {
        var record = await _db.Plans
            .FirstOrDefaultAsync(p => p.Id == plan.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return;
        }

        CatalogEntityMapper.ApplyToRecord(plan, record);
    }

    public async Task<PlanVersion?> GetVersionByIdAsync(PlanVersionId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlanVersions
            .AsNoTracking()
            .Include(v => v.FeatureGrants)
            .FirstOrDefaultAsync(v => v.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<PlanVersion?> GetVersionByPlanAndNumberAsync(
        PlanId planId,
        int versionNumber,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlanVersions
            .AsNoTracking()
            .Include(v => v.FeatureGrants)
            .FirstOrDefaultAsync(
                v => v.PlanId == planId.Value && v.VersionNumber == versionNumber,
                cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<PlanVersion>> ListVersionsAsync(
        PlanId planId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.PlanVersions
            .AsNoTracking()
            .Include(v => v.FeatureGrants)
            .Where(v => v.PlanId == planId.Value)
            .OrderBy(v => v.VersionNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public async Task<PlanVersion?> GetLatestPublishedVersionAsync(
        PlanId planId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlanVersions
            .AsNoTracking()
            .Include(v => v.FeatureGrants)
            .Where(v => v.PlanId == planId.Value && v.Status == PlanVersionStatus.Published.ToString())
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<int> GetMaxVersionNumberAsync(PlanId planId, CancellationToken cancellationToken = default)
    {
        var max = await _db.PlanVersions
            .Where(v => v.PlanId == planId.Value)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(cancellationToken)
            .ConfigureAwait(false);

        return max ?? 0;
    }

    public Task AddVersionAsync(PlanVersion version, CancellationToken cancellationToken = default)
    {
        _db.PlanVersions.Add(CatalogEntityMapper.ToRecord(version));
        return Task.CompletedTask;
    }

    public async Task UpdateVersionAsync(PlanVersion version, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlanVersions
            .Include(v => v.FeatureGrants)
            .FirstOrDefaultAsync(v => v.Id == version.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return;
        }

        CatalogEntityMapper.ApplyToRecord(version, record);
    }
}
