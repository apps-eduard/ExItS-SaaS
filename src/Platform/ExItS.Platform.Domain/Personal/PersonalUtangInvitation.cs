using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>
/// Explicit Personal Utang participant invitation. Acceptance links one contact to one user
/// and authorizes shared relationship view — never Organization membership or product roles.
/// </summary>
public sealed class PersonalUtangInvitation
{
    public const int DefaultLifetimeHours = 24 * 7;
    public static readonly TimeSpan MinIntervalBetweenResends = TimeSpan.FromHours(1);

    public PersonalUtangInvitationId Id { get; }
    public PersonalDebtRelationshipId DebtRelationshipId { get; }
    public PersonalContactId InviteeContactId { get; }
    public PlatformUserId InvitedByUserIdentityId { get; }
    public string? InviteTargetNormalizedEmail { get; private set; }
    public string? InviteTargetPhone { get; private set; }
    public PersonalUtangInvitationStatus Status { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? DeclinedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public PlatformUserId? AcceptedByUserIdentityId { get; private set; }

    private PersonalUtangInvitation(
        PersonalUtangInvitationId id,
        PersonalDebtRelationshipId debtRelationshipId,
        PersonalContactId inviteeContactId,
        PlatformUserId invitedByUserIdentityId,
        string? inviteTargetNormalizedEmail,
        string? inviteTargetPhone,
        PersonalUtangInvitationStatus status,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset? declinedAtUtc,
        DateTimeOffset? revokedAtUtc,
        PlatformUserId? acceptedByUserIdentityId)
    {
        Id = id;
        DebtRelationshipId = debtRelationshipId;
        InviteeContactId = inviteeContactId;
        InvitedByUserIdentityId = invitedByUserIdentityId;
        InviteTargetNormalizedEmail = inviteTargetNormalizedEmail;
        InviteTargetPhone = inviteTargetPhone;
        Status = status;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AcceptedAtUtc = acceptedAtUtc;
        DeclinedAtUtc = declinedAtUtc;
        RevokedAtUtc = revokedAtUtc;
        AcceptedByUserIdentityId = acceptedByUserIdentityId;
    }

    public static (PersonalUtangInvitation Invitation, string AcceptToken) Create(
        PersonalDebtRelationshipId debtRelationshipId,
        PersonalContactId inviteeContactId,
        PlatformUserId invitedByUserIdentityId,
        DateTimeOffset utcNow,
        string? inviteTargetEmail = null,
        string? inviteTargetPhone = null,
        TimeSpan? lifetime = null,
        PersonalUtangInvitationId? id = null)
    {
        ArgumentNullException.ThrowIfNull(debtRelationshipId);
        ArgumentNullException.ThrowIfNull(inviteeContactId);
        ArgumentNullException.ThrowIfNull(invitedByUserIdentityId);
        EnsureUtc(utcNow);

        var acceptToken = CreateAcceptToken();
        var invitation = new PersonalUtangInvitation(
            id ?? PersonalUtangInvitationId.New(),
            debtRelationshipId,
            inviteeContactId,
            invitedByUserIdentityId,
            NormalizeOptionalEmail(inviteTargetEmail),
            NormalizeOptionalPhone(inviteTargetPhone),
            PersonalUtangInvitationStatus.Pending,
            HashToken(acceptToken),
            utcNow,
            utcNow,
            utcNow.Add(lifetime ?? TimeSpan.FromHours(DefaultLifetimeHours)),
            null,
            null,
            null,
            null);
        return (invitation, acceptToken);
    }

    public static PersonalUtangInvitation Rehydrate(
        PersonalUtangInvitationId id,
        PersonalDebtRelationshipId debtRelationshipId,
        PersonalContactId inviteeContactId,
        PlatformUserId invitedByUserIdentityId,
        string? inviteTargetNormalizedEmail,
        string? inviteTargetPhone,
        PersonalUtangInvitationStatus status,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset? declinedAtUtc,
        DateTimeOffset? revokedAtUtc,
        PlatformUserId? acceptedByUserIdentityId) =>
        new(
            id,
            debtRelationshipId,
            inviteeContactId,
            invitedByUserIdentityId,
            inviteTargetNormalizedEmail,
            inviteTargetPhone,
            status,
            tokenHash,
            createdAtUtc,
            updatedAtUtc,
            expiresAtUtc,
            acceptedAtUtc,
            declinedAtUtc,
            revokedAtUtc,
            acceptedByUserIdentityId);

    public string Resend(DateTimeOffset utcNow, TimeSpan? lifetime = null)
    {
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);
        EnsureResendAllowed(utcNow);
        var acceptToken = CreateAcceptToken();
        TokenHash = HashToken(acceptToken);
        ExpiresAtUtc = utcNow.Add(lifetime ?? TimeSpan.FromHours(DefaultLifetimeHours));
        UpdatedAtUtc = utcNow;
        return acceptToken;
    }

