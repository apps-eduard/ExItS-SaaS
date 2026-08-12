using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public interface IPersonalRewardClaimRepository
{
    Task<PersonalRewardClaim?> FindByUserTypeAndKeyAsync(
        PlatformUserId personalUserId,
        string claimType,
        string claimKey,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalRewardClaim claim, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provider-neutral rewarded-ad claim verification. Null/stub implementation first (WP08);
/// real ad network wiring is WP09.
/// </summary>
public interface IRewardedAdClaimVerifier
{
    Task<RewardedAdClaimVerification> VerifyAsync(
        PlatformUserId personalUserId,
        string claimKey,
        CancellationToken cancellationToken = default);
}

public sealed record RewardedAdClaimVerification(
    bool IsValid,
    int? Points,
    string? ErrorCode,
    string? ErrorMessage,
    string? ProviderReference = null);

/// <summary>Whether the Personal user may be offered rewarded ads / claim ad rewards.</summary>
public interface IPersonalAdEligibility
{
    Task<PersonalAdEligibilityResult> EvaluateAsync(
        PlatformUserId personalUserId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);
}

public sealed record PersonalAdEligibilityResult(
    bool IsEligible,
    bool AdFreeActive,
    string? DenialCode,
    string? DenialMessage);

/// <summary>
/// Development/test defaults for Personal reward earning. Not production economics (WP11).
/// </summary>
public sealed class PersonalRewardClaimOptions
{
    public const string SectionName = "PersonalRewardClaims";

    /// <summary>Server-side points awarded per verified AdReward claim.</summary>
    public int AdRewardPoints { get; set; } = 10;

    /// <summary>
    /// Unsafe test-only switch. When true, <see cref="NullRewardedAdClaimVerifier"/> may accept
    /// well-formed claim keys without a real provider. Must remain <c>false</c> in runtime config (WP09).
    /// </summary>
    public bool NullProviderClaimsEnabled { get; set; } = false;
}

/// <summary>
/// Organization commercial features and organization-context callers never redeem Personal reward points.
/// </summary>
public static class OrganizationRewardRedemptionGuard
{
    public static ApplicationResult EnsurePersonalOnly(Guid? organizationId)
    {
        if (organizationId is Guid orgId && orgId != Guid.Empty)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported,
                "Organization context cannot redeem Personal reward points.");
        }

        return ApplicationResult.Success();
    }

    public static ApplicationResult EnsurePersonalIdentity(PlatformUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (user.IsOrganizationScopedStaff
            || user.HomeOrganizationId is not null
            || !string.IsNullOrWhiteSpace(user.StaffNumber))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported,
                "Organization-scoped identities cannot redeem or earn Personal reward points.");
        }

        return ApplicationResult.Success();
    }

    /// <summary>
    /// Organization plan/add-on unlock sources must never be RewardPoints (ADR-021).
    /// </summary>
    public static ApplicationResult RejectOrganizationFeatureRewardPoints(string? unlockSource)
    {
        if (string.Equals(unlockSource, nameof(PersonalFeatureGrantSource.RewardPoints), StringComparison.OrdinalIgnoreCase)
            || string.Equals(unlockSource, PersonalRewardSources.FeatureRedemption, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.OrganizationRewardRedemptionUnsupported,
                "Organization features reject RewardPoints unlocks.");
        }

        return ApplicationResult.Success();
    }
}
