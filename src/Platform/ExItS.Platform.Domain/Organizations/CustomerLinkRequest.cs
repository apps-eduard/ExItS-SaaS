using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Explicit Customer Link Request. Acceptance links one Platform User to one Business Customer —
/// never Organization Staff membership and never a product-local role.
/// </summary>
public sealed class CustomerLinkRequest
{
    public const int DefaultLifetimeHours = 24 * 7;
    public const string InvitationType = InvitationKinds.CustomerLinkRequest;

    public CustomerLinkRequestId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public BusinessCustomerId BusinessCustomerId { get; }
    public string NormalizedEmail { get; private set; }
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

    private CustomerLinkRequest(
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
        PlatformUserId? acceptedByUserId)
    {
        Id = id;
        OrganizationId = organizationId;
        BusinessCustomerId = businessCustomerId;
        NormalizedEmail = normalizedEmail;
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
    }

    public static (CustomerLinkRequest Request, string AcceptToken) Create(
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        string email,
        DateTimeOffset utcNow,
        PlatformUserId? invitedByUserId = null,
        TimeSpan? lifetime = null,
        CustomerLinkRequestId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(businessCustomerId);
        EnsureUtc(utcNow);

        var acceptToken = CreateAcceptToken();
        var request = new CustomerLinkRequest(
            id ?? CustomerLinkRequestId.New(),
            organizationId,
            businessCustomerId,
            PlatformUser.NormalizeEmail(email),
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
        PlatformUserId? acceptedByUserId) =>
        new(
            id,
            organizationId,
            businessCustomerId,
            normalizedEmail,
            status,
            tokenHash,
            invitedByUserId,
            createdAtUtc,
            updatedAtUtc,
            expiresAtUtc,
            acceptedAtUtc,
            declinedAtUtc,
            revokedAtUtc,
            acceptedByUserId);

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
