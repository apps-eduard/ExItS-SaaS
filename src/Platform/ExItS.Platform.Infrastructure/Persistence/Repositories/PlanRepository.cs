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
