using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class RepaymentRepository : IRepaymentRepository
{
    private readonly PosDbContext _db;

    public RepaymentRepository(PosDbContext db) => _db = db;

    public async Task<Repayment?> GetByIdAsync(
        PosOrganizationId organizationId,
        RepaymentId repaymentId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Repayments.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == repaymentId.Value && e.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : RepaymentEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<Repayment> Items, int TotalCount)> ListByCustomerAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Repayments.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value && e.CustomerId == customerId.Value);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(e => e.RecordedAtUtc)
            .ThenByDescending(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(RepaymentEntityMapper.ToDomain).ToList(), total);
    }

    public async Task<decimal> SumActiveAmountAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        var active = RepaymentStatus.Active.ToString();
        return await _db.Repayments.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value
                        && e.CustomerId == customerId.Value
                        && e.Status == active)
            .SumAsync(e => e.Amount, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<int> CountActiveAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        var active = RepaymentStatus.Active.ToString();
        return await _db.Repayments.AsNoTracking()
            .CountAsync(
                e => e.OrganizationId == organizationId.Value
                     && e.CustomerId == customerId.Value
                     && e.Status == active,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public Task AddAsync(Repayment repayment, CancellationToken cancellationToken = default)
    {
        _db.Repayments.Add(RepaymentEntityMapper.ToRecord(repayment));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Repayment repayment, CancellationToken cancellationToken = default)
    {
        var record = await _db.Repayments
            .FirstOrDefaultAsync(
                e => e.Id == repayment.Id.Value && e.OrganizationId == repayment.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.RepaymentNotFound,
                "Repayment was not found.");
        }

        RepaymentEntityMapper.ApplyToRecord(repayment, record);
    }
}
