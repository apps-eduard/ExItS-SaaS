using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Domain.Entitlements;

/// <summary>Organization feature override. Does not mutate historical plan versions.</summary>
public sealed class FeatureOverride
{
    public FeatureOverrideId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public ProductCode ProductCode { get; }
    public FeatureCode FeatureCode { get; }
    public bool Enabled { get; private set; }
    public int? NumericLimit { get; private set; }
    public string Reason { get; }
    public DateTimeOffset EffectiveFromUtc { get; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public FeatureOverrideStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public PlatformUserId CreatedByUserId { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private FeatureOverride(
        FeatureOverrideId id,
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        FeatureCode featureCode,
        bool enabled,
        int? numericLimit,
        string reason,
        DateTimeOffset effectiveFromUtc,
        DateTimeOffset? expiresAtUtc,
        FeatureOverrideStatus status,
        DateTimeOffset createdAtUtc,
        PlatformUserId createdByUserId,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        ProductCode = productCode;
        FeatureCode = featureCode;
        Enabled = enabled;
        NumericLimit = numericLimit;
        Reason = reason;
        EffectiveFromUtc = effectiveFromUtc;
        ExpiresAtUtc = expiresAtUtc;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        CreatedByUserId = createdByUserId;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static FeatureOverride Create(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        FeatureDefinition feature,
        bool enabled,
        string reason,
        PlatformUserId createdByUserId,
        DateTimeOffset utcNow,
        int? numericLimit = null,
        DateTimeOffset? expiresAtUtc = null,
        FeatureOverrideId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(productCode);
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(createdByUserId);
        DomainTime.EnsureUtc(utcNow);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.OverrideReasonRequired,
                "Feature override reason is required.");
        }

        if (feature.ProductCode != productCode)
        {
            throw new DomainException(
                DomainErrorCodes.ProductMismatch,
                "Override cannot target another product's feature.");
        }

        feature.EnsureAssignable();

        if (feature.ValueType == FeatureValueType.Boolean && numericLimit is not null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEntitlementLimit,
                "Boolean features must not carry a numeric limit.");
        }

        if (feature.ValueType is FeatureValueType.NumericLimit or FeatureValueType.QuantityLimit
            && numericLimit is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEntitlementLimit,
                "Limit features require a non-negative numeric limit.");
        }

        if (numericLimit is < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEntitlementLimit,
                "Numeric limits cannot be negative.");
        }

        if (expiresAtUtc is not null)
        {
            DomainTime.EnsureUtc(expiresAtUtc.Value);
            if (expiresAtUtc.Value <= utcNow)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidEffectiveRange,
                    "Override expiration must be after creation time.");
            }
        }

        return new FeatureOverride(
            id ?? FeatureOverrideId.New(),
            organizationId,
            productCode,
            feature.Code,
            enabled,
            numericLimit,
            reason.Trim(),
            utcNow,
            expiresAtUtc,
            FeatureOverrideStatus.Active,
            utcNow,
            createdByUserId,
            utcNow);
    }

    public bool IsActiveAt(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status != FeatureOverrideStatus.Active)
        {
            return false;
        }

        if (utcNow < EffectiveFromUtc)
        {
            return false;
        }

        if (ExpiresAtUtc is not null && utcNow >= ExpiresAtUtc.Value)
        {
            return false;
        }

        return true;
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == FeatureOverrideStatus.Revoked)
        {
            return;
        }

        Status = FeatureOverrideStatus.Revoked;
        UpdatedAtUtc = utcNow;
    }
}
