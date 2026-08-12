using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>Frozen Personal-only feature codes (hyphenated; never dotted).</summary>
public static class PersonalFeatureCodes
{
    /// <summary>Unlocks older settled Business Utang history and settled receipt detail beyond the free window.</summary>
    public const string DigitalRecordsExtended = "personal-digital-records-extended";

    public static FeatureCode DigitalRecordsExtendedCode { get; } = FeatureCode.Create(DigitalRecordsExtended);
}

public enum PersonalFeatureEntitlementStatus
{
    Active = 1,
    Revoked = 2
}

/// <summary>
/// How a Personal feature was granted. RewardPoints is reserved for WP07 — not redeemable in WP06.
/// </summary>
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
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private PersonalFeatureDefinition(
        FeatureCode featureCode,
        string displayName,
        bool isActive,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        FeatureCode = featureCode;
        DisplayName = displayName;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static PersonalFeatureDefinition Create(
        FeatureCode featureCode,
        string displayName,
        DateTimeOffset utcNow,
        bool isActive = true)
    {
        ArgumentNullException.ThrowIfNull(featureCode);
        EnsureUtc(utcNow);
        var name = NormalizeDisplayName(displayName);
        return new PersonalFeatureDefinition(featureCode, name, isActive, utcNow, utcNow);
    }

    public static PersonalFeatureDefinition Rehydrate(
        FeatureCode featureCode,
        string displayName,
        bool isActive,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(featureCode, displayName, isActive, createdAtUtc, updatedAtUtc);

    public void SetActive(bool isActive, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        IsActive = isActive;
        UpdatedAtUtc = utcNow;
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
