using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Entitlements;

public interface IFeatureOverrideRepository
{
    Task<FeatureOverride?> GetByIdAsync(FeatureOverrideId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeatureOverride>> ListActiveForOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<FeatureOverride> Items, int TotalCount)> ListByOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        FeatureOverrideStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(FeatureOverride featureOverride, CancellationToken cancellationToken = default);

    Task UpdateAsync(FeatureOverride featureOverride, CancellationToken cancellationToken = default);
}

public interface IEntitlementSnapshotRepository
{
    Task<EntitlementSnapshot?> GetByIdAsync(
        EntitlementSnapshotId id,
        CancellationToken cancellationToken = default);

    Task<EntitlementSnapshot?> GetLatestForOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default);

    Task<EntitlementSnapshot?> GetByVersionAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        int snapshotVersion,
        CancellationToken cancellationToken = default);

    Task<int?> GetLatestSnapshotVersionAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<EntitlementSnapshot> Items, int TotalCount)> ListHistoryAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>Entitlement snapshots are immutable once generated: insert-only, never updated.</summary>
    Task AddAsync(EntitlementSnapshot snapshot, CancellationToken cancellationToken = default);
}
