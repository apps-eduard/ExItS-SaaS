using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Contracts;

public sealed class FeatureGrantProjection
{
    public string FeatureCode { get; }
    public FeatureValueType ValueType { get; }
    public bool Enabled { get; }
    public int? NumericLimit { get; }
    public EntitlementGrantSource Source { get; }
    public DateTimeOffset EffectiveAtUtc { get; }
    public DateTimeOffset? ExpiresAtUtc { get; }

    public FeatureGrantProjection(
        FeatureCode featureCode,
        FeatureValueType valueType,
        bool enabled,
        EntitlementGrantSource source,
        DateTimeOffset effectiveAtUtc,
        int? numericLimit = null,
        DateTimeOffset? expiresAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(featureCode);
        if (effectiveAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ContractException(DomainErrorCodes.InvalidUtcTimestamp, "EffectiveAt must be UTC.");
        }

        if (expiresAtUtc is not null && expiresAtUtc.Value.Offset != TimeSpan.Zero)
        {
            throw new ContractException(DomainErrorCodes.InvalidUtcTimestamp, "ExpiresAt must be UTC.");
        }

        if (numericLimit is < 0)
        {
            throw new ContractException(DomainErrorCodes.InvalidEntitlementLimit, "Numeric limits cannot be negative.");
        }

        if (valueType == FeatureValueType.Boolean && numericLimit is not null)
        {
            throw new ContractException(
                DomainErrorCodes.InvalidEntitlementLimit,
                "Boolean features must not carry a numeric limit.");
        }

        FeatureCode = featureCode.Value;
        ValueType = valueType;
        Enabled = enabled;
        NumericLimit = numericLimit;
        Source = source;
        EffectiveAtUtc = effectiveAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }
}

/// <summary>Transport-independent entitlement snapshot projection for product consumers. Immutable.</summary>
public sealed class EntitlementSnapshotProjection
{
    private readonly IReadOnlyList<FeatureGrantProjection> _grants;

    public Guid SnapshotId { get; }
    public int SnapshotVersion { get; }
    public ContractVersion SchemaVersion { get; }
    public PlatformOrganizationId PlatformOrganizationId { get; }
    public ProductCode ProductCode { get; }
    public SubscriptionId SubscriptionId { get; }
    public SubscriptionStatus SubscriptionStatus { get; }
    public string PlanCode { get; }
    public int PlanVersionNumber { get; }
    public DateTimeOffset GeneratedAtUtc { get; }
    public DateTimeOffset EffectiveAtUtc { get; }
    public DateTimeOffset RefreshByUtc { get; }
    public bool InGracePeriod { get; }
    public int SourceAggregateVersion { get; }

    public IReadOnlyList<FeatureGrantProjection> Grants => _grants;

    public EntitlementSnapshotProjection(
        Guid snapshotId,
        int snapshotVersion,
        ContractVersion schemaVersion,
        PlatformOrganizationId platformOrganizationId,
        ProductCode productCode,
        SubscriptionId subscriptionId,
        SubscriptionStatus subscriptionStatus,
        PlanCode planCode,
        int planVersionNumber,
        DateTimeOffset generatedAtUtc,
        DateTimeOffset effectiveAtUtc,
        DateTimeOffset refreshByUtc,
        bool inGracePeriod,
        int sourceAggregateVersion,
        IReadOnlyList<FeatureGrantProjection> grants)
    {
        ArgumentNullException.ThrowIfNull(platformOrganizationId);
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(subscriptionId);
        ArgumentNullException.ThrowIfNull(planCode);
        ArgumentNullException.ThrowIfNull(grants);

        if (snapshotId == Guid.Empty)
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Snapshot ID cannot be empty.");
        }

        if (snapshotVersion < 1 || sourceAggregateVersion < 1 || planVersionNumber < 1)
        {
            throw new ContractException(ContractErrorCodes.InvalidSourceVersion, "Versions must be positive.");
        }

        EnsureUtc(generatedAtUtc);
        EnsureUtc(effectiveAtUtc);
        EnsureUtc(refreshByUtc);

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var grant in grants)
        {
            if (!seen.Add(grant.FeatureCode))
            {
                throw new ContractException(
                    DomainErrorCodes.DuplicateFeatureCode,
                    $"Duplicate feature code in entitlement projection: {grant.FeatureCode}.");
            }
        }

        SnapshotId = snapshotId;
        SnapshotVersion = snapshotVersion;
        SchemaVersion = schemaVersion;
        PlatformOrganizationId = platformOrganizationId;
        ProductCode = productCode;
        SubscriptionId = subscriptionId;
        SubscriptionStatus = subscriptionStatus;
        PlanCode = planCode.Value;
        PlanVersionNumber = planVersionNumber;
        GeneratedAtUtc = generatedAtUtc;
        EffectiveAtUtc = effectiveAtUtc;
        RefreshByUtc = refreshByUtc;
        InGracePeriod = inGracePeriod;
        SourceAggregateVersion = sourceAggregateVersion;
        _grants = grants.ToList().AsReadOnly();
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ContractException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}