    /// <summary>
    /// Anti-harassment: resends must be spaced (same floor as reminder delivery interval)
    /// from create or the previous resend (<see cref="UpdatedAtUtc"/>).
    /// </summary>
    public void EnsureResendAllowed(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (utcNow - UpdatedAtUtc < MinIntervalBetweenResends)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangInvitationRateLimited,
                "Invitation resends must be spaced at least one hour apart.");
        }
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status is PersonalUtangInvitationStatus.Accepted
            or PersonalUtangInvitationStatus.Revoked
            or PersonalUtangInvitationStatus.Declined)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalUtangInvitationStatusTransition,
                $"Cannot revoke an invitation in status {Status}.");
        }

        Status = PersonalUtangInvitationStatus.Revoked;
        RevokedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Decline(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);
        Status = PersonalUtangInvitationStatus.Declined;
        DeclinedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Accept(
        PlatformUserId acceptedByUserIdentityId,
        string? acceptingUserEmail,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(acceptedByUserIdentityId);
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        if (acceptedByUserIdentityId == InvitedByUserIdentityId)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalContactLinkInvalid,
                "Cannot accept an invitation you created.");
        }

        if (!string.IsNullOrWhiteSpace(InviteTargetNormalizedEmail))
        {
            var normalized = PlatformUser.NormalizeEmail(acceptingUserEmail ?? string.Empty);
            if (!string.Equals(normalized, InviteTargetNormalizedEmail, StringComparison.Ordinal))
            {
                throw new DomainException(
                    DomainErrorCodes.PersonalUtangInvitationEmailMismatch,
                    "Invitation email does not match the accepting user.");
            }
        }

        Status = PersonalUtangInvitationStatus.Accepted;
        AcceptedAtUtc = utcNow;
        AcceptedByUserIdentityId = acceptedByUserIdentityId;
        UpdatedAtUtc = utcNow;
    }

    public void MarkExpired(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status != PersonalUtangInvitationStatus.Pending)
        {
            return;
        }

        if (utcNow < ExpiresAtUtc)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalUtangInvitationStatusTransition,
                "Invitation has not expired yet.");
        }

        Status = PersonalUtangInvitationStatus.Expired;
        UpdatedAtUtc = utcNow;
    }

    public bool IsExpired(DateTimeOffset utcNow) =>
        Status == PersonalUtangInvitationStatus.Pending && utcNow >= ExpiresAtUtc;

    public static string HashToken(string acceptToken)
    {
        if (string.IsNullOrWhiteSpace(acceptToken))
        {
            throw new DomainException(
                DomainErrorCodes.PersonalUtangInvitationTokenInvalid,
                "Invitation token cannot be blank.");
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(acceptToken.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private void EnsurePendingUsable(DateTimeOffset utcNow)
    {
        if (Status != PersonalUtangInvitationStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalUtangInvitationStatusTransition,
                $"Invitation is not pending (status {Status}).");
        }

        if (utcNow >= ExpiresAtUtc)
        {
            Status = PersonalUtangInvitationStatus.Expired;
            UpdatedAtUtc = utcNow;
            throw new DomainException(
                DomainErrorCodes.PersonalUtangInvitationExpired,
                "Invitation has expired.");
        }
    }

    private static string CreateAcceptToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string? NormalizeOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return PlatformUser.NormalizeEmail(email);
    }

    private static string? NormalizeOptionalPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var trimmed = phone.Trim();
        return trimmed.Length > 32 ? trimmed[..32] : trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalUtangInvitationId : IEquatable<PersonalUtangInvitationId>
{
    public Guid Value { get; }

    private PersonalUtangInvitationId(Guid value) => Value = value;

    public static PersonalUtangInvitationId New() => new(Guid.NewGuid());

    public static PersonalUtangInvitationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalUtangInvitationId,
                "Personal Utang invitation id is required.");
        }

        return new PersonalUtangInvitationId(value);
    }

    public bool Equals(PersonalUtangInvitationId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalUtangInvitationId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PersonalUtangInvitationId? left, PersonalUtangInvitationId? right) =>
        Equals(left, right);

    public static bool operator !=(PersonalUtangInvitationId? left, PersonalUtangInvitationId? right) =>
        !Equals(left, right);
}
