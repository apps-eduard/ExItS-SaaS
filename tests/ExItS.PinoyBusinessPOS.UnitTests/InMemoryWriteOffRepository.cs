using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.UnitTests;

internal sealed class InMemoryWriteOffRepository : IWriteOffRepository
{
    private readonly List<WriteOff> _items = [];

    public Task<WriteOff?> GetByIdAsync(
        PosOrganizationId organizationId,
        WriteOffId writeOffId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.FirstOrDefault(e => e.Id == writeOffId && e.OrganizationId == organizationId));

    public Task<(IReadOnlyList<WriteOff> Items, int TotalCount)> ListByCustomerAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var list = _items.Where(e => e.OrganizationId == organizationId && e.CustomerId == customerId).ToList();
        return Task.FromResult(((IReadOnlyList<WriteOff>)list.Skip(skip).Take(take).ToList(), list.Count));
    }

    public Task<(IReadOnlyList<WriteOff> Items, int TotalCount)> ListCreatedSinceAsync(
        PosOrganizationId organizationId,
        DateTimeOffset? sinceUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<WriteOff> query = _items.Where(e => e.OrganizationId == organizationId);
        if (sinceUtc is not null)
        {
            var since = sinceUtc.Value;
            query = query.Where(e =>
                e.RecordedAtUtc > since
                || (e.ReversedAtUtc is not null && e.ReversedAtUtc > since));
        }

        var list = query.OrderBy(e => e.RecordedAtUtc).ThenBy(e => e.Id.Value).ToList();
        return Task.FromResult(((IReadOnlyList<WriteOff>)list.Skip(skip).Take(take).ToList(), list.Count));
    }

    public Task<decimal> SumActiveAmountAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_items
            .Where(e => e.OrganizationId == organizationId
                        && e.CustomerId == customerId
                        && e.Status == WriteOffStatus.Active)
            .Sum(e => e.Amount));

    public Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var map = _items
            .Where(e => e.OrganizationId == organizationId && e.Status == WriteOffStatus.Active)
            .GroupBy(e => e.CustomerId.Value)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));
        return Task.FromResult((IReadOnlyDictionary<Guid, decimal>)map);
    }

    public Task<int> CountActiveAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_items.Count(e =>
            e.OrganizationId == organizationId
            && e.CustomerId == customerId
            && e.Status == WriteOffStatus.Active));

    public Task AddAsync(WriteOff writeOff, CancellationToken cancellationToken = default)
    {
        _items.Add(writeOff);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(WriteOff writeOff, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
