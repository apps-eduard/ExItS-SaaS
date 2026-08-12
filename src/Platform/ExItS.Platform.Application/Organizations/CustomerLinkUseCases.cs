using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record CustomerLinkRequestDto(
    Guid Id,
    Guid OrganizationId,
    Guid BusinessCustomerId,
    string InvitationType,
    string Email,
    string Status,
    Guid? InvitedByUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? DeclinedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? AcceptedByUserId,
    string? AcceptToken = null);

public sealed record LinkedCustomerAppUserDto(
    Guid Id,
    Guid OrganizationId,
    Guid BusinessCustomerId,
    Guid UserIdentityId,
    Guid SourceLinkRequestId,
    string Status,
    bool IsOrganizationStaff,
    bool GrantedProductRole,
    DateTimeOffset LinkedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record AcceptCustomerLinkResultDto(
    Guid LinkRequestId,
    Guid BusinessCustomerId,
    Guid LinkedUserIdentityId,
    Guid LinkedCustomerAppUserId,
    bool CreatedOrganizationMembership,
    bool GrantedStaffRole,
    bool GrantedProductRole);

public sealed class CustomerLinkRequestQueryService
{
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IClock _clock;

    public CustomerLinkRequestQueryService(ICustomerLinkRequestRepository requests, IClock clock)
    {
        _requests = requests;
        _clock = clock;
    }

    public async Task<CustomerLinkRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _requests.GetByIdAsync(CustomerLinkRequestId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return request is null ? null : Map(request, effectiveNow: _clock.UtcNow);
    }

    public async Task<PagedResult<CustomerLinkRequestDto>> ListByOrganizationAsync(
        Guid organizationId,
        CustomerLinkRequestStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _requests
            .ListByOrganizationAsync(PlatformOrganizationId.From(organizationId), status, skip, take, cancellationToken)
            .ConfigureAwait(false);
        var now = _clock.UtcNow;
        return new PagedResult<CustomerLinkRequestDto>(
            items.Select(i => Map(i, effectiveNow: now)).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static CustomerLinkRequestDto Map(
        CustomerLinkRequest request,
        string? acceptToken = null,
        DateTimeOffset? effectiveNow = null) =>
        new(
            request.Id.Value,
            request.OrganizationId.Value,
            request.BusinessCustomerId.Value,
            CustomerLinkRequest.InvitationType,
            request.NormalizedEmail,
            effectiveNow is not null && request.IsExpired(effectiveNow.Value)
                ? nameof(CustomerLinkRequestStatus.Expired)
                : request.Status.ToString(),
            request.InvitedByUserId?.Value,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            request.ExpiresAtUtc,
            request.AcceptedAtUtc,
            request.DeclinedAtUtc,
            request.RevokedAtUtc,
            request.AcceptedByUserId?.Value,
            acceptToken);
}

public sealed class LinkedCustomerAppUserQueryService
{
    private readonly ILinkedCustomerAppUserRepository _links;

    public LinkedCustomerAppUserQueryService(ILinkedCustomerAppUserRepository links) => _links = links;

    public async Task<PagedResult<LinkedCustomerAppUserDto>> ListByOrganizationAsync(
        Guid organizationId,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _links
            .ListByOrganizationAsync(PlatformOrganizationId.From(organizationId), skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<LinkedCustomerAppUserDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static LinkedCustomerAppUserDto Map(LinkedCustomerAppUser link) =>
        new(
            link.Id.Value,
            link.OrganizationId.Value,
            link.BusinessCustomerId.Value,
            link.UserIdentityId.Value,
            link.SourceLinkRequestId.Value,
            link.Status.ToString(),
            IsOrganizationStaff: false,
            GrantedProductRole: false,
            link.LinkedAtUtc,
            link.UpdatedAtUtc);
}

public sealed class CreateCustomerLinkRequest
{
    private readonly IBusinessCustomerRepository _customers;
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateCustomerLinkRequest(
        IBusinessCustomerRepository customers,
        ICustomerLinkRequestRepository requests,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _customers = customers;
        _requests = requests;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CustomerLinkRequestDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        string email,
        PlatformUserId? invitedByUserId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(businessCustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.OrganizationId != organizationId)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(
                ApplicationErrorCodes.BusinessCustomerNotFound,
                "Business customer was not found.");
        }

        CustomerStaffSeparationGuard.EnsureNotTreatedAsStaff(customer);
        if (customer.LinkedUserIdentityId is not null)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestConflict,
                "Business customer is already linked to a user.");
        }

        try
        {
            var pending = await _requests
                .FindPendingByBusinessCustomerAsync(businessCustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (pending is not null && !pending.IsExpired(_clock.UtcNow))
            {
                return ApplicationResult<CustomerLinkRequestDto>.Failure(
                    ApplicationErrorCodes.CustomerLinkRequestConflict,
                    "A pending customer link request already exists for this business customer.");
            }

            if (pending is not null && pending.IsExpired(_clock.UtcNow))
            {
                pending.MarkExpired(_clock.UtcNow);
                await _requests.UpdateAsync(pending, cancellationToken).ConfigureAwait(false);
            }

            var (request, acceptToken) = CustomerLinkRequest.Create(
                organizationId,
                businessCustomerId,
                email,
                _clock.UtcNow,
                invitedByUserId);
            await _requests.AddAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CustomerLinkRequestDto>.Success(
                CustomerLinkRequestQueryService.Map(request, acceptToken));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ResendCustomerLinkRequest
{
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ResendCustomerLinkRequest(
        ICustomerLinkRequestRepository requests,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CustomerLinkRequestDto>> ExecuteAsync(
        CustomerLinkRequestId requestId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (request is null || request.OrganizationId != organizationId)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        try
        {
            if (request.IsExpired(_clock.UtcNow))
            {
                request.MarkExpired(_clock.UtcNow);
                await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<CustomerLinkRequestDto>.Failure(
                    DomainErrorCodes.CustomerLinkRequestExpired,
                    "Customer link request has expired. Create a new request.");
            }

            var acceptToken = request.Resend(_clock.UtcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CustomerLinkRequestDto>.Success(
                CustomerLinkRequestQueryService.Map(request, acceptToken));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokeCustomerLinkRequest
{
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeCustomerLinkRequest(
        ICustomerLinkRequestRepository requests,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CustomerLinkRequestDto>> ExecuteAsync(
        CustomerLinkRequestId requestId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (request is null || request.OrganizationId != organizationId)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        try
        {
            request.Revoke(_clock.UtcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CustomerLinkRequestDto>.Success(
                CustomerLinkRequestQueryService.Map(request));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeclineCustomerLinkRequest
{
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeclineCustomerLinkRequest(
        ICustomerLinkRequestRepository requests,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CustomerLinkRequestDto>> ExecuteAsync(
        string acceptToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(acceptToken))
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        var hash = CustomerLinkRequest.HashToken(acceptToken);
        var request = await _requests.FindPendingByTokenHashAsync(hash, cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        try
        {
            if (request.IsExpired(_clock.UtcNow))
            {
                request.MarkExpired(_clock.UtcNow);
                await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<CustomerLinkRequestDto>.Failure(
                    ApplicationErrorCodes.CustomerLinkRequestNotFound,
                    "Customer link request was not found.");
            }

            request.Decline(_clock.UtcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CustomerLinkRequestDto>.Success(
                CustomerLinkRequestQueryService.Map(request));
        }
        catch (DomainException)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }
    }
}

public sealed class AcceptCustomerLinkRequest
{
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IBusinessCustomerRepository _customers;
    private readonly ILinkedCustomerAppUserRepository _links;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AcceptCustomerLinkRequest(
        ICustomerLinkRequestRepository requests,
        IBusinessCustomerRepository customers,
        ILinkedCustomerAppUserRepository links,
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _customers = customers;
        _links = links;
        _memberships = memberships;
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<AcceptCustomerLinkResultDto>> ExecuteAsync(
        string acceptToken,
        PlatformUserId acceptingUserId,
        AccountClass accountClass,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(acceptToken))
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        var user = await _users.GetByIdAsync(acceptingUserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                DomainErrorCodes.UserNotActive,
                "Accepting a customer link requires an active user.");
        }

        if (accountClass != AccountClass.Personal)
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                ApplicationErrorCodes.AccountScopeDenied,
                "Accepting a customer link requires a Personal session.");
        }

        if (user.IsOrganizationScopedStaff
            || user.HomeOrganizationId is not null
            || !string.IsNullOrWhiteSpace(user.StaffNumber))
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                DomainErrorCodes.CustomerLinkPersonalIdentityRequired,
                "Only a Personal identity can accept a customer link.");
        }

        var hash = CustomerLinkRequest.HashToken(acceptToken);
        var request = await _requests.FindPendingByTokenHashAsync(hash, cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        try
        {
            if (request.IsExpired(_clock.UtcNow))
            {
                request.MarkExpired(_clock.UtcNow);
                await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                    ApplicationErrorCodes.CustomerLinkRequestNotFound,
                    "Customer link request was not found.");
            }

            var customer = await _customers.GetByIdAsync(request.BusinessCustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (customer is null || customer.OrganizationId != request.OrganizationId)
            {
                return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                    ApplicationErrorCodes.BusinessCustomerNotFound,
                    "Business customer was not found.");
            }

            CustomerStaffSeparationGuard.EnsureNotTreatedAsStaff(customer);

            // Capture membership count before accept to prove no staff membership was created.
            var membershipBefore = await _memberships
                .FindCurrentByUserAndOrganizationAsync(acceptingUserId, request.OrganizationId, cancellationToken)
                .ConfigureAwait(false);

            request.Accept(acceptingUserId, user.NormalizedEmail, _clock.UtcNow);
            customer.LinkAppUser(acceptingUserId, _clock.UtcNow);
            var link = LinkedCustomerAppUser.CreateFromAcceptedLink(
                request.OrganizationId,
                request.BusinessCustomerId,
                acceptingUserId,
                request.Id,
                _clock.UtcNow);

            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _links.AddAsync(link, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var membershipAfter = await _memberships
                .FindCurrentByUserAndOrganizationAsync(acceptingUserId, request.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            var createdMembership = membershipBefore is null && membershipAfter is not null;
            CustomerStaffSeparationGuard.EnsureCustomerLinkDoesNotGrantStaff(
                createdMembership,
                grantedStaffRole: false);

            return ApplicationResult<AcceptCustomerLinkResultDto>.Success(
                new AcceptCustomerLinkResultDto(
                    request.Id.Value,
                    customer.Id.Value,
                    acceptingUserId.Value,
                    link.Id.Value,
                    CreatedOrganizationMembership: false,
                    GrantedStaffRole: false,
                    GrantedProductRole: false));
        }
        catch (DomainException ex) when (
            ex.ErrorCode is DomainErrorCodes.CustomerLinkRequestEmailMismatch
                or DomainErrorCodes.CustomerLinkMustNotCreateStaff
                or DomainErrorCodes.CustomerLinkPersonalIdentityRequired
                or DomainErrorCodes.BusinessCustomerAlreadyLinked)
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException)
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }
    }
}

public sealed class UnlinkAcceptedCustomerLink
{
    private readonly ILinkedCustomerAppUserRepository _links;
    private readonly IBusinessCustomerRepository _customers;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UnlinkAcceptedCustomerLink(
        ILinkedCustomerAppUserRepository links,
        IBusinessCustomerRepository customers,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _links = links;
        _customers = customers;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<LinkedCustomerAppUserDto>> ExecuteAsync(
        LinkedCustomerAppUserId linkId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var link = await _links.GetByIdAsync(linkId, cancellationToken).ConfigureAwait(false);
        if (link is null || link.OrganizationId != organizationId)
        {
            return ApplicationResult<LinkedCustomerAppUserDto>.Failure(
                ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
                "Linked customer was not found.");
        }

        return await RevokeCoreAsync(link, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApplicationResult<LinkedCustomerAppUserDto>> ExecuteForOwnerAsync(
        LinkedCustomerAppUserId linkId,
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default)
    {
        var link = await _links.GetByIdAsync(linkId, cancellationToken).ConfigureAwait(false);
        if (link is null || link.UserIdentityId != userIdentityId)
        {
            return ApplicationResult<LinkedCustomerAppUserDto>.Failure(
                ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
                "Linked customer was not found.");
        }

        return await RevokeCoreAsync(link, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApplicationResult<LinkedCustomerAppUserDto>> RevokeCoreAsync(
        LinkedCustomerAppUser link,
        CancellationToken cancellationToken)
    {
        try
        {
            if (link.Status == LinkedCustomerAppUserStatus.Revoked)
            {
                return ApplicationResult<LinkedCustomerAppUserDto>.Success(
                    LinkedCustomerAppUserQueryService.Map(link));
            }

            link.Revoke(_clock.UtcNow);
            var customer = await _customers.GetByIdAsync(link.BusinessCustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (customer is not null
                && customer.OrganizationId == link.OrganizationId
                && customer.LinkedUserIdentityId == link.UserIdentityId)
            {
                customer.UnlinkAppUser(_clock.UtcNow);
                await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            }

            await _links.UpdateAsync(link, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<LinkedCustomerAppUserDto>.Success(
                LinkedCustomerAppUserQueryService.Map(link));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<LinkedCustomerAppUserDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Staff must not read unrelated personal utang records of linked customer app users.
/// </summary>
public sealed class DenyStaffAccessToUnrelatedPersonalRecords
{
    public ApplicationResult<object> Execute() =>
        ApplicationResult<object>.Failure(
            DomainErrorCodes.StaffCannotAccessUnrelatedPersonalRecords,
            "Organization staff roles cannot expose unrelated personal utang records.");
}
