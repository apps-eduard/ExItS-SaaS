using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Infrastructure.Persistence.Payments;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class SubscriptionPaymentTransactionRepository(PlatformDbContext db)
    : ISubscriptionPaymentTransactionRepository
{
    public async Task<SubscriptionPaymentTransaction?> GetByIdAsync(
        SubscriptionPaymentTransactionId id,
        CancellationToken cancellationToken = default)
    {
        var record = await db.SubscriptionPaymentTransactions
            .Include(p => p.Activities)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : SubscriptionPaymentTransactionMapper.ToDomain(record);
    }

    public async Task<SubscriptionPaymentTransaction?> GetByReferenceAsync(
        string referenceNumber,
        CancellationToken cancellationToken = default)
    {
        var record = await db.SubscriptionPaymentTransactions
            .Include(p => p.Activities)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.ReferenceNumber == referenceNumber, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : SubscriptionPaymentTransactionMapper.ToDomain(record);
    }

    public async Task<long> GetNextSequenceAsync(CancellationToken cancellationToken = default)
    {
        // Postgres sequence-like counter via max+1 under advisory lock is overkill for LV;
        // use count+1 with unique constraint on reference for safety.
        var count = await db.SubscriptionPaymentTransactions.LongCountAsync(cancellationToken)
            .ConfigureAwait(false);
        return count + 1;
    }

    public async Task AddAsync(
        SubscriptionPaymentTransaction payment,
        CancellationToken cancellationToken = default)
    {
        await db.SubscriptionPaymentTransactions
            .AddAsync(SubscriptionPaymentTransactionMapper.ToNewRecord(payment), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        SubscriptionPaymentTransaction payment,
        CancellationToken cancellationToken = default)
    {
        var record = await db.SubscriptionPaymentTransactions
            .Include(p => p.Activities)
            .FirstAsync(p => p.Id == payment.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        SubscriptionPaymentTransactionMapper.ApplyToRecord(payment, record);
    }

    public async Task<IReadOnlyList<SubscriptionPaymentTransaction>> ListRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var records = await db.SubscriptionPaymentTransactions
            .Include(p => p.Activities)
            .AsSplitQuery()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(SubscriptionPaymentTransactionMapper.ToDomain).ToArray();
    }
}
