using System.Buffers.Binary;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class SaleRepository : ISaleRepository
{
    /// <summary>
    /// Transaction-scoped advisory lock over the (organization, business date) counter. Concurrent
    /// checkouts for the same organization and date queue behind each other, so the read-modify-write
    /// of <c>pos.sale_number_sequences</c> below cannot interleave and every checkout receives a
    /// distinct sequence value. The lock is released when the checkout transaction ends.
    /// </summary>
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public SaleRepository(PosDbContext db) => _db = db;

    public async Task<Sale?> GetByIdAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Sales.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Id == saleId.Value && s.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await LoadLinesAsync([record.Id], organizationId, cancellationToken).ConfigureAwait(false);
        return SaleEntityMapper.ToDomain(record, lines.TryGetValue(record.Id, out var found) ? found : []);
    }

    public async Task<Sale?> FindBySaleNumberAsync(
        PosOrganizationId organizationId,
        string saleNumber,
        CancellationToken cancellationToken = default)
    {
        var normalized = SaleNumbers.Normalize(saleNumber);
        var record = await _db.Sales.AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.SaleNumber == normalized,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var lines = await LoadLinesAsync([record.Id], organizationId, cancellationToken).ConfigureAwait(false);
        return SaleEntityMapper.ToDomain(record, lines.TryGetValue(record.Id, out var found) ? found : []);
    }

    public async Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        SaleFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Sales.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value);

        if (filter.Status is not null)
        {
            var statusName = filter.Status.Value.ToString();
            query = query.Where(s => s.Status == statusName);
        }

        if (filter.PaymentMethod is not null)
        {
            var methodCode = SalePaymentMethods.ToCode(filter.PaymentMethod.Value);
            query = query.Where(s => s.PaymentMethod == methodCode);
        }

        if (filter.FromDateUtc is not null)
        {
            var from = new DateTimeOffset(filter.FromDateUtc.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(s => s.RecordedAtUtc >= from);
        }

        if (filter.ToDateUtc is not null)
        {
            var exclusiveTo = new DateTimeOffset(
                filter.ToDateUtc.Value.AddDays(1).ToDateTime(TimeOnly.MinValue),
                TimeSpan.Zero);
            query = query.Where(s => s.RecordedAtUtc < exclusiveTo);
        }

        if (!string.IsNullOrWhiteSpace(filter.SaleNumber))
        {
            var term = filter.SaleNumber.Trim().ToUpperInvariant();
            query = query.Where(s => s.SaleNumber.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(s => s.RecordedAtUtc)
            .ThenByDescending(s => s.SaleNumber)
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

        var sales = records
            .Select(r => SaleEntityMapper.ToDomain(r, lines.TryGetValue(r.Id, out var found) ? found : []))
            .ToList();
        return (sales, total);
    }

    public async Task<Sale> CheckoutAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, Sale> createSale,
        Func<Sale, CancellationToken, Task>? afterSaleCreated = null,
        CancellationToken cancellationToken = default)
    {
        // When called under PosIdempotencyService (or any ambient transaction), reuse that
        // transaction so we do not nest BeginTransaction on the same connection.
        if (_db.Database.CurrentTransaction is not null)
        {
            return await CompleteCheckoutAsync(
                    organizationId,
                    businessDateUtc,
                    createSale,
                    afterSaleCreated,
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
                var sale = await CompleteCheckoutAsync(
                        organizationId,
                        businessDateUtc,
                        createSale,
                        afterSaleCreated,
                        cancellationToken)
                    .ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return sale;
            }
            catch (DbUpdateException ex) when (IsSaleNumberConflict(ex))
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw new PersistenceConflictException(
                    ApplicationErrorCodes.SaleNumberConflict,
                    "A sale number was allocated concurrently. Retry the checkout.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task<Sale> CompleteCheckoutAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        Func<string, Sale> createSale,
        Func<Sale, CancellationToken, Task>? afterSaleCreated,
        CancellationToken cancellationToken)
    {
        try
        {
            var sequence = await ReserveNextSequenceAsync(organizationId, businessDateUtc, cancellationToken)
                .ConfigureAwait(false);
            var sale = createSale(SaleNumbers.Format(businessDateUtc, sequence));

            _db.Sales.Add(SaleEntityMapper.ToRecord(sale));
            foreach (var line in sale.Lines)
            {
                _db.SaleLines.Add(SaleEntityMapper.ToRecord(line));
            }

            if (afterSaleCreated is not null)
            {
                await afterSaleCreated(sale, cancellationToken).ConfigureAwait(false);
            }

            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return sale;
        }
        catch (DbUpdateException ex) when (IsSaleNumberConflict(ex))
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.SaleNumberConflict,
                "A sale number was allocated concurrently. Retry the checkout.");
        }
    }

    public async Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        var record = await _db.Sales
            .FirstOrDefaultAsync(
                s => s.Id == sale.Id.Value && s.OrganizationId == sale.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(ApplicationErrorCodes.SaleNotFound, "Sale was not found.");
        }

        SaleEntityMapper.ApplyToRecord(sale, record);
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

        var sequence = await _db.SaleNumberSequences
            .FirstOrDefaultAsync(
                s => s.OrganizationId == organizationId.Value && s.BusinessDate == businessDateUtc,
                cancellationToken)
            .ConfigureAwait(false);

        if (sequence is null)
        {
            _db.SaleNumberSequences.Add(new SaleNumberSequenceRecord
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

    /// <summary>
    /// Stable 64-bit advisory-lock key for one organization's counter on one business date. Only the
    /// key's stability matters: a collision would merely serialize two unrelated counters.
    /// </summary>
    private static long SequenceLockKey(PosOrganizationId organizationId, DateOnly businessDateUtc)
    {
        Span<byte> bytes = stackalloc byte[20];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        BinaryPrimitives.WriteInt32LittleEndian(bytes[16..], businessDateUtc.DayNumber);

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

    private async Task<Dictionary<Guid, List<SaleLineRecord>>> LoadLinesAsync(
        IReadOnlyCollection<Guid> saleIds,
        PosOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var records = await _db.SaleLines.AsNoTracking()
            .Where(l => l.OrganizationId == organizationId.Value && saleIds.Contains(l.SaleId))
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records
            .GroupBy(l => l.SaleId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static bool IsSaleNumberConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg
        && (pg.ConstraintName ?? string.Empty).Contains("ux_sales_org_sale_number", StringComparison.OrdinalIgnoreCase);
}
