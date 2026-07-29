using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class FeatureOverrideRepository : IFeatureOverrideRepository
{
    private readonly PlatformDbContext _db;

    public FeatureOverrideRepository(PlatformDbContext db) => _db = db;

    public async Task<FeatureOverride?> GetByIdAsync(
        FeatureOverrideId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.FeatureOverrides
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : EntitlementEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<FeatureOverride>> ListActiveForOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.FeatureOverrides
            .AsNoTracking()
            .Where(o => o.OrganizationId == organizationId.Value
                        && o.ProductCode == productCode.Value
                        && o.Status == nameof(FeatureOverrideStatus.Active)
                        && o.EffectiveFromUtc <= utcNow
                        && (o.ExpiresAtUtc == null || o.ExpiresAtUtc > utcNow))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(EntitlementEntityMapper.ToDomain).ToList();
    }

    public async Task<(IReadOnlyList<FeatureOverride> Items, int TotalCount)> ListByOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        FeatureOverrideStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.FeatureOverrides
            .AsNoTracking()
            .Where(o => o.OrganizationId == organizationId.Value && o.ProductCode == productCode.Value);

        if (status is not null)
        {
            query = query.Where(o => o.Status == status.Value.ToString());
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(EntitlementEntityMapper.ToDomain).ToList(), totalCount);
    }

    public Task AddAsync(FeatureOverride featureOverride, CancellationToken cancellationToken = default)
    {
        _db.FeatureOverrides.Add(EntitlementEntityMapper.ToRecord(featureOverride));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(FeatureOverride featureOverride, CancellationToken cancellationToken = default)
    {
        var record = await _db.FeatureOverrides
            .FirstOrDefaultAsync(o => o.Id == featureOverride.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.FeatureOverrideNotFound,
                "Feature override was not found.");
        }

        EntitlementEntityMapper.ApplyToRecord(featureOverride, record);
    }
}
