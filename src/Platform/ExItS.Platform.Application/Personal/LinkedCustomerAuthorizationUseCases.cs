using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Personal;

/// <summary>
/// Platform-side proof that the current Personal identity has an active accepted
/// customer link for a specific organization and BusinessCustomer.
/// Does not include POS correlation (POS database is a separate authority).
/// </summary>
public sealed record AuthorizedLinkedCustomerPlatformContext(
    Guid PersonalUserId,
    Guid OrganizationId,
    Guid PlatformBusinessCustomerId,
    Guid LinkedCustomerAppUserId,
    bool AllowDeliveryBeyondNormalDistance = false);

/// <summary>
/// Fail-closed authorization for linked-customer statement access (Platform facts only).
/// The authenticated Personal identity must come from server session context — never a client userId.
/// </summary>
public sealed class AuthorizeLinkedCustomerAccess
{
    private const string NotFoundMessage = "Linked customer was not found.";

    private readonly IPlatformUserRepository _users;
    private readonly ILinkedCustomerAppUserRepository _links;
    private readonly IBusinessCustomerRepository _customers;

    public AuthorizeLinkedCustomerAccess(
        IPlatformUserRepository users,
        ILinkedCustomerAppUserRepository links,
        IBusinessCustomerRepository customers)
    {
        _users = users;
        _links = links;
        _customers = customers;
    }

    public async Task<ApplicationResult<AuthorizedLinkedCustomerPlatformContext>> ExecuteAsync(
        PlatformUserId currentPersonalUser,
        AccountClass accountClass,
        PlatformOrganizationId organizationId,
        BusinessCustomerId platformBusinessCustomerId,
        CancellationToken cancellationToken = default)
    {
        if (accountClass != AccountClass.Personal)
        {
            return ApplicationResult<AuthorizedLinkedCustomerPlatformContext>.Failure(
                ApplicationErrorCodes.AccountScopeDenied,
                "Linked customer access requires a Personal session.");
        }

        var user = await _users.GetByIdAsync(currentPersonalUser, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<AuthorizedLinkedCustomerPlatformContext>.Failure(
                DomainErrorCodes.UserNotActive,
                "Linked customer access requires an active user.");
        }

        if (user.IsOrganizationScopedStaff
            || user.HomeOrganizationId is not null
            || !string.IsNullOrWhiteSpace(user.StaffNumber))
        {
            return ApplicationResult<AuthorizedLinkedCustomerPlatformContext>.Failure(
                DomainErrorCodes.CustomerLinkPersonalIdentityRequired,
                "Only a Personal identity can access a linked customer.");
        }

        var link = await _links
            .FindActiveByUserOrganizationAndBusinessCustomerAsync(
                currentPersonalUser,
                organizationId,
                platformBusinessCustomerId,
                cancellationToken)
            .ConfigureAwait(false);
        if (link is null
            || link.Status != LinkedCustomerAppUserStatus.Active
            || link.UserIdentityId != currentPersonalUser
            || link.OrganizationId != organizationId
            || link.BusinessCustomerId != platformBusinessCustomerId)
        {
            return NotFound();
        }

        var customer = await _customers.GetByIdAsync(platformBusinessCustomerId, cancellationToken)
            .ConfigureAwait(false);
        if (customer is null
            || customer.OrganizationId != organizationId
            || customer.Status == BusinessCustomerStatus.Archived
            || customer.LinkedUserIdentityId != currentPersonalUser)
        {
            return NotFound();
        }

        try
        {
            CustomerStaffSeparationGuard.EnsureNotTreatedAsStaff(customer);
        }
        catch (DomainException)
        {
            return NotFound();
        }

        return ApplicationResult<AuthorizedLinkedCustomerPlatformContext>.Success(
            new AuthorizedLinkedCustomerPlatformContext(
                currentPersonalUser.Value,
                organizationId.Value,
                platformBusinessCustomerId.Value,
                link.Id.Value,
                customer.AllowDeliveryBeyondNormalDistance));
    }

    private static ApplicationResult<AuthorizedLinkedCustomerPlatformContext> NotFound() =>
        ApplicationResult<AuthorizedLinkedCustomerPlatformContext>.Failure(
            ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
            NotFoundMessage);
}
