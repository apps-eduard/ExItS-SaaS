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
            .AsNoTracking()
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
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ReferenceNumber == referenceNumber, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : SubscriptionPaymentTransactionMapper.ToDomain(record);
    }

    public async Task<long> GetNextSequenceAsync(CancellationToken cancellationToken = default)
    {
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
        var affected = await db.SubscriptionPaymentTransactions
            .Where(p => p.Id == payment.Id.Value)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(p => p.OrganizationId, payment.OrganizationId?.Value)
                    .SetProperty(p => p.SubscriptionId, payment.SubscriptionId?.Value)
                    .SetProperty(p => p.Channel, payment.Channel?.ToString())
                    .SetProperty(p => p.Status, payment.Status.ToString())
                    .SetProperty(p => p.ProviderReference, payment.ProviderReference)
                    .SetProperty(p => p.CardBrand, payment.CardBrand)
                    .SetProperty(p => p.CardLast4, payment.CardLast4)
                    .SetProperty(p => p.FailureCode, payment.FailureCode)
                    .SetProperty(p => p.FailureReason, payment.FailureReason)
                    .SetProperty(p => p.ProcessingAtUtc, payment.ProcessingAtUtc)
                    .SetProperty(p => p.PaidAtUtc, payment.PaidAtUtc)
                    .SetProperty(p => p.FailedAtUtc, payment.FailedAtUtc)
                    .SetProperty(p => p.CancelledAtUtc, payment.CancelledAtUtc)
                    .SetProperty(p => p.ExpiredAtUtc, payment.ExpiredAtUtc)
                    .SetProperty(p => p.PeriodStartUtc, payment.PeriodStartUtc)
                    .SetProperty(p => p.PeriodEndUtc, payment.PeriodEndUtc)
                    .SetProperty(p => p.SubscriptionActivated, payment.SubscriptionActivated),
                cancellationToken)
            .ConfigureAwait(false);

        if (affected == 0)
        {
            throw new InvalidOperationException(
                $"Subscription payment {payment.Id.Value:D} was not found for update.");
        }

        var existingActivityIds = await db.SubscriptionPaymentActivities
            .AsNoTracking()
            .Where(a => a.PaymentId == payment.Id.Value)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var known = existingActivityIds.ToHashSet();

        foreach (var activity in payment.Activities)
        {
            if (known.Contains(activity.Id))
            {
                continue;
            }

            await db.SubscriptionPaymentActivities
                .AddAsync(
                    new SubscriptionPaymentActivityRecord
                    {
                        Id = activity.Id,
                        PaymentId = payment.Id.Value,
                        EventType = activity.EventType,
                        Message = activity.Message,
                        OccurredAtUtc = activity.OccurredAtUtc
                    },
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task<IReadOnlyList<SubscriptionPaymentTransaction>> ListRecentAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var records = await db.SubscriptionPaymentTransactions
            .Include(p => p.Activities)
            .AsSplitQuery()
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return records.Select(SubscriptionPaymentTransactionMapper.ToDomain).ToArray();
    }
}
