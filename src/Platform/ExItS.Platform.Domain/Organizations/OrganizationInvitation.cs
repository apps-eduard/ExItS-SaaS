using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Pending organization membership invitation. Stores only a token hash — never plaintext secrets.
/// Accepting creates an organization membership; never grants platform-wide or product-local roles.
/// </summary>
public sealed class OrganizationInvitation
{
    public const int DefaultLifetimeHours = 24 * 7;

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
    public PlatformUserId? AcceptedByUserId { get; private set; }

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
        PlatformUserId? acceptedByUserId)
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
        AcceptedByUserId = acceptedByUserId;
    }

    /// <summary>Creates a pending invitation and returns the plaintext accept token (show once).</summary>
    public static (OrganizationInvitation Invitation, string AcceptToken) Create(
        PlatformOrganizationId organizationId,
        string email,
        OrganizationRole role,
        DateTimeOffset utcNow,
        PlatformUserId? invitedByUserId = null,
        TimeSpan? lifetime = null,
        OrganizationInvitationId? id = null)
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
            null);
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
        PlatformUserId? acceptedByUserId) =>
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
            acceptedByUserId);

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
        if (Status is InvitationStatus.Accepted or InvitationStatus.Revoked)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInvitationStatusTransition,
                $"Cannot revoke an invitation in status {Status}.");
        }

        Status = InvitationStatus.Revoked;
        RevokedAtUtc = utcNow;
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
}
