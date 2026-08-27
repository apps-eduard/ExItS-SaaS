using ExItS.PinoyBuyNowPayLater.Application.Customers;
using ExItS.PinoyBuyNowPayLater.Application.Financing;
using ExItS.PinoyBuyNowPayLater.Domain.Financing;
using ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Financing;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Repositories;

internal sealed class BnplFinancingApplicationRepository : IBnplFinancingApplicationRepository
{
    private readonly BnplDbContext _db;

    public BnplFinancingApplicationRepository(BnplDbContext db) => _db = db;

    public async Task<BnplFinancingApplication?> GetByIdAsync(
        Guid organizationId,
        BnplFinancingApplicationId applicationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.FinancingApplications
            .AsSplitQuery()
            .Include(a => a.Offers)
            .Include(a => a.Decisions)
            .Include(a => a.InstallmentPlans)
                .ThenInclude(p => p.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.Id == applicationId.Value && a.OrganizationId == organizationId,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : BnplFinancingEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<BnplFinancingApplication> Items, int TotalCount)> SearchAsync(
        Guid organizationId,
        Guid? branchId,
        Guid? customerId,
        BnplFinancingApplicationStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.FinancingApplications.AsNoTracking()
            .Where(a => a.OrganizationId == organizationId);

        if (branchId is Guid b)
        {
            query = query.Where(a => a.BranchId == b);
        }

        if (customerId is Guid c)
        {
            query = query.Where(a => a.CustomerId == c);
        }

        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(a => a.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var ids = await query
            .OrderByDescending(a => a.UpdatedAtUtc)
            .ThenBy(a => a.Id)
            .Skip(skip)
            .Take(take)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var records = await _db.FinancingApplications
            .AsSplitQuery()
            .Include(a => a.Offers)
            .Include(a => a.Decisions)
            .Include(a => a.InstallmentPlans)
                .ThenInclude(p => p.Items)
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ordered = ids
            .Select(id => records.First(r => r.Id == id))
            .Select(BnplFinancingEntityMapper.ToDomain)
            .ToList();

        return (ordered, total);
    }

    public async Task AddAsync(BnplFinancingApplication application, CancellationToken cancellationToken = default)
    {
        await _db.FinancingApplications.AddAsync(BnplFinancingEntityMapper.ToRecord(application), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(BnplFinancingApplication application, CancellationToken cancellationToken = default)
    {
        var record = await _db.FinancingApplications
            .Include(a => a.Offers)
            .Include(a => a.Decisions)
            .Include(a => a.InstallmentPlans)
                .ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(
                a => a.Id == application.Id.Value && a.OrganizationId == application.OrganizationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new BnplPersistenceConflictException(
                BnplFinancingErrorCodes.NotFound,
                "Financing application was not found in this organization.");
        }

        BnplFinancingEntityMapper.CopyToRecord(application, record);
        await Task.CompletedTask;
    }
}
