using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class StockCountRepository : IStockCountRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public StockCountRepository(PosDbContext db) => _db = db;

    public async Task<StockCount?> GetByIdAsync(
        PosOrganizationId organizationId,
        StockCountId stockCountId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.StockCounts.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == stockCountId.Value && c.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await LoadLinesAsync([record.Id], organizationId, cancellationToken).ConfigureAwait(false);
        return StockCountEntityMapper.ToDomain(record, lines.TryGetValue(record.Id, out var found) ? found : []);
    }

    public async Task<(IReadOnlyList<StockCount> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        StockCountFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.StockCounts.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status)
            && Enum.TryParse<StockCountStatus>(filter.Status.Trim(), ignoreCase: true, out var status))
        {
            var statusName = status.ToString();
            query = query.Where(c => c.Status == statusName);
        }

        if (!string.IsNullOrWhiteSpace(filter.CountNumber))
        {
            var term = filter.CountNumber.Trim().ToUpperInvariant();
            query = query.Where(c => c.CountNumber != null && c.CountNumber.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ThenByDescending(c => c.CreatedAtUtc)
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
        var items = records
            .Select(r => StockCountEntityMapper.ToDomain(r, lines.TryGetValue(r.Id, out var found) ? found : []))
            .ToList();
        return (items, total);
    }

    public Task AddAsync(StockCount stockCount, CancellationToken cancellationToken = default)
    {
        _db.StockCounts.Add(StockCountEntityMapper.ToRecord(stockCount));
        foreach (var line in stockCount.Lines)
        {
            _db.StockCountLines.Add(StockCountEntityMapper.ToRecord(line));
        }

        return Task.CompletedTask;
    }

    public async Task UpdateAsync(StockCount stockCount, CancellationToken cancellationToken = default)
    {
        var record = await _db.StockCounts
            .FirstOrDefaultAsync(
                c => c.Id == stockCount.Id.Value && c.OrganizationId == stockCount.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.StockCountNotFound,
                "Stock count was not found.");
        }

        StockCountEntityMapper.ApplyToRecord(stockCount, record);
        var existingLines = await _db.StockCountLines
            .Where(l => l.StockCountId == stockCount.Id.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.StockCountLines.RemoveRange(existingLines);
        foreach (var line in stockCount.Lines)
        {
            _db.StockCountLines.Add(StockCountEntityMapper.ToRecord(line));
        }
    }

    public Task<StockCount> StartAsync(
        PosOrganizationId organizationId,
        StockCountId stockCountId,
        DateOnly businessDateUtc,
        Func<string, StockCount> applyStart,
        CancellationToken cancellationToken = default) =>
        ExecuteNumberedMutationAsync(
            organizationId,
            businessDateUtc,
            async (number, ct) =>
            {
                var count = applyStart(number);
                var record = await _db.StockCounts
                    .FirstOrDefaultAsync(
                        c => c.Id == stockCountId.Value && c.OrganizationId == organizationId.Value,
                        ct)
                    .ConfigureAwait(false);
                if (record is null)
                {
                    throw new PersistenceConflictException(
                        ApplicationErrorCodes.StockCountNotFound,
                        "Stock count was not found.");
                }

                StockCountEntityMapper.ApplyToRecord(count, record);
                var existingLines = await _db.StockCountLines
                    .Where(l => l.StockCountId == count.Id.Value)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                _db.StockCountLines.RemoveRange(existingLines);
                foreach (var line in count.Lines)
                {
                    _db.StockCountLines.Add(StockCountEntityMapper.ToRecord(line));
                }

                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return count;
            },
            ApplicationErrorCodes.StockCountNumberConflict,
            "A stock count number was allocated concurrently. Retry the start.",
            cancellationToken);

    public Task<StockCount> CompleteAsync(
        PosOrganizationId organizationId,
        StockCountId stockCountId,
        Func<StockCount, CancellationToken, Task> afterMarkedComplete,
        CancellationToken cancellationToken = default) =>
        ExecuteInTransactionAsync(
            async ct =>
            {
                var record = await _db.StockCounts
                    .FirstOrDefaultAsync(
                        c => c.Id == stockCountId.Value && c.OrganizationId == organizationId.Value,
                        ct)
                    .ConfigureAwait(false);
                if (record is null)
                {
                    throw new PersistenceConflictException(
                        ApplicationErrorCodes.StockCountNotFound,
                        "Stock count was not found.");
                }

                var lines = await _db.StockCountLines
                    .Where(l => l.StockCountId == record.Id && l.OrganizationId == organizationId.Value)
                    .OrderBy(l => l.LineNumber)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                var count = StockCountEntityMapper.ToDomain(record, lines);

                if (count.Status == StockCountStatus.Completed)
                {
                    return count;
                }

                await afterMarkedComplete(count, ct).ConfigureAwait(false);
                StockCountEntityMapper.ApplyToRecord(count, record);
                var existingLines = await _db.StockCountLines
                    .Where(l => l.StockCountId == count.Id.Value)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                _db.StockCountLines.RemoveRange(existingLines);
                foreach (var line in count.Lines)
                {
                    _db.StockCountLines.Add(StockCountEntityMapper.ToRecord(line));
                }

                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return count;
            },
            cancellationToken);

    private async Task<T> ExecuteNumberedMutationAsync<T>(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, CancellationToken, Task<T>> complete,
        string conflictCode,
        string conflictMessage,
        CancellationToken cancellationToken)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await CompleteNumberedAsync(organizationId, businessDateUtc, complete, conflictCode, conflictMessage, cancellationToken)
                .ConfigureAwait(false);
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                var result = await CompleteNumberedAsync(
                        organizationId,
                        businessDateUtc,
                        complete,
                        conflictCode,
                        conflictMessage,
                        cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (DbUpdateException ex) when (IsNumberConflict(ex))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw new PersistenceConflictException(conflictCode, conflictMessage);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task<T> CompleteNumberedAsync<T>(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, CancellationToken, Task<T>> complete,
        string conflictCode,
        string conflictMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var sequence = await ReserveNextSequenceAsync(organizationId, businessDateUtc, cancellationToken)
                .ConfigureAwait(false);
            var number = StockCountNumbers.Format(businessDateUtc, sequence);
            return await complete(number, cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsNumberConflict(ex))
        {
            throw new PersistenceConflictException(conflictCode, conflictMessage);
        }
    }

    private async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await work(cancellationToken).ConfigureAwait(false);
        }

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database
                .BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                var result = await work(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task<long> ReserveNextSequenceAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken)
    {
        await _db.Database
            .ExecuteSqlRawAsync(LockSequenceSql, [SequenceLockKey(organizationId, businessDateUtc)], cancellationToken)
            .ConfigureAwait(false);

        var sequence = await _db.StockCountNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);
        if (sequence is null)
        {
            _db.StockCountNumberSequences.Add(new StockCountNumberSequenceRecord
            {
                OrganizationId = organizationId.Value,
                BusinessDate = businessDateUtc,
                LastValue = 1
            });
            return 1;
        }

        sequence.LastValue += 1;
        return sequence.LastValue;
    }

    private static long SequenceLockKey(PosOrganizationId organizationId, DateOnly businessDateUtc)
    {
        Span<byte> bytes = stackalloc byte[21];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..20], businessDateUtc.DayNumber);
        bytes[20] = 3;

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

    private async Task<Dictionary<Guid, List<StockCountLineRecord>>> LoadLinesAsync(
        IReadOnlyCollection<Guid> countIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.StockCountLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && countIds.Contains(l.StockCountId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(l => l.StockCountId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static bool IsNumberConflict(DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            return false;
        }

        return pg.ConstraintName?.Contains("stock_count", StringComparison.OrdinalIgnoreCase) == true
            || pg.ConstraintName?.Contains("count_number", StringComparison.OrdinalIgnoreCase) == true;
    }
}
