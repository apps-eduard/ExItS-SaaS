using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>Frozen Personal-only feature codes (hyphenated; never dotted).</summary>
public static class PersonalFeatureCodes
{
    /// <summary>Unlocks older settled Business Utang history and settled receipt detail beyond the free window.</summary>
    public const string DigitalRecordsExtended = "personal-digital-records-extended";

    /// <summary>Ad-Free Personal entitlement (WP09). Used by WP08 eligibility to skip rewarded-ad earning.</summary>
    public const string AdFree = "personal-ad-free";

    /// <summary>
    /// Development/test default reward-point price for digital-records-extended.
    /// Not a production launch price — Admin/config owns economics later (WP11).
    /// </summary>
    public const int DigitalRecordsExtendedDefaultRewardPoints = 100;

    public static FeatureCode DigitalRecordsExtendedCode { get; } = FeatureCode.Create(DigitalRecordsExtended);
    public static FeatureCode AdFreeCode { get; } = FeatureCode.Create(AdFree);
}

public enum PersonalFeatureEntitlementStatus
{
    Active = 1,
    Revoked = 2
}

/// <summary>How a Personal feature was granted.</summary>
public enum PersonalFeatureGrantSource
{
    CashPurchase = 1,
    RewardPoints = 2,
    Promotion = 3,
    AdminGrant = 4
}

/// <summary>Catalog row for Personal-scoped features (not Organization plan features).</summary>
public sealed class PersonalFeatureDefinition
{
    public FeatureCode FeatureCode { get; }
    public string DisplayName { get; }
    public bool IsActive { get; private set; }

    /// <summary>
    /// Reward-point redemption price. Null means the feature is not redeemable with points.
    /// Development defaults only — not production pricing.
    /// </summary>
    public int? RewardPointsPrice { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private PersonalFeatureDefinition(
        FeatureCode featureCode,
        string displayName,
        bool isActive,
        int? rewardPointsPrice,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        FeatureCode = featureCode;
        DisplayName = displayName;
        IsActive = isActive;
        RewardPointsPrice = rewardPointsPrice;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static PersonalFeatureDefinition Create(
        FeatureCode featureCode,
        string displayName,
        DateTimeOffset utcNow,
        bool isActive = true,
        int? rewardPointsPrice = null)
    {
        ArgumentNullException.ThrowIfNull(featureCode);
        EnsureUtc(utcNow);
        var name = NormalizeDisplayName(displayName);
        EnsureRewardPrice(rewardPointsPrice);
        return new PersonalFeatureDefinition(featureCode, name, isActive, rewardPointsPrice, utcNow, utcNow);
    }

    public static PersonalFeatureDefinition Rehydrate(
        FeatureCode featureCode,
        string displayName,
        bool isActive,
        int? rewardPointsPrice,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(featureCode, displayName, isActive, rewardPointsPrice, createdAtUtc, updatedAtUtc);

    public bool IsRewardRedeemable => IsActive && RewardPointsPrice is > 0;

    public void SetActive(bool isActive, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        IsActive = isActive;
        UpdatedAtUtc = utcNow;
    }

    public void SetRewardPointsPrice(int? rewardPointsPrice, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureRewardPrice(rewardPointsPrice);
        RewardPointsPrice = rewardPointsPrice;
        UpdatedAtUtc = utcNow;
    }

    private static void EnsureRewardPrice(int? rewardPointsPrice)
    {
        if (rewardPointsPrice is < 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalRewardPoints,
                "Reward points price must be null or a positive integer.");
        }
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Personal feature display name is required.");
        }

        var trimmed = displayName.Trim();
        if (trimmed.Length > 200)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Personal feature display name must be 200 characters or fewer.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Personal feature timestamps must be UTC.");
        }
    }
}

/// <summary>
/// Personal-user entitlement grant. Subject is <see cref="PersonalUserId"/> — never OrganizationId.
/// </summary>
public sealed class PersonalFeatureEntitlement
{
    public Guid Id { get; }
    public PlatformUserId PersonalUserId { get; }
    public FeatureCode FeatureCode { get; }
    public DateTimeOffset StartsAtUtc { get; }
    public DateTimeOffset? EndsAtUtc { get; private set; }
    public PersonalFeatureEntitlementStatus Status { get; private set; }
    public PersonalFeatureGrantSource GrantSource { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevocationReason { get; private set; }

    private PersonalFeatureEntitlement(
        Guid id,
        PlatformUserId personalUserId,
        FeatureCode featureCode,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? endsAtUtc,
        PersonalFeatureEntitlementStatus status,
        PersonalFeatureGrantSource grantSource,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? revokedAtUtc,
        string? revocationReason)
    {
        Id = id;
        PersonalUserId = personalUserId;
        FeatureCode = featureCode;
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
        Status = status;
        GrantSource = grantSource;
        CreatedAtUtc = createdAtUtc;
        RevokedAtUtc = revokedAtUtc;
        RevocationReason = revocationReason;
    }

    public static PersonalFeatureEntitlement Grant(
        PlatformUserId personalUserId,
        FeatureCode featureCode,
        PersonalFeatureGrantSource grantSource,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? endsAtUtc,
        DateTimeOffset utcNow,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(personalUserId);
        ArgumentNullException.ThrowIfNull(featureCode);
        EnsureUtc(startsAtUtc);
        EnsureUtc(utcNow);
        if (endsAtUtc is DateTimeOffset end)
        {
            EnsureUtc(end);
            if (end < startsAtUtc)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidUtcTimestamp,
                    "Personal feature EndsAtUtc cannot be before StartsAtUtc.");
            }
        }

        if (!Enum.IsDefined(grantSource))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalFeatureGrantSource,
                "Unrecognized Personal feature grant source.");
        }

        return new PersonalFeatureEntitlement(
            id ?? Guid.NewGuid(),
            personalUserId,
            featureCode,
            startsAtUtc,
            endsAtUtc,
            PersonalFeatureEntitlementStatus.Active,
            grantSource,
            utcNow,
            revokedAtUtc: null,
            revocationReason: null);
    }

    public static PersonalFeatureEntitlement Rehydrate(
        Guid id,
        PlatformUserId personalUserId,
        FeatureCode featureCode,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? endsAtUtc,
        PersonalFeatureEntitlementStatus status,
        PersonalFeatureGrantSource grantSource,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? revokedAtUtc,
        string? revocationReason) =>
        new(
            id,
            personalUserId,
            featureCode,
            startsAtUtc,
            endsAtUtc,
            status,
            grantSource,
            createdAtUtc,
            revokedAtUtc,
            revocationReason);

    public bool IsActiveAt(DateTimeOffset asOfUtc)
    {
        EnsureUtc(asOfUtc);
        if (Status != PersonalFeatureEntitlementStatus.Active)
        {
            return false;
        }

        if (asOfUtc < StartsAtUtc)
        {
            return false;
        }

        if (EndsAtUtc is DateTimeOffset end && asOfUtc > end)
        {
            return false;
        }

        return true;
    }

    public void Revoke(string? reason, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == PersonalFeatureEntitlementStatus.Revoked)
        {
            return;
        }

        Status = PersonalFeatureEntitlementStatus.Revoked;
        RevokedAtUtc = utcNow;
        RevocationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (RevocationReason is { Length: > 512 })
        {
            RevocationReason = RevocationReason[..512];
        }
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Personal feature timestamps must be UTC.");
        }
    }
}
