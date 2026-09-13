using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class BusinessCreditEntryRepository : IBusinessCreditEntryRepository
{
    private const string LockSequenceSql = "SELECT pg_advisory_xact_lock({0})";

    private readonly PosDbContext _db;

    public BusinessCreditEntryRepository(PosDbContext db) => _db = db;

    public async Task<BusinessCreditEntry?> GetByIdAsync(
        PosOrganizationId sellerOrganizationId,
        BusinessCreditEntryId entryId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.BusinessCreditEntries.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == entryId.Value
                     && e.SellerOrganizationId == sellerOrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : BusinessCreditEntryEntityMapper.ToDomain(record);
    }

    public Task AddAsync(BusinessCreditEntry entry, CancellationToken cancellationToken = default)
    {
        _db.BusinessCreditEntries.Add(BusinessCreditEntryEntityMapper.ToRecord(entry));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(BusinessCreditEntry entry, CancellationToken cancellationToken = default)
    {
        var record = await _db.BusinessCreditEntries
            .FirstOrDefaultAsync(
                e => e.Id == entry.Id.Value
                     && e.SellerOrganizationId == entry.SellerOrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.BusinessCreditEntryNotFound,
                "Business credit entry was not found.");
        }

        BusinessCreditEntryEntityMapper.ApplyToRecord(entry, record);
    }

    public async Task<decimal> SumActiveAmountAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var active = CreditEntryStatus.Active.ToString();
        return await _db.BusinessCreditEntries.AsNoTracking()
            .Where(e => e.SellerOrganizationId == sellerOrganizationId.Value
                        && e.BuyerOrganizationId == buyerOrganizationId.Value
                        && e.Status == active)
            .SumAsync(e => e.Amount, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByBuyerIdsAsync(
        PosOrganizationId sellerOrganizationId,
        IReadOnlyCollection<Guid> buyerOrganizationIds,
        CancellationToken cancellationToken = default)
    {
        if (buyerOrganizationIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var ids = buyerOrganizationIds as IList<Guid> ?? buyerOrganizationIds.ToList();
        var active = CreditEntryStatus.Active.ToString();
        var rows = await _db.BusinessCreditEntries.AsNoTracking()
            .Where(e => e.SellerOrganizationId == sellerOrganizationId.Value
                        && ids.Contains(e.BuyerOrganizationId)
                        && e.Status == active)
            .GroupBy(e => e.BuyerOrganizationId)
            .Select(g => new { BuyerOrganizationId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows.ToDictionary(r => r.BuyerOrganizationId, r => r.Total);
    }

    public async Task AcquireBusinessCreditLockAsync(
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

    /// <summary>Same advisory-lock namespace as business credit policy mutations.</summary>
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

            return (long)(hash ^ 0xB2BC0001UL);
        }
    }
}
