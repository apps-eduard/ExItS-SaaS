using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Returns;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class ReturnBatchRepository : IReturnBatchRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public ReturnBatchRepository(PosDbContext db) => _db = db;

    public async Task<ReturnBatch?> GetByIdAsync(
        PosOrganizationId organizationId,
        ReturnBatchId returnBatchId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.ReturnBatches.AsNoTracking()
            .FirstOrDefaultAsync(
                b => b.Id == returnBatchId.Value
                    && (b.OrganizationId == organizationId.Value
                        || b.BuyerOrganizationId == organizationId.Value
                        || b.SellerOrganizationId == organizationId.Value),
                cancellationToken)
            .ConfigureAwait(false);
        return record is null
            ? null
            : (await HydrateAsync([record], cancellationToken).ConfigureAwait(false))[0];
    }

    public async Task<IReadOnlyList<ReturnBatch>> ListBySaleIdAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.ReturnBatches.AsNoTracking()
            .Where(b => b.OrganizationId == organizationId.Value && b.SaleId == saleId.Value)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return await HydrateAsync(records, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReturnBatch>> ListByPurchaseOrderIdAsync(
        PosOrganizationId organizationId,
        PurchaseOrderId purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.ReturnBatches.AsNoTracking()
            .Where(b => b.PurchaseOrderId == purchaseOrderId.Value
                && (b.BuyerOrganizationId == organizationId.Value
                    || b.SellerOrganizationId == organizationId.Value))
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return await HydrateAsync(records, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReturnBatch>> ListByConnectedPurchaseOrderIdAsync(
        PosOrganizationId organizationId,
        ConnectedPurchaseOrderId connectedPurchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.ReturnBatches.AsNoTracking()
            .Where(b => b.ConnectedPurchaseOrderId == connectedPurchaseOrderId.Value
                && (b.BuyerOrganizationId == organizationId.Value
                    || b.SellerOrganizationId == organizationId.Value))
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return await HydrateAsync(records, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ReturnBatch>> ListOpenConnectedForSellerAsync(
        PosOrganizationId sellerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var finalized = ReturnBatchStatuses.ToCode(ReturnBatchStatus.Finalized);
        var records = await _db.ReturnBatches.AsNoTracking()
            .Where(b => b.SellerOrganizationId == sellerOrganizationId.Value && b.Status != finalized)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return await HydrateAsync(records, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<ReturnBatch>> HydrateAsync(
        List<ReturnBatchRecord> records,
        CancellationToken cancellationToken)
    {
        if (records.Count == 0)
        {
            return [];
        }

        var batchIds = records.Select(r => r.Id).ToList();
        var lines = await LoadLinesAsync(batchIds, cancellationToken).ConfigureAwait(false);
        var refunds = await LoadRefundsAsync(batchIds, cancellationToken).ConfigureAwait(false);
        var timeline = await LoadTimelineAsync(batchIds, cancellationToken).ConfigureAwait(false);
        return records
            .Select(r => ReturnBatchEntityMapper.ToDomain(
                r,
                lines.TryGetValue(r.Id, out var found) ? found : [],
                refunds.TryGetValue(r.Id, out var foundRefunds) ? foundRefunds : [],
                timeline.TryGetValue(r.Id, out var foundTimeline) ? foundTimeline : []))
            .ToList();
    }

    public async Task<ReturnBatch> CreateAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, ReturnBatch> createBatch,
        Func<ReturnBatch, CancellationToken, Task>? afterCreated = null,
        CancellationToken cancellationToken = default)
    {
        if (_db.Database.CurrentTransaction is not null)
        {
            return await CompleteCreateAsync(organizationId, businessDateUtc, createBatch, afterCreated, cancellationToken)
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
                var created = await CompleteCreateAsync(
                        organizationId,
                        businessDateUtc,
                        createBatch,
                        afterCreated,
                        cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return created;
            }
            catch (DbUpdateException ex) when (IsBatchNumberConflict(ex))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.ConcurrencyConflict,
                    "A return batch number was allocated concurrently. Retry.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ReturnBatch batch, CancellationToken cancellationToken = default)
    {
        var record = await _db.ReturnBatches
            .FirstOrDefaultAsync(
                b => b.OrganizationId == batch.OrganizationId.Value && b.Id == batch.Id.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ReturnBatchNotFound,
                "Return batch was not found.");
        }

        ReturnBatchEntityMapper.ApplyToRecord(batch, record);
        foreach (var line in batch.Lines)
        {
            var lineRecord = await _db.ReturnBatchLines
                .FirstAsync(
                    l => l.OrganizationId == batch.OrganizationId.Value && l.Id == line.Id.Value,
                    cancellationToken)
                .ConfigureAwait(false);
            ReturnBatchEntityMapper.ApplyLineToRecord(line, lineRecord);
        }

        var existingRefundIds = await _db.ReturnBatchRefunds
            .Where(r => r.OrganizationId == batch.OrganizationId.Value && r.ReturnBatchId == batch.Id.Value)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var refund in batch.Refunds.Where(r => !existingRefundIds.Contains(r.Id.Value)))
        {
            var refundRecord = ReturnBatchEntityMapper.ToRecord(refund);
            refundRecord.OrganizationId = batch.OrganizationId.Value;
            _db.ReturnBatchRefunds.Add(refundRecord);
        }

        var existingEventIds = await _db.ReturnBatchAuditEvents
            .Where(r => r.OrganizationId == batch.OrganizationId.Value && r.ReturnBatchId == batch.Id.Value)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var timelineEvent in batch.Timeline.Where(t => !existingEventIds.Contains(t.Id)))
        {
            var timelineRecord = ReturnBatchEntityMapper.ToRecord(timelineEvent);
            timelineRecord.OrganizationId = batch.OrganizationId.Value;
            _db.ReturnBatchAuditEvents.Add(timelineRecord);
        }
    }

    private async Task<ReturnBatch> CompleteCreateAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, ReturnBatch> createBatch,
        Func<ReturnBatch, CancellationToken, Task>? afterCreated,
        CancellationToken cancellationToken)
    {
        try
        {
            var sequence = await ReserveNextSequenceAsync(organizationId, businessDateUtc, cancellationToken)
                .ConfigureAwait(false);
            var batch = createBatch(ReturnBatchNumbers.Format(businessDateUtc, sequence));
            _db.ReturnBatches.Add(ReturnBatchEntityMapper.ToRecord(batch));
            foreach (var line in batch.Lines)
            {
                _db.ReturnBatchLines.Add(ReturnBatchEntityMapper.ToRecord(line));
            }
            foreach (var refund in batch.Refunds)
            {
                var refundRecord = ReturnBatchEntityMapper.ToRecord(refund);
                refundRecord.OrganizationId = batch.OrganizationId.Value;
                _db.ReturnBatchRefunds.Add(refundRecord);
            }
            foreach (var timelineEvent in batch.Timeline)
            {
                var timelineRecord = ReturnBatchEntityMapper.ToRecord(timelineEvent);
                timelineRecord.OrganizationId = batch.OrganizationId.Value;
                _db.ReturnBatchAuditEvents.Add(timelineRecord);
            }

            if (afterCreated is not null)
            {
                await afterCreated(batch, cancellationToken).ConfigureAwait(false);
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return batch;
        }
        catch (DbUpdateException ex) when (IsBatchNumberConflict(ex))
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.ConcurrencyConflict,
                "A return batch number was allocated concurrently. Retry.");
        }
    }

    private async Task<Dictionary<Guid, List<ReturnBatchLineRecord>>> LoadLinesAsync(
        IReadOnlyList<Guid> returnBatchIds,
        CancellationToken cancellationToken)
    {
        var lines = await _db.ReturnBatchLines.AsNoTracking()
            .Where(l => returnBatchIds.Contains(l.ReturnBatchId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return lines.GroupBy(l => l.ReturnBatchId).ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<Dictionary<Guid, List<ReturnBatchRefundRecord>>> LoadRefundsAsync(
        IReadOnlyList<Guid> returnBatchIds,
        CancellationToken cancellationToken)
    {
        var refunds = await _db.ReturnBatchRefunds.AsNoTracking()
            .Where(l => returnBatchIds.Contains(l.ReturnBatchId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return refunds.GroupBy(l => l.ReturnBatchId).ToDictionary(g => g.Key, g => g.ToList());
    }

    private async Task<Dictionary<Guid, List<ReturnBatchAuditEventRecord>>> LoadTimelineAsync(
        IReadOnlyList<Guid> returnBatchIds,
        CancellationToken cancellationToken)
    {
        var timeline = await _db.ReturnBatchAuditEvents.AsNoTracking()
            .Where(l => returnBatchIds.Contains(l.ReturnBatchId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return timeline.GroupBy(l => l.ReturnBatchId).ToDictionary(g => g.Key, g => g.ToList());
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

        var sequence = await _db.ReturnBatchNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);
        if (sequence is null)
        {
            _db.ReturnBatchNumberSequences.Add(new ReturnBatchNumberSequenceRecord
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
        bytes[19] = 0x42;
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

    private static bool IsBatchNumberConflict(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
        && ex.Entries.Any(e => e.Entity is ReturnBatchRecord);
}
