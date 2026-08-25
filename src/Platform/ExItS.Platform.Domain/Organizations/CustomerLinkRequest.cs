using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Explicit Customer Link Request. Acceptance links one Platform User to one Business Customer —
/// never Organization Staff membership and never a product-local role.
/// Existing ExItS users are targeted by <see cref="TargetUserIdentityId"/> (in-app consent).
/// Email+token remains the fallback invite path.
/// </summary>
public sealed class CustomerLinkRequest
{
    public const int DefaultLifetimeHours = 24 * 7;
    public const string InvitationType = InvitationKinds.CustomerLinkRequest;

    public CustomerLinkRequestId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public BusinessCustomerId BusinessCustomerId { get; }
    public string NormalizedEmail { get; private set; }
    public PlatformUserId? TargetUserIdentityId { get; private set; }
    public string? TargetPublicUserId { get; private set; }
    public CustomerLinkRequestStatus Status { get; private set; }
    public string TokenHash { get; private set; }
    public PlatformUserId? InvitedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? DeclinedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public PlatformUserId? AcceptedByUserId { get; private set; }
    public int ReminderCount { get; private set; }
    public DateTimeOffset? LastRemindedAtUtc { get; private set; }

    public static readonly TimeSpan ManualReminderCooldown = TimeSpan.FromHours(24);

    private CustomerLinkRequest(
        CustomerLinkRequestId id,
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        string normalizedEmail,
        PlatformUserId? targetUserIdentityId,
        string? targetPublicUserId,
        CustomerLinkRequestStatus status,
        string tokenHash,
        PlatformUserId? invitedByUserId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset? declinedAtUtc,
        DateTimeOffset? revokedAtUtc,
        PlatformUserId? acceptedByUserId,
        int reminderCount = 0,
        DateTimeOffset? lastRemindedAtUtc = null)
    {
        Id = id;
        OrganizationId = organizationId;
        BusinessCustomerId = businessCustomerId;
        NormalizedEmail = normalizedEmail;
        TargetUserIdentityId = targetUserIdentityId;
        TargetPublicUserId = targetPublicUserId;
        Status = status;
        TokenHash = tokenHash;
        InvitedByUserId = invitedByUserId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        AcceptedAtUtc = acceptedAtUtc;
        DeclinedAtUtc = declinedAtUtc;
        RevokedAtUtc = revokedAtUtc;
        AcceptedByUserId = acceptedByUserId;
        ReminderCount = reminderCount;
        LastRemindedAtUtc = lastRemindedAtUtc;
    }

    public static (CustomerLinkRequest Request, string AcceptToken) Create(
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        string email,
        DateTimeOffset utcNow,
        PlatformUserId? invitedByUserId = null,
        TimeSpan? lifetime = null,
        CustomerLinkRequestId? id = null,
        PlatformUserId? targetUserIdentityId = null,
        string? targetPublicUserId = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(businessCustomerId);
        EnsureUtc(utcNow);

        var normalizedPublicId = NormalizeOptionalPublicUserId(targetPublicUserId);
        if (targetUserIdentityId is null && normalizedPublicId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerLinkRequestTargetMismatch,
                "Target public ExItS ID requires a target user identity.");
        }

