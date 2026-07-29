using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Subscriptions;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken = default);
    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default);
}

public interface IFeatureOverrideRepository
{
    Task<FeatureOverride?> GetByIdAsync(FeatureOverrideId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeatureOverride>> ListActiveForOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task AddAsync(FeatureOverride featureOverride, CancellationToken cancellationToken = default);
    Task UpdateAsync(FeatureOverride featureOverride, CancellationToken cancellationToken = default);
}

public interface IEntitlementSnapshotRepository
{
    Task<int?> GetLatestSnapshotVersionAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default);

    Task AddAsync(EntitlementSnapshot snapshot, CancellationToken cancellationToken = default);
}
