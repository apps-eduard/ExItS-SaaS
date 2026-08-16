using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Purchasing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class PurchaseOrderRepository : IPurchaseOrderRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public PurchaseOrderRepository(PosDbContext db) => _db = db;

    public async Task<PurchaseOrder?> GetByIdAsync(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PurchaseOrders.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == purchaseOrderId.Value && p.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await LoadPoLinesAsync([record.Id], organizationId, cancellationToken).ConfigureAwait(false);
        return PurchaseEntityMapper.ToDomain(record, lines.TryGetValue(record.Id, out var found) ? found : []);
    }

    public async Task<(IReadOnlyList<PurchaseOrder> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        PurchaseOrderFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.PurchaseOrders.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value);

        if (filter.Status is not null)
        {
            var statusName = filter.Status.Value.ToString();
            query = query.Where(p => p.Status == statusName);
        }

        if (filter.SupplierId is not null)
        {
            query = query.Where(p => p.SupplierId == filter.SupplierId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.PoNumber))
        {
            var term = filter.PoNumber.Trim().ToUpperInvariant();
            query = query.Where(p => p.PoNumber != null && p.PoNumber.Contains(term));
        }

        if (filter.FromOrderDate is not null)
        {
            query = query.Where(p => p.OrderDate >= filter.FromOrderDate.Value);
        }

        if (filter.ToOrderDate is not null)
        {
            query = query.Where(p => p.OrderDate <= filter.ToOrderDate.Value);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(p => p.UpdatedAtUtc)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return ([], total);
        }

        var lines = await LoadPoLinesAsync(
                records.Select(r => r.Id).ToList(),
                organizationId,
                cancellationToken)
            .ConfigureAwait(false);

        var items = records
            .Select(r => PurchaseEntityMapper.ToDomain(r, lines.TryGetValue(r.Id, out var found) ? found : []))
            .ToList();
        return (items, total);
    }

    public Task AddAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        _db.PurchaseOrders.Add(PurchaseEntityMapper.ToRecord(purchaseOrder));
        foreach (var line in purchaseOrder.Lines)
        {
            _db.PurchaseOrderLines.Add(PurchaseEntityMapper.ToRecord(line));
        }

        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        var record = await _db.PurchaseOrders
            .FirstOrDefaultAsync(
                p => p.Id == purchaseOrder.Id.Value && p.OrganizationId == purchaseOrder.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.PurchaseOrderNotFound,
                "Purchase order was not found.");
        }

        PurchaseEntityMapper.ApplyToRecord(purchaseOrder, record);

        var existingLines = await _db.PurchaseOrderLines
            .Where(l => l.PurchaseOrderId == purchaseOrder.Id.Value)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        _db.PurchaseOrderLines.RemoveRange(existingLines);
        foreach (var line in purchaseOrder.Lines)
        {
            _db.PurchaseOrderLines.Add(PurchaseEntityMapper.ToRecord(line));
        }
    }

    public Task<PurchaseOrder> SubmitAsync(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        DateOnly businessDateUtc,
        Func<string, PurchaseOrder> applySubmit,
        Func<PurchaseOrder, CancellationToken, Task>? beforeCommit = null,
        CancellationToken cancellationToken = default) =>
        ExecuteNumberedMutationAsync(
            organizationId,
            businessDateUtc,
            isPo: true,
            async (number, ct) =>
            {
                var po = applySubmit(number);
                var record = await _db.PurchaseOrders
                    .FirstOrDefaultAsync(
                        p => p.Id == purchaseOrderId.Value && p.OrganizationId == organizationId.Value,
                        ct)
                    .ConfigureAwait(false);
                if (record is null)
                {
                    throw new PersistenceConflictException(
                        ApplicationErrorCodes.PurchaseOrderNotFound,
                        "Purchase order was not found.");
                }

                PurchaseEntityMapper.ApplyToRecord(po, record);
                var existingLines = await _db.PurchaseOrderLines
                    .Where(l => l.PurchaseOrderId == po.Id.Value)
                    .ToListAsync(ct)
                    .ConfigureAwait(false);
                _db.PurchaseOrderLines.RemoveRange(existingLines);
                foreach (var line in po.Lines)
                {
                    _db.PurchaseOrderLines.Add(PurchaseEntityMapper.ToRecord(line));
                }

                if (beforeCommit is not null)
                {
                    await beforeCommit(po, ct).ConfigureAwait(false);
                }

                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return po;
            },
            ApplicationErrorCodes.PurchaseOrderNumberConflict,
            "A PO number was allocated concurrently. Retry the submit.",
            cancellationToken);

    public Task<(PurchaseOrder PurchaseOrder, GoodsReceipt GoodsReceipt)> ReceiveAsync(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        DateOnly businessDateUtc,
        Func<string, (PurchaseOrder UpdatedPo, GoodsReceipt Receipt)> applyReceive,
        Func<GoodsReceipt, PurchaseOrder, CancellationToken, Task>? afterReceiptCreated = null,
        CancellationToken cancellationToken = default) =>
        ExecuteNumberedMutationAsync(
            organizationId,
            businessDateUtc,
            isPo: false,
            async (grnNumber, ct) =>
            {
                var (po, receipt) = applyReceive(grnNumber);
                var record = await _db.PurchaseOrders
                    .FirstOrDefaultAsync(
                        p => p.Id == purchaseOrderId.Value && p.OrganizationId == organizationId.Value,
                        ct)
                    .ConfigureAwait(false);
                if (record is null)
                {
                    throw new PersistenceConflictException(
                        ApplicationErrorCodes.PurchaseOrderNotFound,
                        "Purchase order was not found.");
                }

                PurchaseEntityMapper.ApplyToRecord(po, record);
                foreach (var line in po.Lines)
                {
                    var lineRecord = await _db.PurchaseOrderLines
                        .FirstOrDefaultAsync(
                            l => l.Id == line.Id.Value && l.PurchaseOrderId == po.Id.Value,
                            ct)
                        .ConfigureAwait(false);
                    if (lineRecord is null)
                    {
                        throw new PersistenceConflictException(
                            ApplicationErrorCodes.PurchaseOrderNotFound,
                            "Purchase order line was not found.");
                    }

                    lineRecord.ReceivedQty = line.ReceivedQty;
                }

                _db.GoodsReceipts.Add(PurchaseEntityMapper.ToRecord(receipt));
                foreach (var line in receipt.Lines)
                {
                    _db.GoodsReceiptLines.Add(PurchaseEntityMapper.ToRecord(line));
                }

                if (afterReceiptCreated is not null)
                {
                    await afterReceiptCreated(receipt, po, ct).ConfigureAwait(false);
                    foreach (var line in receipt.Lines)
                    {
                        var lineRecord = _db.GoodsReceiptLines.Local.FirstOrDefault(l => l.Id == line.Id.Value);
                        if (lineRecord is not null)
                        {
                            PurchaseEntityMapper.ApplyMovementId(line, lineRecord);
                        }
                    }
                }

                await _db.SaveChangesAsync(ct).ConfigureAwait(false);
                return (po, receipt);
            },
            ApplicationErrorCodes.GoodsReceiptNumberConflict,
            "A GRN number was allocated concurrently. Retry the receive.",
            cancellationToken);

    public async Task<GoodsReceipt?> GetGoodsReceiptByIdAsync(
        PosOrganizationId organizationId,
        GoodsReceiptId goodsReceiptId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.GoodsReceipts.AsNoTracking()
            .FirstOrDefaultAsync(
                g => g.Id == goodsReceiptId.Value && g.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await _db.GoodsReceiptLines.AsNoTracking()
            .Where(l => l.GoodsReceiptId == record.Id && l.OrganizationId == organizationId.Value)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return PurchaseEntityMapper.ToDomain(record, lines);
    }

    public async Task<IReadOnlyList<GoodsReceipt>> ListGoodsReceiptsForPurchaseOrderAsync(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.GoodsReceipts.AsNoTracking()
            .Where(g => g.OrganizationId == organizationId.Value && g.PurchaseOrderId == purchaseOrderId.Value)
            .OrderByDescending(g => g.ReceivedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (records.Count == 0)
        {
            return [];
        }

        var grnIds = records.Select(r => r.Id).ToList();
        var lines = await _db.GoodsReceiptLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && grnIds.Contains(l.GoodsReceiptId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var linesByGrn = lines.GroupBy(l => l.GoodsReceiptId).ToDictionary(g => g.Key, g => g.ToList());

        return records
            .Select(r => PurchaseEntityMapper.ToDomain(r, linesByGrn.TryGetValue(r.Id, out var found) ? found : []))
            .ToList();
    }

    private async Task<T> ExecuteNumberedMutationAsync<T>(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        bool isPo,
        Func<string, CancellationToken, Task<T>> complete,
        string conflictCode,
        string conflictMessage,
        CancellationToken cancellationToken)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await CompleteNumberedAsync(organizationId, businessDateUtc, isPo, complete, conflictCode, conflictMessage, cancellationToken)
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
                        isPo,
                        complete,
                        conflictCode,
                        conflictMessage,
                        cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
            catch (DbUpdateException ex) when (IsNumberConflict(ex, isPo))
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
        bool isPo,
        Func<string, CancellationToken, Task<T>> complete,
        string conflictCode,
        string conflictMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var sequence = await ReserveNextSequenceAsync(organizationId, businessDateUtc, isPo, cancellationToken)
                .ConfigureAwait(false);
            var number = isPo
                ? PurchaseOrderNumbers.Format(businessDateUtc, sequence)
                : GoodsReceiptNumbers.Format(businessDateUtc, sequence);
            return await complete(number, cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsNumberConflict(ex, isPo))
        {
            throw new PersistenceConflictException(conflictCode, conflictMessage);
        }
    }

    private async Task<long> ReserveNextSequenceAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        bool isPo,
        CancellationToken cancellationToken)
    {
        await _db.Database
            .ExecuteSqlRawAsync(LockSequenceSql, [SequenceLockKey(organizationId, businessDateUtc, isPo)], cancellationToken)
            .ConfigureAwait(false);

        if (isPo)
        {
            var sequence = await _db.PurchaseOrderNumberSequences
                .FirstOrDefaultAsync(
                    s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                    cancellationToken)
                .ConfigureAwait(false);
            if (sequence is null)
            {
                _db.PurchaseOrderNumberSequences.Add(new PurchaseOrderNumberSequenceRecord
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
        else
        {
            var sequence = await _db.GoodsReceiptNumberSequences
                .FirstOrDefaultAsync(
                    s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                    cancellationToken)
                .ConfigureAwait(false);
            if (sequence is null)
            {
                _db.GoodsReceiptNumberSequences.Add(new GoodsReceiptNumberSequenceRecord
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
    }

    private static long SequenceLockKey(PosOrganizationId organizationId, DateOnly businessDateUtc, bool isPo)
    {
        Span<byte> bytes = stackalloc byte[21];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..20], businessDateUtc.DayNumber);
        bytes[20] = isPo ? (byte)1 : (byte)2;

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

    private async Task<Dictionary<Guid, List<PurchaseOrderLineRecord>>> LoadPoLinesAsync(
        IReadOnlyCollection<Guid> poIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.PurchaseOrderLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && poIds.Contains(l.PurchaseOrderId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(l => l.PurchaseOrderId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static bool IsNumberConflict(DbUpdateException exception, bool isPo)
    {
        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            return false;
        }

        var constraint = pg.ConstraintName ?? string.Empty;
        return isPo
            ? constraint.Contains("ux_purchase_orders_org_po_number", StringComparison.OrdinalIgnoreCase)
            : constraint.Contains("ux_goods_receipts_org_grn_number", StringComparison.OrdinalIgnoreCase);
    }
}
