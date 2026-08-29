using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

public sealed class ProductionRunRepository : IProductionRunRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public ProductionRunRepository(PosDbContext db) => _db = db;

    public async Task<ProductionRun?> GetByIdAsync(
        PosOrganizationId organizationId,
        ProductionRunId productionRunId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.ProductionRuns.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == productionRunId.Value && r.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var materials = await _db.ProductionRunMaterials.AsNoTracking()
            .Where(m => m.ProductionRunId == record.Id && m.OrganizationId == organizationId.Value)
            .OrderBy(m => m.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ProductionEntityMapper.ToDomain(record, materials);
    }

    public async Task<ProductionRun?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey.Trim();
        var record = await _db.ProductionRuns.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.OrganizationId == organizationId.Value && r.IdempotencyKey == key,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var materials = await _db.ProductionRunMaterials.AsNoTracking()
            .Where(m => m.ProductionRunId == record.Id && m.OrganizationId == organizationId.Value)
            .OrderBy(m => m.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return ProductionEntityMapper.ToDomain(record, materials);
    }

    public async Task<(IReadOnlyList<ProductionRun> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ProductionRunFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ProductionRuns.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value);

        if (filter.FromProducedAtUtc is DateTimeOffset from)
        {
            query = query.Where(r => r.ProducedAtUtc >= from);
        }

        if (filter.ToProducedAtUtc is DateTimeOffset to)
        {
            query = query.Where(r => r.ProducedAtUtc <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.Status)
            && ProductionRunStatuses.TryParse(filter.Status, out var status))
        {
            var code = ProductionRunStatuses.ToCode(status);
            query = query.Where(r => r.Status == code);
        }

        if (filter.BranchId is Guid branchId)
        {
            query = query.Where(r => r.BranchId == branchId);
        }

        if (filter.OutputProductId is Guid outputId)
        {
            query = query.Where(r => r.OutputProductId == outputId);
        }

        if (filter.ProductionDefinitionId is Guid definitionId)
        {
            query = query.Where(r => r.ProductionDefinitionId == definitionId);
        }

        if (!string.IsNullOrWhiteSpace(filter.ReferenceNumber))
        {
            var reference = filter.ReferenceNumber.Trim().ToLowerInvariant();
            query = query.Where(r =>
                r.ReferenceNumber != null && r.ReferenceNumber.ToLower().Contains(reference));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(r => r.ProducedAtUtc)
            .ThenByDescending(r => r.ProductionNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return ([], total);
        }

        var runIds = records.Select(r => r.Id).ToList();
        var materialsByRun = await LoadMaterialsAsync(runIds, organizationId, cancellationToken)
            .ConfigureAwait(false);
        var items = records
            .Select(r => ProductionEntityMapper.ToDomain(
                r,
                materialsByRun.TryGetValue(r.Id, out var materials) ? materials : []))
            .ToList();
        return (items, total);
    }

    public async Task AddAsync(ProductionRun productionRun, CancellationToken cancellationToken = default)
    {
        _db.ProductionRuns.Add(ProductionEntityMapper.ToRecord(productionRun));
        foreach (var material in productionRun.Materials)
        {
            _db.ProductionRunMaterials.Add(ProductionEntityMapper.ToRecord(material));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task UpdateAsync(ProductionRun productionRun, CancellationToken cancellationToken = default)
    {
        var record = await _db.ProductionRuns
            .FirstOrDefaultAsync(
                r => r.Id == productionRun.Id.Value && r.OrganizationId == productionRun.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Production run was not found for update.");

        ProductionEntityMapper.Apply(productionRun, record);

        var existing = await _db.ProductionRunMaterials
            .Where(m => m.ProductionRunId == productionRun.Id.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.ProductionRunMaterials.RemoveRange(existing);
        foreach (var material in productionRun.Materials)
        {
            _db.ProductionRunMaterials.Add(ProductionEntityMapper.ToRecord(material));
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

        var sequence = await _db.ProductionRunNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);
        long value;
        if (sequence is null)
        {
            _db.ProductionRunNumberSequences.Add(new ProductionRunNumberSequenceRecord
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

        return ProductionNumbers.Format(businessDateUtc, value);
    }

    private static long SequenceLockKey(PosOrganizationId organizationId, DateOnly businessDateUtc)
    {
        Span<byte> bytes = stackalloc byte[21];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..20], businessDateUtc.DayNumber);
        bytes[20] = 23; // distinct from stock_use (22)

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

    private async Task<Dictionary<Guid, List<ProductionRunMaterialRecord>>> LoadMaterialsAsync(
        IReadOnlyCollection<Guid> runIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.ProductionRunMaterials.AsNoTracking()
            .Where(m => m.OrganizationId == organizationId.Value && runIds.Contains(m.ProductionRunId))
            .OrderBy(m => m.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(m => m.ProductionRunId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}
