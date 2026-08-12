using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Personal;

public sealed record PersonalAdRewardClaimResultDto(
    Guid PersonalUserId,
    string ClaimType,
    string ClaimKey,
    bool AlreadyClaimed,
    int PointsAwarded,
    int AvailablePoints,
    Guid RewardTransactionId,
    DateTimeOffset ClaimedAtUtc);

/// <summary>
/// Null/disabled rewarded-ad verifier. Does not call a real ad network and does not
/// fabricate playback proof. Default runtime behavior rejects all claims (WP09).
/// </summary>
public sealed class NullRewardedAdClaimVerifier(IOptions<PersonalRewardClaimOptions> options) : IRewardedAdClaimVerifier
{
    public Task<RewardedAdClaimVerification> VerifyAsync(
        PlatformUserId personalUserId,
        string claimKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personalUserId);
        _ = cancellationToken;

        try
        {
            PersonalRewardClaim.NormalizeClaimKey(claimKey);
        }
        catch (DomainException ex)
        {
            return Task.FromResult(new RewardedAdClaimVerification(
                IsValid: false,
                Points: null,
                ApplicationErrorCodes.PersonalRewardClaimInvalid,
                ex.Message));
        }

        var opts = options.Value;
        // Explicit: null provider never claims a real verification. The opt-in flag is
        // test-only structural compatibility from WP08 and must stay false in appsettings.
        if (!opts.NullProviderClaimsEnabled)
        {
            return Task.FromResult(new RewardedAdClaimVerification(
                IsValid: false,
                Points: null,
                ApplicationErrorCodes.PersonalRewardClaimProviderUnavailable,
                "Rewarded-ad claim provider is not configured. No fake playback is available."));
        }

        var points = opts.AdRewardPoints;
        if (points <= 0)
        {
            return Task.FromResult(new RewardedAdClaimVerification(
                IsValid: false,
                Points: null,
                ApplicationErrorCodes.PersonalRewardClaimNotEligible,
                "Ad reward points are not configured."));
        }

        return Task.FromResult(new RewardedAdClaimVerification(
            IsValid: true,
            Points: points,
            ErrorCode: null,
            ErrorMessage: null,
            ProviderReference: null));
    }
}

/// <summary>
/// Free Personal users may be offered rewarded ads. Active Ad-Free entitlement makes ads ineligible.
/// Critical debt/security surfaces must never require ads (product rule; not enforced here as UI).
/// </summary>
public sealed class DefaultPersonalAdEligibility(
    IPersonalFeatureEntitlementService entitlements,
    IOptions<PersonalAdsOptions> adsOptions) : IPersonalAdEligibility
{
    public async Task<PersonalAdEligibilityResult> EvaluateAsync(
        PlatformUserId personalUserId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var adFree = await entitlements
            .HasActiveEntitlementAsync(personalUserId, PersonalFeatureCodes.AdFree, asOfUtc, cancellationToken)
            .ConfigureAwait(false);
        if (adFree)
        {
            return new PersonalAdEligibilityResult(
                IsEligible: false,
                AdFreeActive: true,
                ApplicationErrorCodes.PersonalAdsAdFreeActive,
                "Ad-Free entitlement is active; rewarded ads are not available.");
        }

        if (!adsOptions.Value.SurfaceEnabled)
        {
            return new PersonalAdEligibilityResult(
                IsEligible: false,
                AdFreeActive: false,
                ApplicationErrorCodes.PersonalAdsNotEligible,
                "Personal ads surface is disabled.");
        }

        return new PersonalAdEligibilityResult(
            IsEligible: true,
            AdFreeActive: false,
            DenialCode: null,
            DenialMessage: null);
    }
}

