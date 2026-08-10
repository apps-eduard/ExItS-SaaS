using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class OrganizationBranchRepository : IOrganizationBranchRepository
{
    private readonly PlatformDbContext _db;
    public OrganizationBranchRepository(PlatformDbContext db) => _db = db;
    public async Task<OrganizationBranch?> GetByIdAsync(OrganizationBranchId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationBranches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken).ConfigureAwait(false);
        return record is null ? null : OrganizationBranchDeviceEntityMapper.ToDomain(record);
    }
    public async Task<IReadOnlyList<OrganizationBranch>> ListByOrganizationAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
        (await _db.OrganizationBranches.AsNoTracking().Where(x => x.OrganizationId == organizationId.Value).OrderBy(x => x.Code).ToListAsync(cancellationToken).ConfigureAwait(false))
            .Select(OrganizationBranchDeviceEntityMapper.ToDomain).ToList();
    public Task<int> CountActiveAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
        _db.OrganizationBranches.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId.Value && x.Status == nameof(OrganizationBranchStatus.Active), cancellationToken);
    public async Task<OrganizationBranch?> GetPrimaryAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationBranches.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == organizationId.Value && x.IsPrimary, cancellationToken).ConfigureAwait(false);
        return record is null ? null : OrganizationBranchDeviceEntityMapper.ToDomain(record);
    }
    public Task AddAsync(OrganizationBranch branch, CancellationToken cancellationToken = default) { _db.OrganizationBranches.Add(OrganizationBranchDeviceEntityMapper.ToRecord(branch)); return Task.CompletedTask; }
    public async Task UpdateAsync(OrganizationBranch branch, CancellationToken cancellationToken = default)
    {
        var record = await _db.OrganizationBranches.FirstOrDefaultAsync(x => x.Id == branch.Id.Value, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Organization branch was not found.");
        OrganizationBranchDeviceEntityMapper.ApplyToRecord(branch, record);
    }
}
