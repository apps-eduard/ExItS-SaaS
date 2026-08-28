using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization Staff Invitation. Stores only a token hash — never plaintext secrets.
/// Accepting creates an Organization membership + staff role only; never grants platform-wide roles,
/// product-local roles, Business Customer records, or Customer Links.
/// ExItS-native invites may target a Personal user (EX-ID / Personal QR); email-only invites remain supported.
/// </summary>
public sealed class OrganizationInvitation
{
    public const int DefaultLifetimeHours = 24 * 7;
    public const string InvitationType = InvitationKinds.OrganizationStaffInvitation;

    public OrganizationInvitationId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public string NormalizedEmail { get; private set; }
    public OrganizationRole Role { get; private set; }
    public InvitationStatus Status { get; private set; }
    public string TokenHash { get; private set; }
    public PlatformUserId? InvitedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public DateTimeOffset? DeclinedAtUtc { get; private set; }
    public PlatformUserId? AcceptedByUserId { get; private set; }
    public string? InviteeDisplayName { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? Branch { get; private set; }
    public string? ProductRole { get; private set; }
    /// <summary>Personal PlatformUser targeted by ExItS-native invite (null for legacy email-only).</summary>
    public PlatformUserId? TargetPersonalUserId { get; private set; }
    /// <summary>Public EX-ID of the Personal target (safe display / correlation).</summary>
    public string? TargetPublicUserId { get; private set; }

    private OrganizationInvitation(
        OrganizationInvitationId id,
        PlatformOrganizationId organizationId,
        string normalizedEmail,
        OrganizationRole role,
        InvitationStatus status,
        string tokenHash,
        PlatformUserId? invitedByUserId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset? revokedAtUtc,
        DateTimeOffset? declinedAtUtc,
        PlatformUserId? acceptedByUserId,
        string? inviteeDisplayName,
        string? firstName,
        string? lastName,
        string? branch,
        string? productRole,
        PlatformUserId? targetPersonalUserId,
        string? targetPublicUserId)
    {
        Id = id;
        OrganizationId = organizationId;
        NormalizedEmail = normalizedEmail;
        Role = role;
        Status = status;
        TokenHash = tokenHash;
        InvitedByUserId = invitedByUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AcceptedAtUtc = acceptedAtUtc;
        RevokedAtUtc = revokedAtUtc;
        DeclinedAtUtc = declinedAtUtc;
        AcceptedByUserId = acceptedByUserId;
        InviteeDisplayName = inviteeDisplayName;
        FirstName = firstName;
        LastName = lastName;
        Branch = branch;
        ProductRole = productRole;
        TargetPersonalUserId = targetPersonalUserId;
        TargetPublicUserId = targetPublicUserId;
    }

    /// <summary>Creates a pending invitation and returns the plaintext accept token (show once).</summary>
    public static (OrganizationInvitation Invitation, string AcceptToken) Create(
        PlatformOrganizationId organizationId,
        string email,
        OrganizationRole role,
        DateTimeOffset utcNow,
        PlatformUserId? invitedByUserId = null,
        TimeSpan? lifetime = null,
        OrganizationInvitationId? id = null,
        string? inviteeDisplayName = null,
        string? firstName = null,
        string? lastName = null,
        string? branch = null,
        string? productRole = null,
        PlatformUserId? targetPersonalUserId = null,
        string? targetPublicUserId = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        EnsureUtc(utcNow);
        EnsureDefinedRole(role);
        var normalizedEmail = PlatformUser.NormalizeEmail(email);
        var acceptToken = CreateAcceptToken();
        var invitation = new OrganizationInvitation(
            id ?? OrganizationInvitationId.New(),
            organizationId,
            normalizedEmail,
            role,
            InvitationStatus.Pending,
            HashToken(acceptToken),
            invitedByUserId,
            utcNow,
            utcNow,
            utcNow.Add(lifetime ?? TimeSpan.FromHours(DefaultLifetimeHours)),
            null,
            null,
            null,
            null,
            NormalizeOptional(inviteeDisplayName, 256),
            NormalizeOptional(firstName, 100),
            NormalizeOptional(lastName, 100),
            NormalizeOptional(branch, 128),
            NormalizeOptional(productRole, 64),
            targetPersonalUserId,
            NormalizeOptional(targetPublicUserId, 32));
        return (invitation, acceptToken);
    }

    public static OrganizationInvitation Rehydrate(
        OrganizationInvitationId id,
        PlatformOrganizationId organizationId,
        string normalizedEmail,
        OrganizationRole role,
        InvitationStatus status,
        string tokenHash,
        PlatformUserId? invitedByUserId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset? revokedAtUtc,
        PlatformUserId? acceptedByUserId,
        string? inviteeDisplayName = null,
        string? firstName = null,
        string? lastName = null,
        string? branch = null,
        string? productRole = null,
        PlatformUserId? targetPersonalUserId = null,
        string? targetPublicUserId = null,
        DateTimeOffset? declinedAtUtc = null) =>
        new(
            id,
            organizationId,
            normalizedEmail,
            role,
            status,
            tokenHash,
            invitedByUserId,
            createdAtUtc,
            updatedAtUtc,
            expiresAtUtc,
            acceptedAtUtc,
            revokedAtUtc,
            declinedAtUtc,
            acceptedByUserId,
            inviteeDisplayName,
            firstName,
            lastName,
            branch,
            productRole,
            targetPersonalUserId,
            targetPublicUserId);

    /// <summary>Rotates the accept token and extends expiry for a still-pending invitation.</summary>
    public string Resend(DateTimeOffset utcNow, TimeSpan? lifetime = null)
    {
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);
        var acceptToken = CreateAcceptToken();
        TokenHash = HashToken(acceptToken);
        ExpiresAtUtc = utcNow.Add(lifetime ?? TimeSpan.FromHours(DefaultLifetimeHours));
        UpdatedAtUtc = utcNow;
        return acceptToken;
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status is InvitationStatus.Accepted or InvitationStatus.Revoked or InvitationStatus.Declined)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInvitationStatusTransition,
                $"Cannot revoke an invitation in status {Status}.");
        }

        Status = InvitationStatus.Revoked;
        RevokedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>Invitee Personal declines an ExItS-native invitation (not used for inviter cancel).</summary>
    public void Decline(PlatformUserId actorPersonalUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(actorPersonalUserId);
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        if (TargetPersonalUserId is null)
        {
            throw new DomainException(
                DomainErrorCodes.AuthorizationDenied,
                "This invitation cannot be declined in-app. Use the accept link flow or ask the business to cancel it.");
        }

        if (actorPersonalUserId != TargetPersonalUserId)
        {
            throw new DomainException(
                DomainErrorCodes.AuthorizationDenied,
                "Only the invited Personal account can decline this invitation.");
        }

        Status = InvitationStatus.Declined;
        DeclinedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Accept(PlatformUserId acceptedByUserId, string email, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(acceptedByUserId);
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        var normalized = PlatformUser.NormalizeEmail(email);
        if (!string.Equals(normalized, NormalizedEmail, StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.InvitationEmailMismatch,
                "Invitation email does not match the accepting user.");
        }

        Status = InvitationStatus.Accepted;
        AcceptedAtUtc = utcNow;
        AcceptedByUserId = acceptedByUserId;
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Accept when the invite is bound to a Personal target — email still stamps contact on staff identity.
    /// </summary>
    public void AcceptForPersonalTarget(PlatformUserId staffUserId, PlatformUserId personalUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(staffUserId);
        ArgumentNullException.ThrowIfNull(personalUserId);
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        if (TargetPersonalUserId is null || personalUserId != TargetPersonalUserId)
        {
            throw new DomainException(
                DomainErrorCodes.AuthorizationDenied,
                "Only the invited Personal account can accept this invitation.");
        }

        Status = InvitationStatus.Accepted;
        AcceptedAtUtc = utcNow;
        AcceptedByUserId = staffUserId;
        UpdatedAtUtc = utcNow;
    }

    public void MarkExpired(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status != InvitationStatus.Pending)
        {
            return;
        }

        if (utcNow < ExpiresAtUtc)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInvitationStatusTransition,
                "Invitation has not expired yet.");
        }

        Status = InvitationStatus.Expired;
        UpdatedAtUtc = utcNow;
    }

    public bool IsExpired(DateTimeOffset utcNow) =>
        Status == InvitationStatus.Pending && utcNow >= ExpiresAtUtc;

    public bool IsExItsNativePersonalInvite => TargetPersonalUserId is not null;

    public static string HashToken(string acceptToken)
    {
        if (string.IsNullOrWhiteSpace(acceptToken))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInvitationToken,
                "Invitation token cannot be blank.");
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(acceptToken.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private void EnsurePendingUsable(DateTimeOffset utcNow)
    {
        if (Status != InvitationStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInvitationStatusTransition,
                $"Invitation is not pending (status {Status}).");
        }

        if (utcNow >= ExpiresAtUtc)
        {
            Status = InvitationStatus.Expired;
            UpdatedAtUtc = utcNow;
            throw new DomainException(
                DomainErrorCodes.InvitationExpired,
                "Invitation has expired.");
        }
    }

    private static string CreateAcceptToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static void EnsureDefinedRole(OrganizationRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationRole,
                "Organization role is not defined.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamps must be UTC (offset zero).");
        }
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
