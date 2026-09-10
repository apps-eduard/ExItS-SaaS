using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class CustomerCreditPolicyRepository : ICustomerCreditPolicyRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public CustomerCreditPolicyRepository(PosDbContext db) => _db = db;

    public async Task<CustomerCreditPolicy?> GetByCustomerAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CustomerCreditPolicies.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.OrganizationId == organizationId.Value && p.CustomerId == customerId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CustomerCreditPolicyEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<CustomerCreditPolicy>> ListByCustomerIdsAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> customerIds,
        CancellationToken cancellationToken = default)
    {
        if (customerIds.Count == 0)
        {
            return [];
        }

        var ids = customerIds as IList<Guid> ?? customerIds.ToList();
        var records = await _db.CustomerCreditPolicies.AsNoTracking()
            .Where(p => p.OrganizationId == organizationId.Value && ids.Contains(p.CustomerId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(CustomerCreditPolicyEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(CustomerCreditPolicy policy, CancellationToken cancellationToken = default)
    {
        _db.CustomerCreditPolicies.Add(CustomerCreditPolicyEntityMapper.ToRecord(policy));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(CustomerCreditPolicy policy, CancellationToken cancellationToken = default)
    {
        var record = await _db.CustomerCreditPolicies
            .FirstOrDefaultAsync(
                p => p.Id == policy.Id.Value
                     && p.OrganizationId == policy.OrganizationId.Value
                     && p.CustomerId == policy.CustomerId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CustomerCreditPolicyNotFound,
                "Customer credit policy was not found.");
        }

        CustomerCreditPolicyEntityMapper.ApplyToRecord(policy, record);
    }

    public Task AddChangeAsync(CustomerCreditPolicyChange change, CancellationToken cancellationToken = default)
    {
        _db.CustomerCreditPolicyChanges.Add(CustomerCreditPolicyEntityMapper.ToRecord(change));
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<CustomerCreditPolicyChange> Items, int TotalCount)> ListChangesAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CustomerCreditPolicyChanges.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId.Value && c.CustomerId == customerId.Value);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(c => c.ChangedAtUtc)
            .ThenByDescending(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CustomerCreditPolicyEntityMapper.ToDomain).ToList(), total);
    }

    public async Task AcquireCustomerCreditLockAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        await _db.Database
            .ExecuteSqlRawAsync(
                LockSequenceSql,
                [CustomerCreditLockKey(organizationId, customerId)],
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static long CustomerCreditLockKey(PosOrganizationId organizationId, POSCustomerId customerId)
    {
        Span<byte> bytes = stackalloc byte[32];
        organizationId.Value.TryWriteBytes(bytes[..16]);
        customerId.Value.TryWriteBytes(bytes[16..]);

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
}
