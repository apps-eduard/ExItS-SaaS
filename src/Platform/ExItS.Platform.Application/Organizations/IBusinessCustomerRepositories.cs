using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface IBusinessCustomerRepository
{
    Task<BusinessCustomer?> GetByIdAsync(BusinessCustomerId id, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<BusinessCustomer> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        string? owningProductCode,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessCustomer>> ListByIdsAsync(
        IReadOnlyCollection<BusinessCustomerId> ids,
        CancellationToken cancellationToken = default);

    Task AddAsync(BusinessCustomer customer, CancellationToken cancellationToken = default);

    Task UpdateAsync(BusinessCustomer customer, CancellationToken cancellationToken = default);
}

public interface ICreditCustomerRepository
{
    Task<CreditCustomer?> GetByIdAsync(CreditCustomerId id, CancellationToken cancellationToken = default);

    Task<CreditCustomer?> FindActiveByBusinessCustomerAsync(
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CreditCustomer> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(CreditCustomer creditCustomer, CancellationToken cancellationToken = default);

    Task UpdateAsync(CreditCustomer creditCustomer, CancellationToken cancellationToken = default);
}

public interface ICustomerLinkRequestRepository
{
    Task<CustomerLinkRequest?> GetByIdAsync(CustomerLinkRequestId id, CancellationToken cancellationToken = default);

    Task<CustomerLinkRequest?> FindPendingByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<CustomerLinkRequest?> FindPendingByBusinessCustomerAsync(
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default);

    Task<CustomerLinkRequest?> FindPendingByOrganizationAndTargetUserAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId targetUserIdentityId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerLinkRequest> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CustomerLinkRequestStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerLinkRequest>> ListPendingForTargetUserAsync(
        PlatformUserId targetUserIdentityId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Non-pending requests targeting the Personal user (Accepted/Active, Declined, Expired, Revoked), newest first.
    /// </summary>
    Task<IReadOnlyList<CustomerLinkRequest>> ListResolvedForTargetUserAsync(
        PlatformUserId targetUserIdentityId,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerLinkRequest>> ListByBusinessCustomerAsync(
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts requests by effective status for an organization.
    /// Pending rows whose <c>ExpiresAtUtc</c> has passed are counted as Expired
    /// (effective expiry), not as Pending — stored status may still be Pending until marked.
    /// </summary>
    Task<IReadOnlyDictionary<string, int>> CountByOrganizationGroupedAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(CustomerLinkRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(CustomerLinkRequest request, CancellationToken cancellationToken = default);
}

public interface IOrganizationInAppNotificationRepository
{
    Task<OrganizationInAppNotification?> GetByIdAsync(
        OrganizationInAppNotificationId id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists inbox items for a recipient. When <paramref name="branchId"/> is supplied the read is a
    /// branch workspace inbox: branch-targeted rows for that branch plus organization-wide (null) rows.
    /// Null lists everything (global organization inbox).
    /// </summary>
    Task<IReadOnlyList<OrganizationInAppNotification>> ListForRecipientInOrganizationAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId recipientUserIdentityId,
        int take,
        CancellationToken cancellationToken = default,
        Guid? branchId = null);

    Task<OrganizationInAppNotification?> FindByRecipientRelatedAsync(
        PlatformUserId recipientUserIdentityId,
        string relatedType,
        string relatedId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationInAppNotification>> ListByOrganizationRelatedAsync(
        PlatformOrganizationId organizationId,
        string relatedType,
        string relatedId,
        CancellationToken cancellationToken = default);

    Task AddAsync(OrganizationInAppNotification notification, CancellationToken cancellationToken = default);

    Task UpdateAsync(OrganizationInAppNotification notification, CancellationToken cancellationToken = default);
}

public interface ILinkedCustomerAppUserRepository
{
    Task<LinkedCustomerAppUser?> GetByIdAsync(
        LinkedCustomerAppUserId id,
        CancellationToken cancellationToken = default);

    Task<LinkedCustomerAppUser?> FindActiveByBusinessCustomerAsync(
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default);

    Task<LinkedCustomerAppUser?> FindActiveByUserAndOrganizationAsync(
        PlatformUserId userIdentityId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<LinkedCustomerAppUser?> FindActiveByUserOrganizationAndBusinessCustomerAsync(
        PlatformUserId userIdentityId,
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<LinkedCustomerAppUser> Items, int TotalCount)> ListActiveByUserAsync(
        PlatformUserId userIdentityId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<LinkedCustomerAppUser> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LinkedCustomerAppUser>> ListActiveByUserAndOrganizationAsync(
        PlatformUserId userIdentityId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(LinkedCustomerAppUser link, CancellationToken cancellationToken = default);

    Task UpdateAsync(LinkedCustomerAppUser link, CancellationToken cancellationToken = default);
}

public interface IPersonalOrganizationConnectionBlockRepository
{
    Task<PersonalOrganizationConnectionBlock?> GetByIdAsync(
        PersonalOrganizationConnectionBlockId id,
        CancellationToken cancellationToken = default);

    Task<PersonalOrganizationConnectionBlock?> FindByPersonalAndOrganizationAsync(
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<PersonalOrganizationConnectionBlock?> FindActiveByPersonalAndOrganizationAsync(
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PersonalOrganizationConnectionBlock>> ListActiveByPersonalUserAsync(
        PlatformUserId personalUserIdentityId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PersonalOrganizationConnectionBlock block, CancellationToken cancellationToken = default);

    Task UpdateAsync(PersonalOrganizationConnectionBlock block, CancellationToken cancellationToken = default);
}
