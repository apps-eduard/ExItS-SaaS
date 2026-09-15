using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class BusinessRepaymentRepository : IBusinessRepaymentRepository
{
    private readonly PosDbContext _db;

    public BusinessRepaymentRepository(PosDbContext db) => _db = db;

    public async Task<BusinessRepayment?> GetByIdAsync(
        PosOrganizationId sellerOrganizationId,
        BusinessRepaymentId repaymentId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BusinessRepayments.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == repaymentId.Value
                     && e.SellerOrganizationId == sellerOrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : BusinessRepaymentEntityMapper.ToDomain(record);
    }

    public Task AddAsync(BusinessRepayment repayment, CancellationToken cancellationToken = default)
    {
        _db.BusinessRepayments.Add(BusinessRepaymentEntityMapper.ToRecord(repayment));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(BusinessRepayment repayment, CancellationToken cancellationToken = default)
    {
        var record = await _db.BusinessRepayments
            .FirstOrDefaultAsync(
                e => e.Id == repayment.Id.Value
                     && e.SellerOrganizationId == repayment.SellerOrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.BusinessRepaymentNotFound,
                "Business repayment was not found.");
        }

        BusinessRepaymentEntityMapper.ApplyToRecord(repayment, record);
    }

    public async Task<decimal> SumSettledAmountAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var active = RepaymentStatus.Active.ToString();
        var checkMethod = UtangPaymentMethod.Check.ToString();
        var cleared = UtangCheckClearingStatus.Cleared.ToString();
        return await _db.BusinessRepayments.AsNoTracking()
            .Where(e => e.SellerOrganizationId == sellerOrganizationId.Value
                        && e.BuyerOrganizationId == buyerOrganizationId.Value
                        && e.Status == active
                        && (e.PaymentMethod != checkMethod || e.CheckClearingStatus == cleared))
            .SumAsync(e => e.Amount, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<decimal> SumPendingCheckAmountAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var active = RepaymentStatus.Active.ToString();
        var checkMethod = UtangPaymentMethod.Check.ToString();
        var pending = UtangCheckClearingStatus.PendingClearing.ToString();
        return await _db.BusinessRepayments.AsNoTracking()
            .Where(e => e.SellerOrganizationId == sellerOrganizationId.Value
                        && e.BuyerOrganizationId == buyerOrganizationId.Value
                        && e.Status == active
                        && e.PaymentMethod == checkMethod
                        && e.CheckClearingStatus == pending)
            .SumAsync(e => e.Amount, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BusinessRepayment>> ListByConnectionAsync(
        PosOrganizationId sellerOrganizationId,
        Guid connectionId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.BusinessRepayments.AsNoTracking()
            .Where(e => e.SellerOrganizationId == sellerOrganizationId.Value
                        && e.ConnectionId == connectionId)
            .OrderByDescending(e => e.RecordedAtUtc)
            .ThenByDescending(e => e.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(BusinessRepaymentEntityMapper.ToDomain).ToList();
    }
}
