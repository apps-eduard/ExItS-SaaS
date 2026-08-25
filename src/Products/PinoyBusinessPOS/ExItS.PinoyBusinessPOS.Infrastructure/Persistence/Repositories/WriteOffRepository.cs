using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class WriteOffRepository : IWriteOffRepository
{
    private readonly PosDbContext _db;

    public WriteOffRepository(PosDbContext db) => _db = db;

    public async Task<WriteOff?> GetByIdAsync(
        PosOrganizationId organizationId,
        WriteOffId writeOffId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.WriteOffs.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == writeOffId.Value && e.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : WriteOffEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<WriteOff> Items, int TotalCount)> ListByCustomerAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.WriteOffs.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value && e.CustomerId == customerId.Value);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(e => e.RecordedAtUtc)
            .ThenByDescending(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(WriteOffEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<(IReadOnlyList<WriteOff> Items, int TotalCount)> ListCreatedSinceAsync(
        PosOrganizationId organizationId,
        DateTimeOffset? sinceUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.WriteOffs.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value);

        if (sinceUtc is not null)
        {
            var since = sinceUtc.Value.ToUniversalTime();
            query = query.Where(e =>
                e.RecordedAtUtc > since
                || (e.ReversedAtUtc != null && e.ReversedAtUtc > since));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(e => e.RecordedAtUtc)
            .ThenBy(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(WriteOffEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<decimal> SumActiveAmountAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        var active = WriteOffStatus.Active.ToString();
        return await _db.WriteOffs.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value
                        && e.CustomerId == customerId.Value
                        && e.Status == active)
            .SumAsync(e => e.Amount, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var active = WriteOffStatus.Active.ToString();
        var rows = await _db.WriteOffs.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value && e.Status == active)
            .GroupBy(e => e.CustomerId)
            .Select(g => new { CustomerId = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(r => r.CustomerId, r => r.Total);
    }

    public async Task<int> CountActiveAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        var active = WriteOffStatus.Active.ToString();
        return await _db.WriteOffs.AsNoTracking()
            .CountAsync(
                e => e.OrganizationId == organizationId.Value
                     && e.CustomerId == customerId.Value
                     && e.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task AddAsync(WriteOff writeOff, CancellationToken cancellationToken = default)
    {
        _db.WriteOffs.Add(WriteOffEntityMapper.ToRecord(writeOff));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(WriteOff writeOff, CancellationToken cancellationToken = default)
    {
        var record = await _db.WriteOffs
            .FirstOrDefaultAsync(
                e => e.Id == writeOff.Id.Value && e.OrganizationId == writeOff.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.WriteOffNotFound,
                "Write-off was not found.");
        }

        WriteOffEntityMapper.ApplyToRecord(writeOff, record);
    }
}
