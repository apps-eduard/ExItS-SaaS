using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

public enum PersonalRewardTransactionType
{
    Credit = 1,
    Debit = 2
}

/// <summary>Trusted or system sources for reward-point ledger movements.</summary>
public static class PersonalRewardSources
{
    public const string AdminAward = "AdminAward";
    public const string FeatureRedemption = "FeatureRedemption";
    public const string Promotion = "Promotion";
    public const string AdReward = "AdReward";
}

/// <summary>
/// Per-PersonalUser maintained reward-points balance (never Organization-scoped).
/// Append-only transactions update this aggregate under optimistic concurrency.
/// </summary>
public sealed class PersonalRewardBalance
{
    public PlatformUserId PersonalUserId { get; }
    public int AvailablePoints { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }

    private PersonalRewardBalance(
        PlatformUserId personalUserId,
        int availablePoints,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version)
    {
        PersonalUserId = personalUserId;
        AvailablePoints = availablePoints;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Version = version;
    }

    public static PersonalRewardBalance Create(PlatformUserId personalUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(personalUserId);
        EnsureUtc(utcNow);
        return new PersonalRewardBalance(personalUserId, 0, utcNow, utcNow, version: 1);
    }

    public static PersonalRewardBalance Rehydrate(
        PlatformUserId personalUserId,
        int availablePoints,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version) =>
        new(personalUserId, availablePoints, createdAtUtc, updatedAtUtc, version);

    public PersonalRewardTransaction Credit(
        int points,
        string source,
        DateTimeOffset utcNow,
        string? reason = null,
        string? referenceId = null,
        string? idempotencyKey = null,
        Guid? transactionId = null)
    {
        EnsureUtc(utcNow);
        EnsurePositivePoints(points);
        EnsureSource(source);

        AvailablePoints = checked(AvailablePoints + points);
        UpdatedAtUtc = utcNow;
        Version++;

        return PersonalRewardTransaction.Create(
            transactionId,
            PersonalUserId,
            PersonalRewardTransactionType.Credit,
            points,
            signedDelta: points,
            balanceAfter: AvailablePoints,
            source,
            utcNow,
            reason,
            referenceId,
            idempotencyKey);
    }

    public PersonalRewardTransaction Debit(
        int points,
        string source,
        DateTimeOffset utcNow,
        string? reason = null,
        string? referenceId = null,
        string? idempotencyKey = null,
        Guid? transactionId = null)
    {
        EnsureUtc(utcNow);
        EnsurePositivePoints(points);
        EnsureSource(source);

        if (AvailablePoints < points)
        {
            throw new DomainException(
                DomainErrorCodes.InsufficientPersonalRewardPoints,
                "Insufficient personal reward points.");
        }

        AvailablePoints -= points;
        UpdatedAtUtc = utcNow;
        Version++;

        return PersonalRewardTransaction.Create(
            transactionId,
            PersonalUserId,
            PersonalRewardTransactionType.Debit,
            points,
            signedDelta: -points,
            balanceAfter: AvailablePoints,
            source,
            utcNow,
            reason,
            referenceId,
            idempotencyKey);
    }

    private static void EnsurePositivePoints(int points)
    {
        if (points <= 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalRewardPoints,
                "Reward points amount must be a positive integer.");
        }
    }

    private static void EnsureSource(string source)
    {
        if (string.IsNullOrWhiteSpace(source) || source.Trim().Length > 64)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalRewardSource,
                "Reward source is required (max 64 characters).");
        }
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Personal reward timestamps must be UTC.");
        }
    }
}

/// <summary>Immutable append-only reward-points ledger entry.</summary>
public sealed class PersonalRewardTransaction
{
    public Guid Id { get; }
    public PlatformUserId PersonalUserId { get; }
    public PersonalRewardTransactionType TransactionType { get; }
    public int Points { get; }
    public int SignedDelta { get; }
    public int BalanceAfter { get; }
    public string Source { get; }
    public string? Reason { get; }
    public string? ReferenceId { get; }
    public string? IdempotencyKey { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    private PersonalRewardTransaction(
        Guid id,
        PlatformUserId personalUserId,
        PersonalRewardTransactionType transactionType,
        int points,
        int signedDelta,
        int balanceAfter,
        string source,
        string? reason,
        string? referenceId,
        string? idempotencyKey,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        PersonalUserId = personalUserId;
        TransactionType = transactionType;
        Points = points;
        SignedDelta = signedDelta;
        BalanceAfter = balanceAfter;
        Source = source;
        Reason = reason;
        ReferenceId = referenceId;
        IdempotencyKey = idempotencyKey;
        CreatedAtUtc = createdAtUtc;
    }

    internal static PersonalRewardTransaction Create(
        Guid? id,
        PlatformUserId personalUserId,
        PersonalRewardTransactionType transactionType,
        int points,
        int signedDelta,
        int balanceAfter,
        string source,
        DateTimeOffset utcNow,
        string? reason,
        string? referenceId,
        string? idempotencyKey)
    {
        ArgumentNullException.ThrowIfNull(personalUserId);
        var normalizedReason = string.IsNullOrWhiteSpace(reason)
            ? null
            : reason.Trim()[..Math.Min(reason.Trim().Length, 512)];
        var normalizedReference = string.IsNullOrWhiteSpace(referenceId)
            ? null
            : referenceId.Trim()[..Math.Min(referenceId.Trim().Length, 128)];
        var normalizedKey = string.IsNullOrWhiteSpace(idempotencyKey)
            ? null
            : idempotencyKey.Trim()[..Math.Min(idempotencyKey.Trim().Length, 128)];

        return new PersonalRewardTransaction(
            id ?? Guid.NewGuid(),
            personalUserId,
            transactionType,
            points,
            signedDelta,
            balanceAfter,
            source.Trim(),
            normalizedReason,
            normalizedReference,
            normalizedKey,
            utcNow);
    }

    public static PersonalRewardTransaction Rehydrate(
        Guid id,
        PlatformUserId personalUserId,
        PersonalRewardTransactionType transactionType,
        int points,
        int signedDelta,
        int balanceAfter,
        string source,
        string? reason,
        string? referenceId,
        string? idempotencyKey,
        DateTimeOffset createdAtUtc) =>
        new(
            id,
            personalUserId,
            transactionType,
            points,
            signedDelta,
            balanceAfter,
            source,
            reason,
            referenceId,
            idempotencyKey,
            createdAtUtc);
}
