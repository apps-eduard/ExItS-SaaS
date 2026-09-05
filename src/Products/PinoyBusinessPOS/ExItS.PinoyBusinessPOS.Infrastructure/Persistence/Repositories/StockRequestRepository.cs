using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class SupplyRouteRepository : ISupplyRouteRepository
{
    private readonly PosDbContext _db;

    public SupplyRouteRepository(PosDbContext db) => _db = db;

    public async Task<SupplyRoute?> GetByIdAsync(
        PosOrganizationId organizationId,
        SupplyRouteId routeId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.SupplyRoutes.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == routeId.Value && r.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : StockRequestEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<SupplyRoute>> ListByDestinationAsync(
        PosOrganizationId organizationId,
        PosBranchId destinationLocationId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.SupplyRoutes.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value && r.DestinationLocationId == destinationLocationId.Value)
            .OrderByDescending(r => r.IsPreferred)
            .ThenBy(r => r.SourceLocationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(StockRequestEntityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SupplyRoute>> ListAllAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.SupplyRoutes.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value)
            .OrderBy(r => r.DestinationLocationId)
            .ThenByDescending(r => r.IsPreferred)
            .ThenBy(r => r.SourceLocationId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(StockRequestEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(SupplyRoute route, CancellationToken cancellationToken = default)
    {
        _db.SupplyRoutes.Add(StockRequestEntityMapper.ToRecord(route));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(SupplyRoute route, CancellationToken cancellationToken = default)
    {
        var record = await _db.SupplyRoutes
            .FirstOrDefaultAsync(
                r => r.Id == route.Id.Value && r.OrganizationId == route.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException("pos.inventory.supply_route.not_found", "Supply route was not found.");
        }

        StockRequestEntityMapper.ApplyToRecord(route, record);
    }
}

internal sealed class StockRequestRepository : IStockRequestRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";
    private readonly PosDbContext _db;

    public StockRequestRepository(PosDbContext db) => _db = db;

    public async Task<StockRequest?> GetByIdAsync(
        PosOrganizationId organizationId,
        StockRequestId stockRequestId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.StockRequests.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == stockRequestId.Value && r.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await LoadLinesAsync([record.Id], organizationId, cancellationToken).ConfigureAwait(false);
        return StockRequestEntityMapper.ToDomain(record, lines.TryGetValue(record.Id, out var found) ? found : []);
    }

    public async Task<(IReadOnlyList<StockRequest> Items, int TotalCount)> ListByDestinationAsync(
        PosOrganizationId organizationId,
        PosBranchId destinationLocationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.StockRequests.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value && r.DestinationLocationId == destinationLocationId.Value);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(r => r.UpdatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (records.Count == 0)
        {
            return ([], total);
        }

        var lines = await LoadLinesAsync(records.Select(r => r.Id).ToList(), organizationId, cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(r => StockRequestEntityMapper.ToDomain(r, lines.TryGetValue(r.Id, out var found) ? found : [])).ToList(), total);
    }

    public async Task<(IReadOnlyList<StockRequest> Items, int TotalCount)> ListBySourceAsync(
        PosOrganizationId organizationId,
        PosBranchId sourceLocationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.StockRequests.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value && r.RequestedSourceLocationId == sourceLocationId.Value);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(r => r.UpdatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (records.Count == 0)
        {
            return ([], total);
        }

        var lines = await LoadLinesAsync(records.Select(r => r.Id).ToList(), organizationId, cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(r => StockRequestEntityMapper.ToDomain(r, lines.TryGetValue(r.Id, out var found) ? found : [])).ToList(), total);
    }

    public Task AddAsync(StockRequest stockRequest, CancellationToken cancellationToken = default)
    {
        _db.StockRequests.Add(StockRequestEntityMapper.ToRecord(stockRequest));
        foreach (var line in stockRequest.Lines)
        {
            _db.StockRequestLines.Add(StockRequestEntityMapper.ToRecord(line, stockRequest.OrganizationId));
        }

        return Task.CompletedTask;
    }

    public async Task UpdateAsync(StockRequest stockRequest, CancellationToken cancellationToken = default)
    {
        var record = await _db.StockRequests
            .FirstOrDefaultAsync(
                r => r.Id == stockRequest.Id.Value && r.OrganizationId == stockRequest.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException("pos.inventory.stock_request.not_found", "Stock request was not found.");
        }

        StockRequestEntityMapper.ApplyToRecord(stockRequest, record);
        var existingLines = await _db.StockRequestLines
            .Where(l => l.StockRequestId == stockRequest.Id.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.StockRequestLines.RemoveRange(existingLines);
        foreach (var line in stockRequest.Lines)
        {
            _db.StockRequestLines.Add(StockRequestEntityMapper.ToRecord(line, stockRequest.OrganizationId));
        }
    }

    public async Task<string> AllocateNextNumberAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken = default)
    {
        await _db.Database
            .ExecuteSqlRawAsync(LockSequenceSql, [SequenceLockKey(organizationId, businessDateUtc)], cancellationToken)
            .ConfigureAwait(false);

        var sequence = await _db.StockRequestNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);
        long value;
        if (sequence is null)
        {
            _db.StockRequestNumberSequences.Add(new StockRequestNumberSequenceRecord
            {
                OrganizationId = organizationId.Value,
                BusinessDate = businessDateUtc,
                LastValue = 1
            });
            value = 1;
        }
        else
        {
            sequence.LastValue += 1;
            value = sequence.LastValue;
        }

        return StockRequestNumbers.Format(businessDateUtc, value);
    }

    private static long SequenceLockKey(PosOrganizationId organizationId, DateOnly businessDateUtc)
    {
        Span<byte> bytes = stackalloc byte[21];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..20], businessDateUtc.DayNumber);
        bytes[20] = 33;

        unchecked
        {
            var hash = 0xcbf29ce484222325UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            return (long)hash;
        }
    }

    private async Task<Dictionary<Guid, List<StockRequestLineRecord>>> LoadLinesAsync(
        IReadOnlyCollection<Guid> stockRequestIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.StockRequestLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && stockRequestIds.Contains(l.StockRequestId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.GroupBy(l => l.StockRequestId).ToDictionary(g => g.Key, g => g.ToList());
    }
}
