using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class BranchDeliveryServiceAreaRepository(PlatformDbContext db) : IBranchDeliveryServiceAreaRepository
{
    public async Task<BranchDeliveryServiceArea?> GetByIdAsync(
        BranchDeliveryServiceAreaId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.BranchDeliveryServiceAreas
            .FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : OrganizationBranchDeviceEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<BranchDeliveryServiceArea>> ListByBranchAsync(
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.BranchDeliveryServiceAreas.AsNoTracking()
            .Where(x => x.BranchId == branchId.Value)
            .OrderBy(x => x.NormalizedCityMunicipalityName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(OrganizationBranchDeviceEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<BranchDeliveryServiceArea>> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await db.BranchDeliveryServiceAreas.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value)
            .OrderBy(x => x.BranchId)
            .ThenBy(x => x.NormalizedCityMunicipalityName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(OrganizationBranchDeviceEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountActiveByBranchIdsAsync(
        PlatformOrganizationId organizationId,
        IReadOnlyCollection<OrganizationBranchId> branchIds,
        CancellationToken cancellationToken = default)
    {
        if (branchIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var ids = branchIds.Select(b => b.Value).ToList();
        var rows = await db.BranchDeliveryServiceAreas.AsNoTracking()
            .Where(x => x.OrganizationId == organizationId.Value
                        && x.IsActive
                        && ids.Contains(x.BranchId))
            .GroupBy(x => x.BranchId)
            .Select(g => new { BranchId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.ToDictionary(r => r.BranchId, r => r.Count);
    }

    public Task AddAsync(BranchDeliveryServiceArea area, CancellationToken cancellationToken = default)
    {
        db.BranchDeliveryServiceAreas.Add(OrganizationBranchDeviceEntityMapper.ToRecord(area));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(BranchDeliveryServiceArea area, CancellationToken cancellationToken = default)
    {
        var record = await db.BranchDeliveryServiceAreas
            .FirstOrDefaultAsync(x => x.Id == area.Id.Value, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Branch delivery service area was not found.");
        OrganizationBranchDeviceEntityMapper.ApplyToRecord(area, record);
    }
}
