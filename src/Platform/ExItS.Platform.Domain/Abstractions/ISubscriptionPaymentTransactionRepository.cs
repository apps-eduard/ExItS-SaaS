using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Domain.Abstractions;

public interface ISubscriptionPaymentTransactionRepository
{
    Task<SubscriptionPaymentTransaction?> GetByIdAsync(
        SubscriptionPaymentTransactionId id,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPaymentTransaction?> GetByReferenceAsync(
        string referenceNumber,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPaymentTransaction?> GetByProviderReferenceAsync(
        string providerReference,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPaymentTransaction?> FindLatestOpenAsync(
        PlatformUserId initiatedByUserId,
        PlatformOrganizationId? organizationId,
        string planKey,
        BillingCycle billingCycle,
        CancellationToken cancellationToken = default);

    Task<long> GetNextSequenceAsync(CancellationToken cancellationToken = default);

    Task AddAsync(SubscriptionPaymentTransaction payment, CancellationToken cancellationToken = default);

    Task UpdateAsync(SubscriptionPaymentTransaction payment, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubscriptionPaymentTransaction>> ListRecentAsync(
        int take,
        CancellationToken cancellationToken = default);
}
