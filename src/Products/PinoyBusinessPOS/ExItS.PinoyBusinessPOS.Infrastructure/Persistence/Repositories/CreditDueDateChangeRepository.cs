using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class CreditDueDateChangeRepository : ICreditDueDateChangeRepository
{
    private readonly PosDbContext _db;

    public CreditDueDateChangeRepository(PosDbContext db) => _db = db;

    public Task AddAsync(CreditDueDateChange change, CancellationToken cancellationToken = default)
    {
        _db.CreditDueDateChanges.Add(CreditDueDateChangeEntityMapper.ToRecord(change));
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<CreditDueDateChange> Items, int TotalCount)> ListByCreditAsync(
        PosOrganizationId organizationId,
        CreditEntryId creditEntryId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CreditDueDateChanges.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && c.CreditEntryId == creditEntryId.Value);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(c => c.ChangedAtUtc)
            .ThenByDescending(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CreditDueDateChangeEntityMapper.ToDomain).ToList(), total);
    }
}
