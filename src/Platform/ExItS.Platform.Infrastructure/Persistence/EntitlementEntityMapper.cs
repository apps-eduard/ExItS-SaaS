using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Persistence.Entitlements;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class EntitlementEntityMapper
{
    public static FeatureOverride ToDomain(FeatureOverrideRecord record) =>
        FeatureOverride.Rehydrate(
            FeatureOverrideId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            ProductCode.Create(record.ProductCode),
            FeatureCode.Create(record.FeatureCode),
            record.Enabled,
            record.NumericLimit,
            record.Reason,
            record.EffectiveFromUtc,
            record.ExpiresAtUtc,
            Enum.Parse<FeatureOverrideStatus>(record.Status),
            record.CreatedAtUtc,
            PlatformUserId.From(record.CreatedByUserId),
            record.UpdatedAtUtc,
            record.RevokedAtUtc,
            record.RevokedByUserId is null ? null : PlatformUserId.From(record.RevokedByUserId.Value),
            record.RevocationReason);

    public static FeatureOverrideRecord ToRecord(FeatureOverride featureOverride) =>
        new()
        {
            Id = featureOverride.Id.Value,
            OrganizationId = featureOverride.OrganizationId.Value,
            ProductCode = featureOverride.ProductCode.Value,
            FeatureCode = featureOverride.FeatureCode.Value,
            Enabled = featureOverride.Enabled,
            NumericLimit = featureOverride.NumericLimit,
            Reason = featureOverride.Reason,
            EffectiveFromUtc = featureOverride.EffectiveFromUtc,
            ExpiresAtUtc = featureOverride.ExpiresAtUtc,
            Status = featureOverride.Status.ToString(),
            CreatedAtUtc = featureOverride.CreatedAtUtc,
            CreatedByUserId = featureOverride.CreatedByUserId.Value,
            UpdatedAtUtc = featureOverride.UpdatedAtUtc,
            RevokedAtUtc = featureOverride.RevokedAtUtc,
            RevokedByUserId = featureOverride.RevokedByUserId?.Value,
            RevocationReason = featureOverride.RevocationReason
        };

    public static void ApplyToRecord(FeatureOverride featureOverride, FeatureOverrideRecord record)
    {
        record.Status = featureOverride.Status.ToString();
        record.UpdatedAtUtc = featureOverride.UpdatedAtUtc;
        record.RevokedAtUtc = featureOverride.RevokedAtUtc;
        record.RevokedByUserId = featureOverride.RevokedByUserId?.Value;
        record.RevocationReason = featureOverride.RevocationReason;
    }

    public static EntitlementSnapshot ToDomain(EntitlementSnapshotRecord record) =>
        EntitlementSnapshot.Rehydrate(
            EntitlementSnapshotId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            ProductCode.Create(record.ProductCode),
            SubscriptionId.From(record.SubscriptionId),
            PlanCode.Create(record.PlanCode),
            record.PlanVersionNumber,
            record.SnapshotVersion,
            record.SchemaVersion,
            Enum.Parse<SubscriptionStatus>(record.SubscriptionStatus),
            record.InGracePeriod,
            record.GeneratedAtUtc,
            record.EffectiveAtUtc,
            record.RefreshByUtc,
            record.ExpiresAtUtc,
            record.SourceAggregateVersion,
            record.Grants
                .OrderBy(g => g.FeatureCode, StringComparer.Ordinal)
                .Select(ToDomain));

    private static EntitlementGrant ToDomain(EntitlementSnapshotGrantRecord record) =>
        new(
            FeatureCode.Create(record.FeatureCode),
            record.Enabled,
            Enum.Parse<EntitlementGrantSource>(record.Source),
            record.EffectiveAtUtc,
            record.NumericLimit,
            record.ExpiresAtUtc);

    public static EntitlementSnapshotRecord ToRecord(EntitlementSnapshot snapshot)
    {
        var record = new EntitlementSnapshotRecord
        {
            Id = snapshot.Id.Value,
            OrganizationId = snapshot.OrganizationId.Value,
            ProductCode = snapshot.ProductCode.Value,
            SubscriptionId = snapshot.SubscriptionId.Value,
            PlanCode = snapshot.PlanCode.Value,
            PlanVersionNumber = snapshot.PlanVersionNumber,
            SnapshotVersion = snapshot.SnapshotVersion,
            SchemaVersion = snapshot.SchemaVersion,
            SubscriptionStatus = snapshot.SubscriptionStatus.ToString(),
            InGracePeriod = snapshot.InGracePeriod,
            GeneratedAtUtc = snapshot.GeneratedAtUtc,
            EffectiveAtUtc = snapshot.EffectiveAtUtc,
            RefreshByUtc = snapshot.RefreshByUtc,
            ExpiresAtUtc = snapshot.ExpiresAtUtc,
            SourceAggregateVersion = snapshot.SourceAggregateVersion,
            CreatedAtUtc = snapshot.GeneratedAtUtc
        };

        record.Grants = snapshot.Grants.Select(g => new EntitlementSnapshotGrantRecord
        {
            SnapshotId = snapshot.Id.Value,
            FeatureCode = g.FeatureCode.Value,
            Enabled = g.Enabled,
            NumericLimit = g.NumericLimit,
            Source = g.Source.ToString(),
            EffectiveAtUtc = g.EffectiveAtUtc,
            ExpiresAtUtc = g.ExpiresAtUtc
        }).ToList();

        return record;
    }
}
