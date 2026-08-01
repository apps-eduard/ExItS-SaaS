using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// One account profile for a verified person (<see cref="PlatformUser"/> = User Identity persistence).
/// A session is bound to exactly one profile / <see cref="AccountClass"/>.
/// </summary>
public sealed class AccountProfile
{
    public AccountProfileId Id { get; }
    public PlatformUserId UserIdentityId { get; }
    public AccountClass AccountClass { get; }
    public string Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private AccountProfile(
        AccountProfileId id,
        PlatformUserId userIdentityId,
        AccountClass accountClass,
        string status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        UserIdentityId = userIdentityId;
        AccountClass = accountClass;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static AccountProfile Create(
        PlatformUserId userIdentityId,
        AccountClass accountClass,
        DateTimeOffset utcNow,
        AccountProfileId? id = null)
    {
        ArgumentNullException.ThrowIfNull(userIdentityId);
        EnsureUtc(utcNow);
        if (!Enum.IsDefined(accountClass))
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Account class is invalid.");
        }

        return new AccountProfile(
            id ?? AccountProfileId.New(),
            userIdentityId,
            accountClass,
            nameof(AccountStatus.Active),
            utcNow,
            utcNow);
    }

    public static AccountProfile Rehydrate(
        AccountProfileId id,
        PlatformUserId userIdentityId,
        AccountClass accountClass,
        string status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, userIdentityId, accountClass, status, createdAtUtc, updatedAtUtc);

    public bool IsActive =>
        string.Equals(Status, nameof(AccountStatus.Active), StringComparison.Ordinal);

    public void Deactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        Status = nameof(AccountStatus.Deactivated);
        UpdatedAtUtc = utcNow;
    }

    public void Activate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        Status = nameof(AccountStatus.Active);
        UpdatedAtUtc = utcNow;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Account profile timestamps must be UTC.");
        }
    }
}

public sealed class AccountProfileId : IEquatable<AccountProfileId>
{
    public Guid Value { get; }

    private AccountProfileId(Guid value) => Value = value;

    public static AccountProfileId New() => new(Guid.NewGuid());

    public static AccountProfileId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidAccountStatusTransition, "Account profile id is required.");
        }

        return new AccountProfileId(value);
    }

    public bool Equals(AccountProfileId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is AccountProfileId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(AccountProfileId? left, AccountProfileId? right) => Equals(left, right);

    public static bool operator !=(AccountProfileId? left, AccountProfileId? right) => !Equals(left, right);
}
