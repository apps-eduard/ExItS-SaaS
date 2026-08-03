using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.OperationalSetup;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class OperationalSetupRepository : IPosOperationalSetupRepository
{
    private readonly PosDbContext _db;

    public OperationalSetupRepository(PosDbContext db) => _db = db;

    public async Task<PosOperationalSetup?> GetByOrganizationIdAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.OperationalSetups.AsNoTracking()
            .FirstOrDefaultAsync(r => r.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : OperationalSetupEntityMapper.ToDomain(record);
    }

    public async Task AddAsync(PosOperationalSetup setup, CancellationToken cancellationToken = default)
    {
        _db.OperationalSetups.Add(OperationalSetupEntityMapper.ToRecord(setup));
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task UpdateAsync(PosOperationalSetup setup, CancellationToken cancellationToken = default)
    {
        var record = await _db.OperationalSetups
            .FirstOrDefaultAsync(r => r.OrganizationId == setup.OrganizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.OperationalSetupConcurrencyConflict,
                "Operational setup was not found for update.");
        }

        OperationalSetupEntityMapper.ApplyToRecord(setup, record);
    }
}
