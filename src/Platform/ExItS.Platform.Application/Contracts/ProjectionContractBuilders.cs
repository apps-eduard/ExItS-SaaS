using ExItS.Platform.Application.Contracts;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Contracts;

/// <summary>Deterministic builders from Platform domain aggregates to outbound projection contracts.</summary>
public sealed class ProjectionContractBuilders
{
    private readonly IClock _clock;

    public ProjectionContractBuilders(IClock clock) => _clock = clock;

    public PlatformUserProjection BuildUser(PlatformUser user) =>
        new(user.Id, user.DisplayName, user.NormalizedEmail, user.Status, user.UpdatedAtUtc, sourceVersion: 1);

    public OrganizationMembershipProjection BuildMembership(OrganizationMembership membership) =>
        new(
            membership.OrganizationId,
            membership.UserId,
            membership.Status,
            membership.Role,
            membership.UpdatedAtUtc,
            sourceVersion: 1);

    public ProductAccessProjection BuildProductAccess(ProductAccess access) =>
        new(
            access.OrganizationId,
            access.ProductCode,
            access.Status,
            access.UpdatedAtUtc,
            sourceVersion: 1,
            access.UserId,
            revokedAtUtc: access.Status == ProductAccessStatus.Revoked ? access.UpdatedAtUtc : null);

    public SubscriptionProjection BuildSubscription(Subscription subscription, PlanCode planCode, int planVersionNumber) =>
        new(
            subscription.OrganizationId,
            subscription.ProductCode,
            subscription.Id,
            planCode,
            planVersionNumber,
            subscription.Status,
            subscription.UpdatedAtUtc,
            subscription.Version,
            subscription.TrialStartUtc,
            subscription.TrialEndUtc,
            subscription.PaidPeriodStartUtc,
            subscription.PaidPeriodEndUtc,
            subscription.GracePeriodEndUtc);

    public EntitlementSnapshotProjection BuildEntitlementSnapshot(
        EntitlementSnapshot snapshot,
        IReadOnlyDictionary<string, FeatureValueType> featureValueTypes)
    {
        var grants = snapshot.Grants.Select(g =>
        {
            if (!featureValueTypes.TryGetValue(g.FeatureCode.Value, out var valueType))
            {
                valueType = g.NumericLimit is null ? FeatureValueType.Boolean : FeatureValueType.NumericLimit;
            }

            return new FeatureGrantProjection(
                g.FeatureCode,
                valueType,
                g.Enabled,
                g.Source,
                g.EffectiveAtUtc,
                g.NumericLimit,
                g.ExpiresAtUtc);
        }).ToList();

        return new EntitlementSnapshotProjection(
            snapshot.Id.Value,
            snapshot.SnapshotVersion,
            ContractVersion.Create(snapshot.SchemaVersion),
            snapshot.OrganizationId,
            snapshot.ProductCode,
            snapshot.SubscriptionId,
            snapshot.SubscriptionStatus,
            snapshot.PlanCode,
            snapshot.PlanVersionNumber,
            snapshot.GeneratedAtUtc,
            snapshot.EffectiveAtUtc,
            snapshot.RefreshByUtc,
            snapshot.InGracePeriod,
            snapshot.SourceAggregateVersion,
            grants);
    }

    public ContractEnvelope<T> Wrap<T>(
        string contractName,
        T payload,
        string sourceAggregateId,
        int sourceAggregateVersion,
        Guid messageId,
        Guid correlationId,
        PlatformOrganizationId? organizationId = null,
        ProductCode? productCode = null,
        Guid? causationId = null,
        DateTimeOffset? occurredAtUtc = null)
    {
        var now = _clock.UtcNow;
        return ContractEnvelope<T>.Create(
            contractName,
            ContractVersion.V1,
            messageId,
            correlationId,
            occurredAtUtc ?? now,
            now,
            ContractSourceSystems.ExItsPlatform,
            sourceAggregateId,
            sourceAggregateVersion,
            payload,
            causationId,
            organizationId,
            productCode);
    }
}
