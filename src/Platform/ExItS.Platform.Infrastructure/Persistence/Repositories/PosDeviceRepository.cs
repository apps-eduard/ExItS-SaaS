using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PosDeviceRepository : IPosDeviceRepository
{
    private readonly PlatformDbContext _db;
    public PosDeviceRepository(PlatformDbContext db) => _db = db;
    public async Task<PosDevice?> GetByIdAsync(PosDeviceId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.PosDevices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id.Value, cancellationToken).ConfigureAwait(false);
        return record is null ? null : OrganizationBranchDeviceEntityMapper.ToDomain(record);
    }
    public async Task<PosDevice?> GetByInstallationDeviceIdAsync(PlatformOrganizationId organizationId, string installationDeviceId, CancellationToken cancellationToken = default)
    {
        var value = PosDevice.NormalizeInstallationDeviceId(installationDeviceId);
        var record = await _db.PosDevices.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == organizationId.Value && x.InstallationDeviceId == value, cancellationToken).ConfigureAwait(false);
        return record is null ? null : OrganizationBranchDeviceEntityMapper.ToDomain(record);
    }
    public async Task<IReadOnlyList<PosDevice>> ListByOrganizationAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
        (await _db.PosDevices.AsNoTracking().Where(x => x.OrganizationId == organizationId.Value).OrderBy(x => x.FriendlyName).ToListAsync(cancellationToken).ConfigureAwait(false))
            .Select(OrganizationBranchDeviceEntityMapper.ToDomain).ToList();
    public Task<int> CountActiveAsync(PlatformOrganizationId organizationId, CancellationToken cancellationToken = default) =>
        _db.PosDevices.AsNoTracking().CountAsync(x => x.OrganizationId == organizationId.Value && x.Status == nameof(PosDeviceStatus.Active), cancellationToken);
    public Task AddAsync(PosDevice device, CancellationToken cancellationToken = default) { _db.PosDevices.Add(OrganizationBranchDeviceEntityMapper.ToRecord(device)); return Task.CompletedTask; }
    public async Task UpdateAsync(PosDevice device, CancellationToken cancellationToken = default)
    {
        var record = await _db.PosDevices.FirstOrDefaultAsync(x => x.Id == device.Id.Value, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("POS device was not found.");
        OrganizationBranchDeviceEntityMapper.ApplyToRecord(device, record);
    }
}
