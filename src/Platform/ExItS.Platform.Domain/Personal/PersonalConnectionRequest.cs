using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>
/// Person-level Personal connection consent request. Independent of Utang/debt records.
/// </summary>
public sealed class PersonalConnectionRequest
{
    public const int DefaultLifetimeDays = 30;

    public PersonalConnectionRequestId Id { get; }
    public PlatformUserId RequesterUserIdentityId { get; }
    public PlatformUserId TargetUserIdentityId { get; }
    public PersonalContactId RequesterContactId { get; }
    public PersonalConnectionRequestStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? DeclinedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public PlatformUserId? RespondedByUserIdentityId { get; private set; }

    private PersonalConnectionRequest(
        PersonalConnectionRequestId id,
        PlatformUserId requesterUserIdentityId,
        PlatformUserId targetUserIdentityId,
        PersonalContactId requesterContactId,
        PersonalConnectionRequestStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset? declinedAtUtc,
        DateTimeOffset? revokedAtUtc,
        PlatformUserId? respondedByUserIdentityId)
    {
        Id = id;
        RequesterUserIdentityId = requesterUserIdentityId;
        TargetUserIdentityId = targetUserIdentityId;
        RequesterContactId = requesterContactId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AcceptedAtUtc = acceptedAtUtc;
        DeclinedAtUtc = declinedAtUtc;
        RevokedAtUtc = revokedAtUtc;
        RespondedByUserIdentityId = respondedByUserIdentityId;
    }

    public static PersonalConnectionRequest Create(
        PlatformUserId requesterUserIdentityId,
        PlatformUserId targetUserIdentityId,
        PersonalContactId requesterContactId,
        DateTimeOffset utcNow,
        TimeSpan? lifetime = null,
        PersonalConnectionRequestId? id = null)
    {
        ArgumentNullException.ThrowIfNull(requesterUserIdentityId);
        ArgumentNullException.ThrowIfNull(targetUserIdentityId);
        ArgumentNullException.ThrowIfNull(requesterContactId);
        EnsureUtc(utcNow);

        if (requesterUserIdentityId == targetUserIdentityId)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalConnectionRequestInvalid,
                "Cannot request a connection with yourself.");
        }

        return new PersonalConnectionRequest(
            id ?? PersonalConnectionRequestId.New(),
            requesterUserIdentityId,
            targetUserIdentityId,
            requesterContactId,
            PersonalConnectionRequestStatus.Pending,
            utcNow,
            utcNow,
            utcNow.Add(lifetime ?? TimeSpan.FromDays(DefaultLifetimeDays)),
            null,
            null,
            null,
            null);
    }

    public static PersonalConnectionRequest Rehydrate(
        PersonalConnectionRequestId id,
        PlatformUserId requesterUserIdentityId,
        PlatformUserId targetUserIdentityId,
        PersonalContactId requesterContactId,
        PersonalConnectionRequestStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset? declinedAtUtc,
        DateTimeOffset? revokedAtUtc,
        PlatformUserId? respondedByUserIdentityId) =>
        new(
            id,
            requesterUserIdentityId,
            targetUserIdentityId,
            requesterContactId,
            status,
            createdAtUtc,
            updatedAtUtc,
            expiresAtUtc,
            acceptedAtUtc,
            declinedAtUtc,
            revokedAtUtc,
            respondedByUserIdentityId);

    public bool IsExpired(DateTimeOffset utcNow) =>
        Status == PersonalConnectionRequestStatus.Pending && utcNow >= ExpiresAtUtc;

    public void MarkExpired(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status != PersonalConnectionRequestStatus.Pending)
        {
            return;
        }

        Status = PersonalConnectionRequestStatus.Expired;
        UpdatedAtUtc = utcNow;
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);
        Status = PersonalConnectionRequestStatus.Revoked;
        RevokedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        RespondedByUserIdentityId = RequesterUserIdentityId;
    }

    public void InvalidatePending(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status != PersonalConnectionRequestStatus.Pending)
        {
            return;
        }

        Status = PersonalConnectionRequestStatus.Revoked;
        RevokedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Decline(PlatformUserId decliningUserIdentityId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(decliningUserIdentityId);
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        if (decliningUserIdentityId != TargetUserIdentityId)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalConnectionRequestUnauthorized,
                "Only the target user can decline this connection request.");
        }

        Status = PersonalConnectionRequestStatus.Declined;
        DeclinedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        RespondedByUserIdentityId = decliningUserIdentityId;
    }

    public void Accept(PlatformUserId acceptingUserIdentityId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(acceptingUserIdentityId);
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        if (acceptingUserIdentityId != TargetUserIdentityId)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalConnectionRequestUnauthorized,
                "Only the target user can accept this connection request.");
        }

        Status = PersonalConnectionRequestStatus.Accepted;
        AcceptedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        RespondedByUserIdentityId = acceptingUserIdentityId;
    }

    private void EnsurePendingUsable(DateTimeOffset utcNow)
    {
        if (Status != PersonalConnectionRequestStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalConnectionRequestStatusTransition,
                $"Connection request is not pending (status: {Status}).");
        }

        if (IsExpired(utcNow))
        {
            throw new DomainException(
                DomainErrorCodes.PersonalConnectionRequestExpired,
                "Connection request has expired.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalConnectionRequestId : IEquatable<PersonalConnectionRequestId>
{
    public Guid Value { get; }

    private PersonalConnectionRequestId(Guid value) => Value = value;

    public static PersonalConnectionRequestId New() => new(Guid.NewGuid());

    public static PersonalConnectionRequestId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalConnectionRequestId,
                "Personal connection request id is required.");
        }

        return new PersonalConnectionRequestId(value);
    }

    public bool Equals(PersonalConnectionRequestId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalConnectionRequestId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PersonalConnectionRequestId? left, PersonalConnectionRequestId? right) =>
        Equals(left, right);

    public static bool operator !=(PersonalConnectionRequestId? left, PersonalConnectionRequestId? right) =>
        !Equals(left, right);
}
