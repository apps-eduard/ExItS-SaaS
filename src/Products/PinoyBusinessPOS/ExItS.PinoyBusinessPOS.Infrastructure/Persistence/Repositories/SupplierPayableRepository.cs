using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.SupplierPayables;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class SupplierPayableRepository : ISupplierPayableRepository
{
    private readonly PosDbContext _db;

    public SupplierPayableRepository(PosDbContext db) => _db = db;

    public async Task<SupplierPayable?> GetByIdAsync(
        PosOrganizationId organizationId,
        SupplierPayableId payableId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.SupplierPayables.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == payableId.Value && p.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var payments = await _db.SupplierPayablePayments.AsNoTracking()
            .Where(p => p.PayableId == payableId.Value && p.OrganizationId == organizationId.Value)
            .OrderBy(p => p.RecordedAtUtc)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return SupplierPayableEntityMapper.ToDomain(record, payments);
    }

    public async Task<SupplierPayable?> FindBySourceAsync(
        PosOrganizationId organizationId,
        SupplierPayableSourceType sourceType,
        Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        var sourceCode = SupplierPayableSourceTypes.ToCode(sourceType);
        var record = await _db.SupplierPayables.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.OrganizationId == organizationId.Value
                    && p.SourceType == sourceCode
                    && p.SourceId == sourceId,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        var payments = await _db.SupplierPayablePayments.AsNoTracking()
            .Where(p => p.PayableId == record.Id && p.OrganizationId == organizationId.Value)
            .OrderBy(p => p.RecordedAtUtc)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return SupplierPayableEntityMapper.ToDomain(record, payments);
    }

    public async Task<(IReadOnlyList<SupplierPayable> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        SupplierPayableFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(
            _db.SupplierPayables.AsNoTracking().Where(p => p.OrganizationId == organizationId.Value),
            filter);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .ThenByDescending(p => p.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (records.Count == 0)
        {
            return (Array.Empty<SupplierPayable>(), total);
        }

        var ids = records.Select(r => r.Id).ToList();
        var payments = await _db.SupplierPayablePayments.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value && ids.Contains(p.PayableId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var byPayable = payments.GroupBy(p => p.PayableId).ToDictionary(g => g.Key, g => g.ToList());

        var items = records
            .Select(r => SupplierPayableEntityMapper.ToDomain(
                r,
                byPayable.GetValueOrDefault(r.Id)))
            .ToList();

        return (items, total);
    }

    public async Task<IReadOnlyList<SupplierPayablePayment>> ListPaymentsAsync(
        PosOrganizationId organizationId,
        SupplierPayableId payableId,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.SupplierPayablePayments.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value && p.PayableId == payableId.Value)
            .OrderBy(p => p.RecordedAtUtc)
            .ThenBy(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(SupplierPayableEntityMapper.ToPaymentDomain).ToList();
    }

    public async Task AddAsync(SupplierPayable payable, CancellationToken cancellationToken = default)
    {
        _db.SupplierPayables.Add(SupplierPayableEntityMapper.ToRecord(payable));
        foreach (var payment in payable.Payments)
        {
            _db.SupplierPayablePayments.Add(
                SupplierPayableEntityMapper.ToPaymentRecord(payment, payable.OrganizationId.Value));
        }

        // Caller owns the ambient unit-of-work SaveChanges when inside a transaction.
        if (_db.Database.CurrentTransaction is null)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task UpdateAsync(SupplierPayable payable, CancellationToken cancellationToken = default)
    {
        var record = await _db.SupplierPayables
            .FirstOrDefaultAsync(
                p => p.Id == payable.Id.Value && p.OrganizationId == payable.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.SupplierPayableNotFound,
                "Supplier payable was not found.");
        }

        SupplierPayableEntityMapper.ApplyToRecord(payable, record);

        var existingPaymentIds = await _db.SupplierPayablePayments
            .Where(p => p.PayableId == payable.Id.Value && p.OrganizationId == payable.OrganizationId.Value)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var existingSet = existingPaymentIds.ToHashSet();

        foreach (var payment in payable.Payments)
        {
            if (existingSet.Contains(payment.Id.Value))
            {
                continue;
            }

            _db.SupplierPayablePayments.Add(
                SupplierPayableEntityMapper.ToPaymentRecord(payment, payable.OrganizationId.Value));
        }

        if (_db.Database.CurrentTransaction is null)
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<SupplierPayableSummaryTotals> GetSupplierSummaryAsync(
        PosOrganizationId organizationId,
        SupplierId supplierId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        var openStatuses = new[]
        {
            nameof(SupplierPayableStatus.Open),
            nameof(SupplierPayableStatus.PartiallyPaid)
        };

        var rows = await _db.SupplierPayables.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value
                && p.SupplierId == supplierId.Value
                && openStatuses.Contains(p.Status)
                && p.Balance > 0m)
            .Select(p => new { p.Balance, p.DueDate })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var outstanding = rows.Sum(r => r.Balance);
        var overdue = rows.Where(r => r.DueDate is DateOnly d && d < asOfDate).Sum(r => r.Balance);
        return new SupplierPayableSummaryTotals(outstanding, overdue, rows.Count);
    }

    private static IQueryable<SupplierPayableRecord> ApplyFilter(
        IQueryable<SupplierPayableRecord> query,
        SupplierPayableFilter filter)
    {
        if (filter.SupplierId is not null)
        {
            query = query.Where(p => p.SupplierId == filter.SupplierId.Value);
        }

        if (filter.Status is not null)
        {
            var status = filter.Status.Value.ToString();
            query = query.Where(p => p.Status == status);
        }

        if (filter.OutstandingOnly == true)
        {
            var open = new[]
            {
                nameof(SupplierPayableStatus.Open),
                nameof(SupplierPayableStatus.PartiallyPaid)
            };
            query = query.Where(p => open.Contains(p.Status) && p.Balance > 0m);
        }

        if (filter.OverdueOnly == true)
        {
            var asOf = filter.AsOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var open = new[]
            {
                nameof(SupplierPayableStatus.Open),
                nameof(SupplierPayableStatus.PartiallyPaid)
            };
            query = query.Where(p =>
                open.Contains(p.Status)
                && p.Balance > 0m
                && p.DueDate != null
                && p.DueDate < asOf);
        }

        return query;
    }
}
