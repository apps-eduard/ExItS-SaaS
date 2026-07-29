using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Entitlements;

public sealed record EntitlementGrantDto(
    string FeatureCode,
    bool Enabled,
    int? NumericLimit,
    string Source,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset? ExpiresAtUtc);

public sealed record EntitlementSnapshotDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    Guid SubscriptionId,
    string PlanCode,
    int PlanVersionNumber,
    int SnapshotVersion,
    int SchemaVersion,
    string SubscriptionStatus,
    bool InGracePeriod,
    DateTimeOffset GeneratedAtUtc,
    DateTimeOffset EffectiveAtUtc,
    DateTimeOffset RefreshByUtc,
    DateTimeOffset? ExpiresAtUtc,
    int SourceAggregateVersion,
    IReadOnlyList<EntitlementGrantDto> Grants);

public sealed record FeatureOverrideDto(
    Guid Id,
    Guid OrganizationId,
    string ProductCode,
    string FeatureCode,
    bool Enabled,
    int? NumericLimit,
    string Reason,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? ExpiresAtUtc,
    string Status,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedByUserId,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedByUserId,
    string? RevocationReason);

public sealed class EntitlementQueryService
{
    private readonly IEntitlementSnapshotRepository _snapshots;

    public EntitlementQueryService(IEntitlementSnapshotRepository snapshots)
    {
        _snapshots = snapshots;
    }

    public async Task<EntitlementSnapshotDto?> GetSnapshotByIdAsync(
        Guid snapshotId,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _snapshots
            .GetByIdAsync(EntitlementSnapshotId.From(snapshotId), cancellationToken)
            .ConfigureAwait(false);
        return snapshot is null ? null : Map(snapshot);
    }

    public async Task<EntitlementSnapshotDto?> GetLatestAsync(
        Guid organizationId,
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _snapshots
            .GetLatestForOrganizationProductAsync(
                PlatformOrganizationId.From(organizationId),
                ProductCode.Create(productCode),
                cancellationToken)
            .ConfigureAwait(false);
        return snapshot is null ? null : Map(snapshot);
    }

    public async Task<EntitlementSnapshotDto?> GetByVersionAsync(
        Guid organizationId,
        string productCode,
        int snapshotVersion,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _snapshots
            .GetByVersionAsync(
                PlatformOrganizationId.From(organizationId),
                ProductCode.Create(productCode),
                snapshotVersion,
                cancellationToken)
            .ConfigureAwait(false);
        return snapshot is null ? null : Map(snapshot);
    }

    public async Task<PagedResult<EntitlementSnapshotDto>> ListHistoryAsync(
        Guid organizationId,
        string productCode,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _snapshots
            .ListHistoryAsync(
                PlatformOrganizationId.From(organizationId),
                ProductCode.Create(productCode),
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        var pageNumber = Math.Max(page ?? 1, 1);
        return new PagedResult<EntitlementSnapshotDto>(items.Select(Map).ToList(), totalCount, pageNumber, take);
    }

    private static EntitlementSnapshotDto Map(EntitlementSnapshot snapshot) =>
        new(
            snapshot.Id.Value,
            snapshot.OrganizationId.Value,
            snapshot.ProductCode.Value,
            snapshot.SubscriptionId.Value,
            snapshot.PlanCode.Value,
            snapshot.PlanVersionNumber,
            snapshot.SnapshotVersion,
            snapshot.SchemaVersion,
            snapshot.SubscriptionStatus.ToString(),
            snapshot.InGracePeriod,
            snapshot.GeneratedAtUtc,
            snapshot.EffectiveAtUtc,
            snapshot.RefreshByUtc,
            snapshot.ExpiresAtUtc,
            snapshot.SourceAggregateVersion,
            snapshot.Grants.Select(MapGrant).ToList());

    private static EntitlementGrantDto MapGrant(EntitlementGrant grant) =>
        new(
            grant.FeatureCode.Value,
            grant.Enabled,
            grant.NumericLimit,
            grant.Source.ToString(),
            grant.EffectiveAtUtc,
            grant.ExpiresAtUtc);
}

public sealed class FeatureOverrideQueryService
{
    private readonly IFeatureOverrideRepository _overrides;

    public FeatureOverrideQueryService(IFeatureOverrideRepository overrides)
    {
        _overrides = overrides;
    }

    public async Task<FeatureOverrideDto?> GetByIdAsync(Guid overrideId, CancellationToken cancellationToken = default)
    {
        var featureOverride = await _overrides
            .GetByIdAsync(FeatureOverrideId.From(overrideId), cancellationToken)
            .ConfigureAwait(false);
        return featureOverride is null ? null : Map(featureOverride);
    }

    public async Task<PagedResult<FeatureOverrideDto>> ListByOrganizationProductAsync(
        Guid organizationId,
        string productCode,
        FeatureOverrideStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, totalCount) = await _overrides
            .ListByOrganizationProductAsync(
                PlatformOrganizationId.From(organizationId),
                ProductCode.Create(productCode),
                status,
                skip,
                take,
                cancellationToken)
            .ConfigureAwait(false);

        var pageNumber = Math.Max(page ?? 1, 1);
        return new PagedResult<FeatureOverrideDto>(items.Select(Map).ToList(), totalCount, pageNumber, take);
    }

    private static FeatureOverrideDto Map(FeatureOverride featureOverride) =>
        new(
            featureOverride.Id.Value,
            featureOverride.OrganizationId.Value,
            featureOverride.ProductCode.Value,
            featureOverride.FeatureCode.Value,
            featureOverride.Enabled,
            featureOverride.NumericLimit,
            featureOverride.Reason,
            featureOverride.EffectiveFromUtc,
            featureOverride.ExpiresAtUtc,
            featureOverride.Status.ToString(),
            featureOverride.CreatedAtUtc,
            featureOverride.CreatedByUserId.Value,
            featureOverride.UpdatedAtUtc,
            featureOverride.RevokedAtUtc,
            featureOverride.RevokedByUserId?.Value,
            featureOverride.RevocationReason);
}
