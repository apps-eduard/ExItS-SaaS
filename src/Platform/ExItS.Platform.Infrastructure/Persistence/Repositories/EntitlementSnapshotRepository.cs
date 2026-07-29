using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class EntitlementSnapshotRepository : IEntitlementSnapshotRepository
{
    private readonly PlatformDbContext _db;

    public EntitlementSnapshotRepository(PlatformDbContext db) => _db = db;

    public async Task<EntitlementSnapshot?> GetByIdAsync(
        EntitlementSnapshotId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.EntitlementSnapshots
            .AsNoTracking()
            .Include(s => s.Grants)
            .FirstOrDefaultAsync(s => s.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : EntitlementEntityMapper.ToDomain(record);
    }

    public async Task<EntitlementSnapshot?> GetLatestForOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.EntitlementSnapshots
            .AsNoTracking()
            .Include(s => s.Grants)
            .Where(s => s.OrganizationId == organizationId.Value && s.ProductCode == productCode.Value)
            .OrderByDescending(s => s.SnapshotVersion)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : EntitlementEntityMapper.ToDomain(record);
    }

    public async Task<EntitlementSnapshot?> GetByVersionAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        int snapshotVersion,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.EntitlementSnapshots
            .AsNoTracking()
            .Include(s => s.Grants)
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value
                     && s.ProductCode == productCode.Value
                     && s.SnapshotVersion == snapshotVersion,
                cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : EntitlementEntityMapper.ToDomain(record);
    }

    public async Task<int?> GetLatestSnapshotVersionAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var hasAny = await _db.EntitlementSnapshots
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value && s.ProductCode == productCode.Value)
            .Select(s => (int?)s.SnapshotVersion)
            .OrderByDescending(v => v)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return hasAny;
    }

    public async Task<(IReadOnlyList<EntitlementSnapshot> Items, int TotalCount)> ListHistoryAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.EntitlementSnapshots
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value && s.ProductCode == productCode.Value);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .Include(s => s.Grants)
            .OrderByDescending(s => s.SnapshotVersion)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(EntitlementEntityMapper.ToDomain).ToList(), totalCount);
    }

    public Task AddAsync(EntitlementSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        _db.EntitlementSnapshots.Add(EntitlementEntityMapper.ToRecord(snapshot));
        return Task.CompletedTask;
    }
}