        var acceptToken = CreateAcceptToken();
        var request = new CustomerLinkRequest(
            id ?? CustomerLinkRequestId.New(),
            organizationId,
            businessCustomerId,
            PlatformUser.NormalizeEmail(email),
            targetUserIdentityId,
            normalizedPublicId,
            CustomerLinkRequestStatus.Pending,
            HashToken(acceptToken),
            invitedByUserId,
            utcNow,
            utcNow,
            utcNow.Add(lifetime ?? TimeSpan.FromHours(DefaultLifetimeHours)),
            null,
            null,
            null,
            null);
        return (request, acceptToken);
    }

    public static CustomerLinkRequest Rehydrate(
        CustomerLinkRequestId id,
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        string normalizedEmail,
        CustomerLinkRequestStatus status,
        string tokenHash,
        PlatformUserId? invitedByUserId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? acceptedAtUtc,
        DateTimeOffset? declinedAtUtc,
        DateTimeOffset? revokedAtUtc,
        PlatformUserId? acceptedByUserId,
        PlatformUserId? targetUserIdentityId = null,
        string? targetPublicUserId = null,
        int reminderCount = 0,
        DateTimeOffset? lastRemindedAtUtc = null) =>
        new(
            id,
            organizationId,
            businessCustomerId,
            normalizedEmail,
            targetUserIdentityId,
            targetPublicUserId,
            status,
            tokenHash,
            invitedByUserId,
            createdAtUtc,
            updatedAtUtc,
            expiresAtUtc,
            acceptedAtUtc,
            declinedAtUtc,
            revokedAtUtc,
            acceptedByUserId,
            reminderCount,
            lastRemindedAtUtc);

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

    /// <summary>
    /// Manual Org reminder for a Pending request. Does not rotate the accept token.
    /// Enforces a 24-hour cooldown between manual reminders.
    /// </summary>
    public int RecordReminder(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        if (LastRemindedAtUtc is DateTimeOffset last
            && utcNow < last.Add(ManualReminderCooldown))
        {
            throw new DomainException(
                DomainErrorCodes.CustomerLinkReminderTooSoon,
                "A reminder was sent recently. Try again later.");
        }

        ReminderCount += 1;
        LastRemindedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        return ReminderCount;
    }

    public DateTimeOffset? NextReminderEligibleAtUtc =>
        LastRemindedAtUtc is DateTimeOffset last
            ? last.Add(ManualReminderCooldown)
            : null;

    public void Revoke(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status is CustomerLinkRequestStatus.Active
            or CustomerLinkRequestStatus.Revoked
            or CustomerLinkRequestStatus.Declined)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerLinkRequestStatusTransition,
                $"Cannot revoke a customer link request in status {Status}.");
        }

        Status = CustomerLinkRequestStatus.Revoked;
        RevokedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Decline(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);
        Status = CustomerLinkRequestStatus.Declined;
        DeclinedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Accept(PlatformUserId acceptedByUserId, string email, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(acceptedByUserId);
        EnsureUtc(utcNow);
        EnsurePendingUsable(utcNow);

        if (TargetUserIdentityId is not null && TargetUserIdentityId != acceptedByUserId)
        {
            throw new DomainException(
                DomainErrorCodes.CustomerLinkRequestTargetMismatch,
                "Customer link request was issued to a different ExItS identity.");
        }

        var normalized = PlatformUser.NormalizeEmail(email);
        if (!string.Equals(normalized, NormalizedEmail, StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.CustomerLinkRequestEmailMismatch,
                "Customer link email does not match the accepting user.");
        }

        Status = CustomerLinkRequestStatus.Active;
        AcceptedAtUtc = utcNow;
        AcceptedByUserId = acceptedByUserId;
        UpdatedAtUtc = utcNow;
    }

    public void MarkExpired(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status != CustomerLinkRequestStatus.Pending)
        {
            return;
        }

        if (utcNow < ExpiresAtUtc)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerLinkRequestStatusTransition,
                "Customer link request has not expired yet.");
        }

        Status = CustomerLinkRequestStatus.Expired;
        UpdatedAtUtc = utcNow;
    }

    public bool IsExpired(DateTimeOffset utcNow) =>
        Status == CustomerLinkRequestStatus.Pending && utcNow >= ExpiresAtUtc;

    public bool IsTargetedTo(PlatformUserId userId) =>
        TargetUserIdentityId is not null && TargetUserIdentityId == userId;

    public static string HashToken(string acceptToken)
    {
        if (string.IsNullOrWhiteSpace(acceptToken))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerLinkRequestToken,
                "Customer link token cannot be blank.");
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(acceptToken.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private void EnsurePendingUsable(DateTimeOffset utcNow)
    {
        if (Status != CustomerLinkRequestStatus.Pending)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerLinkRequestStatusTransition,
                $"Customer link request is not pending (status {Status}).");
        }

        if (utcNow >= ExpiresAtUtc)
        {
            Status = CustomerLinkRequestStatus.Expired;
            UpdatedAtUtc = utcNow;
            throw new DomainException(
                DomainErrorCodes.CustomerLinkRequestExpired,
                "Customer link request has expired.");
        }
    }

    private static string? NormalizeOptionalPublicUserId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return PublicUserIdRules.Normalize(value.Trim());
    }

    private static string CreateAcceptToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}
