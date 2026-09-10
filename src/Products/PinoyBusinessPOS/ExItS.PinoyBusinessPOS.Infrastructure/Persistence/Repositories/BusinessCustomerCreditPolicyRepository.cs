using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class BusinessCustomerCreditPolicyRepository : IBusinessCustomerCreditPolicyRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public BusinessCustomerCreditPolicyRepository(PosDbContext db) => _db = db;

    public async Task<BusinessCustomerCreditPolicy?> GetBySellerAndBuyerAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BusinessCustomerCreditPolicies.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.SellerOrganizationId == sellerOrganizationId.Value
                     && p.BuyerOrganizationId == buyerOrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : BusinessCustomerCreditPolicyEntityMapper.ToDomain(record);
    }

    public Task AddAsync(BusinessCustomerCreditPolicy policy, CancellationToken cancellationToken = default)
    {
        _db.BusinessCustomerCreditPolicies.Add(BusinessCustomerCreditPolicyEntityMapper.ToRecord(policy));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(BusinessCustomerCreditPolicy policy, CancellationToken cancellationToken = default)
    {
        var record = await _db.BusinessCustomerCreditPolicies
            .FirstOrDefaultAsync(
                p => p.Id == policy.Id.Value
                     && p.SellerOrganizationId == policy.SellerOrganizationId.Value
                     && p.BuyerOrganizationId == policy.BuyerOrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.BusinessCustomerCreditPolicyNotFound,
                "Business customer credit policy was not found.");
        }

        BusinessCustomerCreditPolicyEntityMapper.ApplyToRecord(policy, record);
    }

    public Task AddChangeAsync(BusinessCustomerCreditPolicyChange change, CancellationToken cancellationToken = default)
    {
        _db.BusinessCustomerCreditPolicyChanges.Add(BusinessCustomerCreditPolicyEntityMapper.ToRecord(change));
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<BusinessCustomerCreditPolicyChange> Items, int TotalCount)> ListChangesAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.BusinessCustomerCreditPolicyChanges.AsNoTracking()
            .Where(c => c.SellerOrganizationId == sellerOrganizationId.Value
                        && c.BuyerOrganizationId == buyerOrganizationId.Value);

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(c => c.ChangedAtUtc)
            .ThenByDescending(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(BusinessCustomerCreditPolicyEntityMapper.ToDomain).ToList(), total);
    }

    public async Task AcquireBusinessCustomerCreditLockAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        await _db.Database
            .ExecuteSqlRawAsync(
                LockSequenceSql,
                [BusinessCustomerCreditLockKey(sellerOrganizationId, buyerOrganizationId)],
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static long BusinessCustomerCreditLockKey(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId)
    {
        Span<byte> bytes = stackalloc byte[32];
        sellerOrganizationId.Value.TryWriteBytes(bytes[..16]);
        buyerOrganizationId.Value.TryWriteBytes(bytes[16..]);

        unchecked
        {
            var hash = 0xcbf29ce484222325UL;
            foreach (var b in bytes)
            {
                hash = (hash ^ b) * 0x100000001b3UL;
            }

            // Distinct namespace from people-customer credit locks.
            return (long)(hash ^ 0xB2BC0001UL);
        }
    }
}
