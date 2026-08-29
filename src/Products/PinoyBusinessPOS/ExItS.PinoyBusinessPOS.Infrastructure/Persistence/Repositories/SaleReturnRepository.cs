using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class SaleReturnRepository : ISaleReturnRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public SaleReturnRepository(PosDbContext db) => _db = db;

    public async Task<SaleReturn?> GetByIdAsync(
        PosOrganizationId organizationId,
        SaleReturnId returnId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.SaleReturns.AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.Id == returnId.Value && r.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await LoadLinesAsync([record.Id], organizationId, cancellationToken).ConfigureAwait(false);
        return SaleReturnEntityMapper.ToDomain(record, lines.TryGetValue(record.Id, out var found) ? found : []);
    }

    public async Task<(IReadOnlyList<SaleReturn> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        SaleReturnFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.SaleReturns.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value);

        if (filter.SaleId is not null)
        {
            query = query.Where(r => r.SaleId == filter.SaleId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ReturnNumber))
        {
            var term = filter.ReturnNumber.Trim().ToUpperInvariant();
            query = query.Where(r => r.ReturnNumber.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .ThenByDescending(r => r.ReturnNumber)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return ([], total);
        }

        var lines = await LoadLinesAsync(
                records.Select(r => r.Id).ToList(),
                organizationId,
                cancellationToken)
            .ConfigureAwait(false);

        var items = records
            .Select(r => SaleReturnEntityMapper.ToDomain(r, lines.TryGetValue(r.Id, out var found) ? found : []))
            .ToList();
        return (items, total);
    }

    public async Task<IReadOnlyList<SaleReturn>> ListBySaleIdAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.SaleReturns.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value && r.SaleId == saleId.Value)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return [];
        }

        var lines = await LoadLinesAsync(
                records.Select(r => r.Id).ToList(),
                organizationId,
                cancellationToken)
            .ConfigureAwait(false);

        return records
            .Select(r => SaleReturnEntityMapper.ToDomain(r, lines.TryGetValue(r.Id, out var found) ? found : []))
            .ToList();
    }

    public Task<bool> HasReturnsForSaleAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default) =>
        _db.SaleReturns.AsNoTracking()
            .AnyAsync(
                r => r.OrganizationId == organizationId.Value && r.SaleId == saleId.Value,
                cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, SaleLineReturnTotals>> GetPriorTotalsBySaleLineAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default)
    {
        var totals = await (
                from line in _db.SaleReturnLines.AsNoTracking()
                join ret in _db.SaleReturns.AsNoTracking()
                    on line.SaleReturnId equals ret.Id
                where ret.OrganizationId == organizationId.Value && ret.SaleId == saleId.Value
                group line by line.SaleLineId
                into g
                select new
                {
                    SaleLineId = g.Key,
                    ReturnedQuantity = g.Sum(x => x.QuantityReturned),
                    RefundedAmount = g.Sum(x => x.RefundAmount)
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return totals.ToDictionary(
            t => t.SaleLineId,
            t => new SaleLineReturnTotals(t.ReturnedQuantity, t.RefundedAmount));
    }

    public async Task<decimal> SumCashRefundsForShiftAsync(
        PosOrganizationId organizationId,
        Guid cashierShiftId,
        CancellationToken cancellationToken = default)
    {
        var cashCode = SalePaymentMethods.ToCode(SalePaymentMethod.Cash);
        var sum = await _db.SaleReturns.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value
                        && r.CashierShiftId == cashierShiftId
                        && r.RefundMethod == cashCode)
            .SumAsync(r => r.TotalRefundAmount, cancellationToken)
            .ConfigureAwait(false);
        return SaleMoney.RoundMoney(sum);
    }

    public async Task<SaleReturn> CreateAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, SaleReturn> createReturn,
        Func<SaleReturn, CancellationToken, Task>? afterReturnCreated = null,
        CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await CompleteCreateAsync(
                    organizationId,
                    businessDateUtc,
                    createReturn,
                    afterReturnCreated,
                    cancellationToken)
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
                var saleReturn = await CompleteCreateAsync(
                        organizationId,
                        businessDateUtc,
                        createReturn,
                        afterReturnCreated,
                        cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return saleReturn;
            }
            catch (DbUpdateException ex) when (IsReturnNumberConflict(ex))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.SaleReturnNumberConflict,
                    "A return number was allocated concurrently. Retry the return.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task<SaleReturn> CompleteCreateAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, SaleReturn> createReturn,
        Func<SaleReturn, CancellationToken, Task>? afterReturnCreated,
        CancellationToken cancellationToken)
    {
        try
        {
            var sequence = await ReserveNextSequenceAsync(organizationId, businessDateUtc, cancellationToken)
                .ConfigureAwait(false);
            var saleReturn = createReturn(ReturnNumbers.Format(businessDateUtc, sequence));

            _db.SaleReturns.Add(SaleReturnEntityMapper.ToRecord(saleReturn));
            foreach (var line in saleReturn.Lines)
            {
                _db.SaleReturnLines.Add(SaleReturnEntityMapper.ToRecord(line));
            }

            if (afterReturnCreated is not null)
            {
                await afterReturnCreated(saleReturn, cancellationToken).ConfigureAwait(false);
            }

            foreach (var line in saleReturn.Lines)
            {
                if (line.InventoryMovementId is null)
                {
                    continue;
                }

                var lineRecord = await _db.SaleReturnLines
                    .FirstAsync(
                        l => l.Id == line.Id.Value && l.OrganizationId == organizationId.Value,
                        cancellationToken)
                    .ConfigureAwait(false);
                SaleReturnEntityMapper.ApplyLineInventoryMovement(line, lineRecord);
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return saleReturn;
        }
        catch (DbUpdateException ex) when (IsReturnNumberConflict(ex))
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.SaleReturnNumberConflict,
                "A return number was allocated concurrently. Retry the return.");
        }
    }

    private async Task<Dictionary<Guid, List<SaleReturnLineRecord>>> LoadLinesAsync(
        IReadOnlyList<Guid> returnIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.SaleReturnLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && returnIds.Contains(l.SaleReturnId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(l => l.SaleReturnId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<long> ReserveNextSequenceAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken)
    {
        await _db.Database
            .ExecuteSqlRawAsync(
                LockSequenceSql,
                [SequenceLockKey(organizationId, businessDateUtc)],
                cancellationToken)
            .ConfigureAwait(false);

        var sequence = await _db.SaleReturnNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);

        if (sequence is null)
        {
            _db.SaleReturnNumberSequences.Add(new SaleReturnNumberSequenceRecord
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
        Span<byte> bytes = stackalloc byte[20];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..], businessDateUtc.DayNumber);
        bytes[19] = 0x52; // distinguish from sale sequence locks

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

    public async Task<SaleReturnCogsPeriodAggregate> AggregateReturnCogsForPeriodAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        const string completed = nameof(SaleReturnStatus.Completed);

        var returns = _db.SaleReturns.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value
                        && r.Status == completed
                        && r.ReturnDate >= fromDateUtc
                        && r.ReturnDate <= toDateUtc);

        if (branchId is not null)
        {
            returns = from ret in returns
                join sale in _db.Sales.AsNoTracking() on ret.SaleId equals sale.Id
                where sale.BranchId == branchId.Value
                select ret;
        }

        var rows = await (
                from ret in returns
                join line in _db.SaleReturnLines.AsNoTracking() on ret.Id equals line.SaleReturnId
                join saleLine in _db.SaleLines.AsNoTracking() on line.SaleLineId equals saleLine.Id
                select new { line.QuantityReturned, saleLine.UnitCostSnapshot })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return new SaleReturnCogsPeriodAggregate(0m, false);
        }

        var hasUnknown = false;
        decimal known = 0m;
        foreach (var row in rows)
        {
            if (row.UnitCostSnapshot is null)
            {
                hasUnknown = true;
                continue;
            }

            known += SaleMoney.RoundMoney(row.UnitCostSnapshot.Value * row.QuantityReturned);
        }

        return new SaleReturnCogsPeriodAggregate(SaleMoney.RoundMoney(known), hasUnknown);
    }

    public async Task<decimal> SumRefundsForPeriodAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        const string completed = nameof(SaleReturnStatus.Completed);

        var returns = _db.SaleReturns.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId.Value
                        && r.Status == completed
                        && r.ReturnDate >= fromDateUtc
                        && r.ReturnDate <= toDateUtc);

        if (branchId is not null)
        {
            returns = from ret in returns
                join sale in _db.Sales.AsNoTracking() on ret.SaleId equals sale.Id
                where sale.BranchId == branchId.Value
                select ret;
        }

        var total = await returns.SumAsync(r => (decimal?)r.TotalRefundAmount, cancellationToken)
            .ConfigureAwait(false);

        return SaleMoney.RoundMoney(total ?? 0m);
    }

    private static bool IsReturnNumberConflict(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
        && ex.Entries.Any(e => e.Entity is SaleReturnRecord);
}
