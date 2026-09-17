using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Payments;

namespace ExItS.Platform.Domain.Abstractions;

public interface ISubscriptionPaymentTransactionRepository
{
    Task<SubscriptionPaymentTransaction?> GetByIdAsync(
        SubscriptionPaymentTransactionId id,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPaymentTransaction?> GetByReferenceAsync(
        string referenceNumber,
        CancellationToken cancellationToken = default);

    Task<long> GetNextSequenceAsync(CancellationToken cancellationToken = default);

    Task AddAsync(SubscriptionPaymentTransaction payment, CancellationToken cancellationToken = default);

    Task UpdateAsync(SubscriptionPaymentTransaction payment, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPaymentTransaction>> ListRecentAsync(
        int take,
        CancellationToken cancellationToken = default);
}
