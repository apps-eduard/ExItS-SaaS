using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Personal;

/// <summary>
/// Provider-neutral Personal ads configuration. No real ad network in WP09.
/// </summary>
public sealed class PersonalAdsOptions
{
    public const string SectionName = "PersonalAds";

    /// <summary>
    /// Provider selection placeholder. Only <c>None</c> is supported in WP09
    /// (no real network; no fake playback).
    /// </summary>
    public string ProviderMode { get; set; } = "None";

    /// <summary>
    /// When false, eligibility reports ads surface unavailable even without Ad-Free.
    /// Default true so free Personal remains eligible for a future provider.
    /// </summary>
    public bool SurfaceEnabled { get; set; } = true;
}

public sealed record PersonalAdEligibilityDto(
    Guid PersonalUserId,
    bool Eligible,
    bool AdFreeActive,
    bool ProviderConfigured,
    string? ReasonCode,
    string? ReasonMessage);

/// <summary>
/// Authoritative Personal ads eligibility (server-side). Ad-Free entitlement blocks ads.
/// </summary>
public sealed class GetPersonalAdEligibility
{
    private readonly IPersonalAdEligibility _eligibility;
    private readonly IPlatformUserRepository _users;
    private readonly IClock _clock;
    private readonly IOptions<PersonalAdsOptions> _adsOptions;
    private readonly IOptions<PersonalRewardClaimOptions> _claimOptions;

    public GetPersonalAdEligibility(
        IPersonalAdEligibility eligibility,
        IPlatformUserRepository users,
        IClock clock,
        IOptions<PersonalAdsOptions> adsOptions,
        IOptions<PersonalRewardClaimOptions> claimOptions)
    {
        _eligibility = eligibility;
        _users = users;
        _clock = clock;
        _adsOptions = adsOptions;
        _claimOptions = claimOptions;
    }

    public async Task<ApplicationResult<PersonalAdEligibilityDto>> ExecuteAsync(
        PlatformUserId personalUserId,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personalUserId);

        var orgGuard = OrganizationRewardRedemptionGuard.EnsurePersonalOnly(organizationId);
        if (!orgGuard.IsSuccess)
        {
            return ApplicationResult<PersonalAdEligibilityDto>.Failure(
                orgGuard.ErrorCode!,
                orgGuard.ErrorMessage!);
        }

        var user = await _users.GetByIdAsync(personalUserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<PersonalAdEligibilityDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Personal user was not found.");
        }

        var identityGuard = OrganizationRewardRedemptionGuard.EnsurePersonalIdentity(user);
        if (!identityGuard.IsSuccess)
        {
            return ApplicationResult<PersonalAdEligibilityDto>.Failure(
                identityGuard.ErrorCode!,
                identityGuard.ErrorMessage!);
        }

        var evaluation = await _eligibility
            .EvaluateAsync(personalUserId, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<PersonalAdEligibilityDto>.Success(
            new PersonalAdEligibilityDto(
                personalUserId.Value,
                evaluation.IsEligible,
                evaluation.AdFreeActive,
                ProviderConfigured: IsProviderConfigured(_adsOptions.Value, _claimOptions.Value),
                evaluation.DenialCode,
                evaluation.DenialMessage));
    }

    internal static bool IsProviderConfigured(PersonalAdsOptions ads, PersonalRewardClaimOptions claims)
    {
        // WP09: no real provider. Explicitly never report a configured production verifier.
        _ = ads;
        _ = claims;
        return false;
    }
}
