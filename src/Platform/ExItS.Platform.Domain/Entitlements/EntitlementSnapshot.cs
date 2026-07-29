using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Domain.Entitlements;

/// <summary>
/// Authoritative entitlement snapshot for later product-local projection.
/// Immutable after creation. Does not contain clinical or retail operational data.
/// </summary>
public sealed class EntitlementSnapshot
{
    private readonly List<EntitlementGrant> _grants;

    public EntitlementSnapshotId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public ProductCode ProductCode { get; }
    public SubscriptionId SubscriptionId { get; }
    public PlanCode PlanCode { get; }
    public int PlanVersionNumber { get; }
    public int SnapshotVersion { get; }
    public int SchemaVersion { get; }
    public SubscriptionStatus SubscriptionStatus { get; }
    public bool InGracePeriod { get; }
    public DateTimeOffset GeneratedAtUtc { get; }
    public DateTimeOffset EffectiveAtUtc { get; }
    public DateTimeOffset RefreshByUtc { get; }
    public DateTimeOffset? ExpiresAtUtc { get; }
    public int SourceAggregateVersion { get; }

    public IReadOnlyList<EntitlementGrant> Grants => _grants;

    private EntitlementSnapshot(
        EntitlementSnapshotId id,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        SubscriptionId subscriptionId,
        PlanCode planCode,
        int planVersionNumber,
        int snapshotVersion,
        int schemaVersion,
        SubscriptionStatus subscriptionStatus,
        bool inGracePeriod,
        DateTimeOffset generatedAtUtc,
        DateTimeOffset effectiveAtUtc,
        DateTimeOffset refreshByUtc,
        DateTimeOffset? expiresAtUtc,
        int sourceAggregateVersion,
        IEnumerable<EntitlementGrant> grants)
    {
        Id = id;
        OrganizationId = organizationId;
        ProductCode = productCode;
        SubscriptionId = subscriptionId;
        PlanCode = planCode;
        PlanVersionNumber = planVersionNumber;
        SnapshotVersion = snapshotVersion;
        SchemaVersion = schemaVersion;
        SubscriptionStatus = subscriptionStatus;
        InGracePeriod = inGracePeriod;
        GeneratedAtUtc = generatedAtUtc;
        EffectiveAtUtc = effectiveAtUtc;
        RefreshByUtc = refreshByUtc;
        ExpiresAtUtc = expiresAtUtc;
        SourceAggregateVersion = sourceAggregateVersion;
        _grants = grants.ToList();
    }

    public const int CurrentSchemaVersion = 1;

    public static EntitlementSnapshot Create(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        SubscriptionId subscriptionId,
        PlanCode planCode,
        int planVersionNumber,
        int snapshotVersion,
        SubscriptionStatus subscriptionStatus,
        bool inGracePeriod,
        DateTimeOffset generatedAtUtc,
        DateTimeOffset effectiveAtUtc,
        DateTimeOffset refreshByUtc,
        int sourceAggregateVersion,
        IReadOnlyList<EntitlementGrant> grants,
        EntitlementSnapshotId? id = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(subscriptionId);
        ArgumentNullException.ThrowIfNull(planCode);
        ArgumentNullException.ThrowIfNull(grants);
        DomainTime.EnsureUtc(generatedAtUtc);
        DomainTime.EnsureUtc(effectiveAtUtc);
        DomainTime.EnsureUtc(refreshByUtc);
        if (expiresAtUtc is not null)
        {
            DomainTime.EnsureUtc(expiresAtUtc.Value);
            if (expiresAtUtc.Value < effectiveAtUtc)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidEffectiveRange,
                    "Snapshot expiry cannot precede its effective time.");
            }
        }

        if (snapshotVersion < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSnapshotVersion,
                "Snapshot version must be positive.");
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grant in grants)
        {
            if (!seen.Add(grant.FeatureCode.Value))
            {
                throw new DomainException(
                    DomainErrorCodes.DuplicateFeatureCode,
                    $"Duplicate feature code in snapshot: {grant.FeatureCode}.");
            }

            if (grant.NumericLimit is < 0)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidEntitlementLimit,
                    "Numeric limits cannot be negative.");
            }
        }

        return new EntitlementSnapshot(
            id ?? EntitlementSnapshotId.New(),
            organizationId,
            productCode,
            subscriptionId,
            planCode,
            planVersionNumber,
            snapshotVersion,
            CurrentSchemaVersion,
            subscriptionStatus,
            inGracePeriod,
            generatedAtUtc,
            effectiveAtUtc,
            refreshByUtc,
            expiresAtUtc,
            sourceAggregateVersion,
            grants);
    }

    /// <summary>EF rehydration only. Bypasses creation invariants for a row already persisted.</summary>
    internal static EntitlementSnapshot Rehydrate(
        EntitlementSnapshotId id,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        SubscriptionId subscriptionId,
        PlanCode planCode,
        int planVersionNumber,
        int snapshotVersion,
        int schemaVersion,
        SubscriptionStatus subscriptionStatus,
        bool inGracePeriod,
        DateTimeOffset generatedAtUtc,
        DateTimeOffset effectiveAtUtc,
        DateTimeOffset refreshByUtc,
        DateTimeOffset? expiresAtUtc,
        int sourceAggregateVersion,
        IEnumerable<EntitlementGrant> grants) =>
        new(
            id,
            organizationId,
            productCode,
            subscriptionId,
            planCode,
            planVersionNumber,
            snapshotVersion,
            schemaVersion,
            subscriptionStatus,
            inGracePeriod,
            generatedAtUtc,
            effectiveAtUtc,
            refreshByUtc,
            expiresAtUtc,
            sourceAggregateVersion,
            grants);
}
