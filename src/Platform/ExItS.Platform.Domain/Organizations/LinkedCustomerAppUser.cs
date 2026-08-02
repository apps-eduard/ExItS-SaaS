using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Result of accepting a Customer Link Request: a Platform User Identity linked to one Business Customer.
/// Never Organization Staff membership and never a product-local role.
/// </summary>
public sealed class LinkedCustomerAppUser
{
    public LinkedCustomerAppUserId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public BusinessCustomerId BusinessCustomerId { get; }
    public PlatformUserId UserIdentityId { get; }
    public CustomerLinkRequestId SourceLinkRequestId { get; }
    public LinkedCustomerAppUserStatus Status { get; private set; }
    public DateTimeOffset LinkedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    private LinkedCustomerAppUser(
        LinkedCustomerAppUserId id,
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        PlatformUserId userIdentityId,
        CustomerLinkRequestId sourceLinkRequestId,
        LinkedCustomerAppUserStatus status,
        DateTimeOffset linkedAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? revokedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        BusinessCustomerId = businessCustomerId;
        UserIdentityId = userIdentityId;
        SourceLinkRequestId = sourceLinkRequestId;
        Status = status;
        LinkedAtUtc = linkedAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        RevokedAtUtc = revokedAtUtc;
    }

    public static LinkedCustomerAppUser CreateFromAcceptedLink(
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        PlatformUserId userIdentityId,
        CustomerLinkRequestId sourceLinkRequestId,
        DateTimeOffset utcNow,
        LinkedCustomerAppUserId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(businessCustomerId);
        ArgumentNullException.ThrowIfNull(userIdentityId);
        ArgumentNullException.ThrowIfNull(sourceLinkRequestId);
        EnsureUtc(utcNow);

        return new LinkedCustomerAppUser(
            id ?? LinkedCustomerAppUserId.New(),
            organizationId,
            businessCustomerId,
            userIdentityId,
            sourceLinkRequestId,
            LinkedCustomerAppUserStatus.Active,
            utcNow,
            utcNow,
            null);
    }

    public static LinkedCustomerAppUser Rehydrate(
        LinkedCustomerAppUserId id,
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        PlatformUserId userIdentityId,
        CustomerLinkRequestId sourceLinkRequestId,
        LinkedCustomerAppUserStatus status,
        DateTimeOffset linkedAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? revokedAtUtc) =>
        new(
            id,
            organizationId,
            businessCustomerId,
            userIdentityId,
            sourceLinkRequestId,
            status,
            linkedAtUtc,
            updatedAtUtc,
            revokedAtUtc);

    public void Revoke(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == LinkedCustomerAppUserStatus.Revoked)
        {
            return;
        }

        Status = LinkedCustomerAppUserStatus.Revoked;
        RevokedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public bool IsOrganizationStaff => false;
    public bool GrantsProductRole => false;

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}
