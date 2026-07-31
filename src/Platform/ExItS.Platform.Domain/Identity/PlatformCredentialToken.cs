using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Identity;

public enum PlatformCredentialTokenPurpose
{
    PasswordReset = 1,
    EmailVerification = 2,
    RecoveryEmailVerification = 3
}

/// <summary>
/// One-time opaque credential workflow token (password reset or email verification).
/// Only the hash is persisted — never the raw token, password, or email body.
/// </summary>
public sealed class PlatformCredentialToken
{
    public PlatformCredentialTokenId Id { get; }
    public PlatformUserId UserId { get; }
    public PlatformCredentialTokenPurpose Purpose { get; }
    public string TokenHash { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    private PlatformCredentialToken(
        PlatformCredentialTokenId id,
        PlatformUserId userId,
        PlatformCredentialTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? consumedAtUtc)
    {
        Id = id;
        UserId = userId;
        Purpose = purpose;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        ConsumedAtUtc = consumedAtUtc;
    }

    public static PlatformCredentialToken Create(
        PlatformUserId userId,
        PlatformCredentialTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset utcNow,
        TimeSpan lifetime,
        PlatformCredentialTokenId? id = null)
    {
        ArgumentNullException.ThrowIfNull(userId);
        EnsureUtc(utcNow);
        if (lifetime <= TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Token lifetime must be positive.");
        }

        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length > 128)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Token hash is invalid.");
        }

        if (!Enum.IsDefined(purpose))
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Token purpose is invalid.");
        }

        return new PlatformCredentialToken(
            id ?? PlatformCredentialTokenId.New(),
            userId,
            purpose,
            tokenHash.Trim(),
            utcNow,
            utcNow.Add(lifetime),
            consumedAtUtc: null);
    }

    public static PlatformCredentialToken Rehydrate(
        PlatformCredentialTokenId id,
        PlatformUserId userId,
        PlatformCredentialTokenPurpose purpose,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? consumedAtUtc) =>
        new(id, userId, purpose, tokenHash, createdAtUtc, expiresAtUtc, consumedAtUtc);

    public bool IsRedeemable(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        return ConsumedAtUtc is null && ExpiresAtUtc > utcNow;
    }

    public void Consume(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (!IsRedeemable(utcNow))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "Credential token is not redeemable.");
        }

        ConsumedAtUtc = utcNow;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Token timestamps must be UTC.");
        }
    }
}

public sealed class PlatformCredentialTokenId : IEquatable<PlatformCredentialTokenId>
{
    public Guid Value { get; }

    private PlatformCredentialTokenId(Guid value) => Value = value;

    public static PlatformCredentialTokenId New() => new(Guid.NewGuid());

    public static PlatformCredentialTokenId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Token id is required.");
        }

        return new PlatformCredentialTokenId(value);
    }

    public bool Equals(PlatformCredentialTokenId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PlatformCredentialTokenId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PlatformCredentialTokenId? left, PlatformCredentialTokenId? right) =>
        Equals(left, right);

    public static bool operator !=(PlatformCredentialTokenId? left, PlatformCredentialTokenId? right) =>
        !Equals(left, right);
}
