using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>Logical earning claim types for idempotent Personal reward credits.</summary>
public static class PersonalRewardClaimTypes
{
    /// <summary>Rewarded-ad completion claim (verifier is null/provider-neutral until WP09).</summary>
    public const string AdReward = "AdReward";
}

/// <summary>
/// Idempotent earning-claim record. Subject is PersonalUserId only — never OrganizationId.
/// One claim key credits the ledger at most once per user and claim type.
/// </summary>
public sealed class PersonalRewardClaim
{
    public Guid Id { get; }
    public PlatformUserId PersonalUserId { get; }
    public string ClaimType { get; }
    public string ClaimKey { get; }
    public int PointsAwarded { get; }
    public Guid RewardTransactionId { get; }
    public DateTimeOffset ClaimedAtUtc { get; }

    private PersonalRewardClaim(
        Guid id,
        PlatformUserId personalUserId,
        string claimType,
        string claimKey,
        int pointsAwarded,
        Guid rewardTransactionId,
        DateTimeOffset claimedAtUtc)
    {
        Id = id;
        PersonalUserId = personalUserId;
        ClaimType = claimType;
        ClaimKey = claimKey;
        PointsAwarded = pointsAwarded;
        RewardTransactionId = rewardTransactionId;
        ClaimedAtUtc = claimedAtUtc;
    }

    public static PersonalRewardClaim Create(
        PlatformUserId personalUserId,
        string claimType,
        string claimKey,
        int pointsAwarded,
        Guid rewardTransactionId,
        DateTimeOffset utcNow,
        Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(personalUserId);
        EnsureUtc(utcNow);
        var type = NormalizeClaimType(claimType);
        var key = NormalizeClaimKey(claimKey);
        if (pointsAwarded <= 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalRewardPoints,
                "Claim points must be a positive integer.");
        }

        if (rewardTransactionId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalRewardPoints,
                "Reward transaction id is required for a claim.");
        }

        return new PersonalRewardClaim(
            id ?? Guid.NewGuid(),
            personalUserId,
            type,
            key,
            pointsAwarded,
            rewardTransactionId,
            utcNow);
    }

    public static PersonalRewardClaim Rehydrate(
        Guid id,
        PlatformUserId personalUserId,
        string claimType,
        string claimKey,
        int pointsAwarded,
        Guid rewardTransactionId,
        DateTimeOffset claimedAtUtc) =>
        new(id, personalUserId, claimType, claimKey, pointsAwarded, rewardTransactionId, claimedAtUtc);

    public static string BuildLedgerIdempotencyKey(string claimType, string claimKey) =>
        $"{NormalizeClaimType(claimType)}:{NormalizeClaimKey(claimKey)}";

    public static string NormalizeClaimType(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType) || claimType.Trim().Length > 32)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalRewardClaim,
                "Reward claim type is required (max 32 characters).");
        }

        return claimType.Trim();
    }

    public static string NormalizeClaimKey(string claimKey)
    {
        if (string.IsNullOrWhiteSpace(claimKey))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalRewardClaim,
                "Reward claim key is required.");
        }

        var trimmed = claimKey.Trim();
        if (trimmed.Length is < 8 or > 128)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalRewardClaim,
                "Reward claim key must be 8–128 characters.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Personal reward claim timestamps must be UTC.");
        }
    }
}