/// <summary>
/// Idempotent AdReward earning claim. Credits Personal reward points exactly once per
/// (PersonalUserId, ClaimType, ClaimKey). No client-controlled points amount.
/// </summary>
public sealed class ClaimPersonalAdReward
{
    private readonly IPersonalRewardClaimRepository _claims;
    private readonly IPersonalRewardBalanceRepository _balances;
    private readonly IPersonalRewardTransactionRepository _transactions;
    private readonly IRewardedAdClaimVerifier _verifier;
    private readonly IPersonalAdEligibility _eligibility;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClaimPersonalAdReward(
        IPersonalRewardClaimRepository claims,
        IPersonalRewardBalanceRepository balances,
        IPersonalRewardTransactionRepository transactions,
        IRewardedAdClaimVerifier verifier,
        IPersonalAdEligibility eligibility,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _claims = claims;
        _balances = balances;
        _transactions = transactions;
        _verifier = verifier;
        _eligibility = eligibility;
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalAdRewardClaimResultDto>> ExecuteAsync(
        PlatformUserId personalUserId,
        string claimKey,
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(personalUserId);

        var orgGuard = OrganizationRewardRedemptionGuard.EnsurePersonalOnly(organizationId);
        if (!orgGuard.IsSuccess)
        {
            return ApplicationResult<PersonalAdRewardClaimResultDto>.Failure(
                orgGuard.ErrorCode!,
                orgGuard.ErrorMessage!);
        }

        var user = await _users.GetByIdAsync(personalUserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<PersonalAdRewardClaimResultDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Personal user was not found.");
        }

        var identityGuard = OrganizationRewardRedemptionGuard.EnsurePersonalIdentity(user);
        if (!identityGuard.IsSuccess)
        {
            return ApplicationResult<PersonalAdRewardClaimResultDto>.Failure(
                identityGuard.ErrorCode!,
                identityGuard.ErrorMessage!);
        }

        string normalizedKey;
        try
        {
            normalizedKey = PersonalRewardClaim.NormalizeClaimKey(claimKey);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalAdRewardClaimResultDto>.Failure(
                ApplicationErrorCodes.PersonalRewardClaimInvalid,
                ex.Message);
        }

        var existing = await _claims
            .FindByUserTypeAndKeyAsync(
                personalUserId,
                PersonalRewardClaimTypes.AdReward,
                normalizedKey,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            var bal = await _balances.GetByUserAsync(personalUserId, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PersonalAdRewardClaimResultDto>.Success(
                Map(existing, bal?.AvailablePoints ?? 0, alreadyClaimed: true));
        }

        var utcNow = _clock.UtcNow;
        var eligibility = await _eligibility
            .EvaluateAsync(personalUserId, utcNow, cancellationToken)
            .ConfigureAwait(false);
        if (!eligibility.IsEligible)
        {
            return ApplicationResult<PersonalAdRewardClaimResultDto>.Failure(
                eligibility.DenialCode ?? ApplicationErrorCodes.PersonalRewardClaimNotEligible,
                eligibility.DenialMessage ?? "Rewarded-ad claim is not eligible.");
        }

        var verification = await _verifier
            .VerifyAsync(personalUserId, normalizedKey, cancellationToken)
            .ConfigureAwait(false);
        if (!verification.IsValid || verification.Points is not > 0)
        {
            return ApplicationResult<PersonalAdRewardClaimResultDto>.Failure(
                verification.ErrorCode ?? ApplicationErrorCodes.PersonalRewardClaimNotEligible,
                verification.ErrorMessage ?? "Rewarded-ad claim could not be verified.");
        }

        var points = verification.Points.Value;
        var balance = await _balances.GetByUserAsync(personalUserId, cancellationToken).ConfigureAwait(false);
        var isNewBalance = balance is null;
        balance ??= PersonalRewardBalance.Create(personalUserId, utcNow);
        var expectedVersion = balance.Version;
        var ledgerKey = PersonalRewardClaim.BuildLedgerIdempotencyKey(
            PersonalRewardClaimTypes.AdReward,
            normalizedKey);

        PersonalRewardTransaction creditTx;
        try
        {
            creditTx = balance.Credit(
                points,
                PersonalRewardSources.AdReward,
                utcNow,
                reason: "Rewarded ad claim",
                referenceId: normalizedKey,
                idempotencyKey: ledgerKey);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalAdRewardClaimResultDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                ex.Message);
        }

        PersonalRewardClaim claim;
        try
        {
            claim = PersonalRewardClaim.Create(
                personalUserId,
                PersonalRewardClaimTypes.AdReward,
                normalizedKey,
                points,
                creditTx.Id,
                utcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalAdRewardClaimResultDto>.Failure(
                ApplicationErrorCodes.PersonalRewardClaimInvalid,
                ex.Message);
        }

        if (isNewBalance)
        {
            await _balances.AddAsync(balance, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _balances.UpdateAsync(balance, expectedVersion, cancellationToken).ConfigureAwait(false);
        }

        await _transactions.AddAsync(creditTx, cancellationToken).ConfigureAwait(false);
        await _claims.AddAsync(claim, cancellationToken).ConfigureAwait(false);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PersistenceConflictException)
        {
            var raced = await _claims
                .FindByUserTypeAndKeyAsync(
                    personalUserId,
                    PersonalRewardClaimTypes.AdReward,
                    normalizedKey,
                    cancellationToken)
                .ConfigureAwait(false);
            if (raced is not null)
            {
                var bal = await _balances.GetByUserAsync(personalUserId, cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PersonalAdRewardClaimResultDto>.Success(
                    Map(raced, bal?.AvailablePoints ?? 0, alreadyClaimed: true));
            }

            return ApplicationResult<PersonalAdRewardClaimResultDto>.Failure(
                ApplicationErrorCodes.PersonalRewardBalanceConflict,
                "Personal reward claim conflicted concurrently. Retry the claim.");
        }

        return ApplicationResult<PersonalAdRewardClaimResultDto>.Success(
            Map(claim, balance.AvailablePoints, alreadyClaimed: false));
    }

    private static PersonalAdRewardClaimResultDto Map(
        PersonalRewardClaim claim,
        int availablePoints,
        bool alreadyClaimed) =>
        new(
            claim.PersonalUserId.Value,
            claim.ClaimType,
            claim.ClaimKey,
            alreadyClaimed,
            claim.PointsAwarded,
            availablePoints,
            claim.RewardTransactionId,
            claim.ClaimedAtUtc);
}
