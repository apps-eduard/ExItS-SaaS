using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;

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
    string? AcceptToken = null,
    Guid? TargetUserIdentityId = null,
    string? TargetPublicUserId = null);

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

public sealed record CustomerLinkRequestStatsDto(
    Guid OrganizationId,
    IReadOnlyDictionary<string, int> CountsByStatus);

public sealed record CustomerLinkStatusDto(
    Guid BusinessCustomerId,
    Guid OrganizationId,
    string Status,
    Guid? LinkedUserIdentityId,
    Guid? LatestLinkRequestId,
    string? LatestLinkRequestStatus);

public sealed record CreateBusinessCustomerWithPersonalLinkResultDto(
    BusinessCustomerDto Customer,
    CustomerLinkRequestDto LinkRequest);

/// <summary>Personal inbox projection for pending merchant customer-link requests.</summary>
public sealed record PersonalPendingCustomerLinkRequestDto(
    Guid Id,
    Guid OrganizationId,
    string OrganizationDisplayName,
    Guid BusinessCustomerId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? TargetPublicUserId);

public sealed record OrganizationInAppNotificationDto(
    Guid Id,
    Guid OrganizationId,
    Guid RecipientUserIdentityId,
    string Title,
    string Preview,
    string RelatedType,
    string? RelatedId,
    bool IsRead,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

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

    public async Task<IReadOnlyList<CustomerLinkRequestDto>> ListByBusinessCustomerAsync(
        Guid organizationId,
        Guid businessCustomerId,
        CancellationToken cancellationToken = default)
    {
        var items = await _requests
            .ListByBusinessCustomerAsync(BusinessCustomerId.From(businessCustomerId), cancellationToken)
            .ConfigureAwait(false);
        var now = _clock.UtcNow;
        var orgId = PlatformOrganizationId.From(organizationId);
        return items
            .Where(i => i.OrganizationId == orgId)
            .Select(i => Map(i, effectiveNow: now))
            .ToList();
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
            acceptToken,
            request.TargetUserIdentityId?.Value,
            request.TargetPublicUserId);
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

public sealed class CustomerLinkRequestStatsQuery
{
    private readonly ICustomerLinkRequestRepository _requests;

    public CustomerLinkRequestStatsQuery(ICustomerLinkRequestRepository requests) => _requests = requests;

    public async Task<CustomerLinkRequestStatsDto> CountByOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var counts = await _requests
            .CountByOrganizationGroupedAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        return new CustomerLinkRequestStatsDto(organizationId.Value, counts);
    }
}

public sealed class GetCustomerLinkStatusForBusinessCustomer
{
    private readonly IBusinessCustomerRepository _customers;
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IClock _clock;

    public GetCustomerLinkStatusForBusinessCustomer(
        IBusinessCustomerRepository customers,
        ICustomerLinkRequestRepository requests,
        IClock clock)
    {
        _customers = customers;
        _requests = requests;
        _clock = clock;
    }

    public async Task<ApplicationResult<CustomerLinkStatusDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        CancellationToken cancellationToken = default)
    {
        var customer = await _customers.GetByIdAsync(businessCustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.OrganizationId != organizationId)
        {
            return ApplicationResult<CustomerLinkStatusDto>.Failure(
                ApplicationErrorCodes.BusinessCustomerNotFound,
                "Business customer was not found.");
        }

        var history = await _requests
            .ListByBusinessCustomerAsync(businessCustomerId, cancellationToken)
            .ConfigureAwait(false);
        var latest = history.OrderByDescending(r => r.CreatedAtUtc).FirstOrDefault();
        var now = _clock.UtcNow;

        string status;
        if (customer.LinkedUserIdentityId is not null)
        {
            status = "Linked";
        }
        else if (latest is null)
        {
            status = "NotLinked";
        }
        else if (latest.IsExpired(now) || latest.Status == CustomerLinkRequestStatus.Expired)
        {
            status = nameof(CustomerLinkRequestStatus.Expired);
        }
        else
        {
            status = latest.Status switch
            {
                CustomerLinkRequestStatus.Pending => nameof(CustomerLinkRequestStatus.Pending),
                CustomerLinkRequestStatus.Active => "Linked",
                CustomerLinkRequestStatus.Declined => nameof(CustomerLinkRequestStatus.Declined),
                CustomerLinkRequestStatus.Revoked => nameof(CustomerLinkRequestStatus.Revoked),
                _ => latest.Status.ToString()
            };
        }

        return ApplicationResult<CustomerLinkStatusDto>.Success(
            new CustomerLinkStatusDto(
                customer.Id.Value,
                customer.OrganizationId.Value,
                status,
                customer.LinkedUserIdentityId?.Value,
                latest?.Id.Value,
                latest is null
                    ? null
                    : latest.IsExpired(now)
                        ? nameof(CustomerLinkRequestStatus.Expired)
                        : latest.Status.ToString()));
    }
}

public sealed class CreateCustomerLinkRequest
{
    private readonly IBusinessCustomerRepository _customers;
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPlatformUserRepository? _users;
    private readonly IPlatformOrganizationRepository? _organizations;
    private readonly IPersonalAccountSettingsRepository? _personalSettings;
    private readonly IPersonalInAppNotificationRepository? _personalNotifications;

    public CreateCustomerLinkRequest(
        IBusinessCustomerRepository customers,
        ICustomerLinkRequestRepository requests,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IPlatformUserRepository? users = null,
        IPlatformOrganizationRepository? organizations = null,
        IPersonalAccountSettingsRepository? personalSettings = null,
        IPersonalInAppNotificationRepository? personalNotifications = null)
    {
        _customers = customers;
        _requests = requests;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _users = users;
        _organizations = organizations;
        _personalSettings = personalSettings;
        _personalNotifications = personalNotifications;
    }

    public Task<ApplicationResult<CustomerLinkRequestDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        string email,
        PlatformUserId? invitedByUserId,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            organizationId,
            businessCustomerId,
            email,
            invitedByUserId,
            targetUserIdentityId: null,
            publicUserId: null,
            cancellationToken);

    public async Task<ApplicationResult<CustomerLinkRequestDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        BusinessCustomerId businessCustomerId,
        string? email,
        PlatformUserId? invitedByUserId,
        PlatformUserId? targetUserIdentityId,
        string? publicUserId,
        CancellationToken cancellationToken = default,
        bool persist = true,
        BusinessCustomer? knownCustomer = null)
    {
        var customer = knownCustomer
            ?? await _customers.GetByIdAsync(businessCustomerId, cancellationToken).ConfigureAwait(false);
        if (customer is null
            || customer.Id != businessCustomerId
            || customer.OrganizationId != organizationId)
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
            var resolved = await ResolveTargetAsync(
                    email,
                    targetUserIdentityId,
                    publicUserId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!resolved.IsSuccess)
            {
                return ApplicationResult<CustomerLinkRequestDto>.Failure(
                    resolved.ErrorCode!,
                    resolved.ErrorMessage!);
            }

            var (resolvedEmail, targetId, targetPublicId) = resolved.Value!;

            var pending = await _requests
                .FindPendingByBusinessCustomerAsync(businessCustomerId, cancellationToken)
                .ConfigureAwait(false);
            if (pending is not null && !pending.IsExpired(_clock.UtcNow))
            {
                // Idempotent retry: return existing pending without exposing a new AcceptToken.
                return ApplicationResult<CustomerLinkRequestDto>.Success(
                    CustomerLinkRequestQueryService.Map(pending, effectiveNow: _clock.UtcNow));
            }

            if (pending is not null && pending.IsExpired(_clock.UtcNow))
            {
                pending.MarkExpired(_clock.UtcNow);
                await _requests.UpdateAsync(pending, cancellationToken).ConfigureAwait(false);
            }

            var (request, acceptToken) = CustomerLinkRequest.Create(
                organizationId,
                businessCustomerId,
                resolvedEmail,
                _clock.UtcNow,
                invitedByUserId,
                targetUserIdentityId: targetId,
                targetPublicUserId: targetPublicId);
            await _requests.AddAsync(request, cancellationToken).ConfigureAwait(false);

            if (targetId is not null)
            {
                await TryCreatePersonalPendingNotificationAsync(
                        request,
                        targetId,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (persist)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return ApplicationResult<CustomerLinkRequestDto>.Success(
                CustomerLinkRequestQueryService.Map(request, acceptToken));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal async Task TryCreatePersonalPendingNotificationAsync(
        CustomerLinkRequest request,
        PlatformUserId targetUserId,
        CancellationToken cancellationToken)
    {
        if (_personalNotifications is null || _personalSettings is null)
        {
            return;
        }

        var settings = await _personalSettings.GetByUserAsync(targetUserId, cancellationToken).ConfigureAwait(false);
        var inAppEnabled = settings?.InAppNotificationsEnabled ?? true;
        if (!inAppEnabled)
        {
            return;
        }

        var relatedId = request.Id.Value.ToString("D");
        var existing = await _personalNotifications
            .FindByRecipientRelatedAsync(
                targetUserId,
                CustomerLinkNotificationTypes.PersonalPendingRequest,
                relatedId,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var merchantName = "a merchant";
        if (_organizations is not null)
        {
            var org = await _organizations.GetByIdAsync(request.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            if (org is not null && !string.IsNullOrWhiteSpace(org.DisplayName))
            {
                merchantName = org.DisplayName;
            }
        }

        var notification = PersonalInAppNotification.Create(
            targetUserId,
            title: "Customer link request",
            preview: $"{merchantName} added you as a customer and wants to link your ExItS account.",
            relatedType: CustomerLinkNotificationTypes.PersonalPendingRequest,
            utcNow: _clock.UtcNow,
            relatedId: relatedId);
        await _personalNotifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApplicationResult<(string Email, PlatformUserId? TargetId, string? TargetPublicId)>> ResolveTargetAsync(
        string? email,
        PlatformUserId? targetUserIdentityId,
        string? publicUserId,
        CancellationToken cancellationToken)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(email);
        var hasTargetId = targetUserIdentityId is not null;
        var hasPublicId = !string.IsNullOrWhiteSpace(publicUserId);

        if (!hasEmail && !hasTargetId && !hasPublicId)
        {
            return ApplicationResult<(string, PlatformUserId?, string?)>.Failure(
                DomainErrorCodes.InvalidEmail,
                "Email or target ExItS user is required.");
        }

        if (!hasTargetId && !hasPublicId)
        {
            return ApplicationResult<(string, PlatformUserId?, string?)>.Success(
                (email!.Trim(), null, null));
        }

        if (_users is null)
        {
            return ApplicationResult<(string, PlatformUserId?, string?)>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Target user resolution is unavailable.");
        }

        PlatformUser? user = null;
        if (hasTargetId)
        {
            user = await _users.GetByIdAsync(targetUserIdentityId!, cancellationToken).ConfigureAwait(false);
        }

        if (hasPublicId)
        {
            string normalizedPublic;
            try
            {
                normalizedPublic = PublicUserIdRules.Normalize(publicUserId);
            }
            catch (DomainException ex)
            {
                return ApplicationResult<(string, PlatformUserId?, string?)>.Failure(ex.ErrorCode, ex.Message);
            }

            var byPublic = await _users.GetByPublicUserIdAsync(normalizedPublic, cancellationToken)
                .ConfigureAwait(false);
            if (byPublic is null)
            {
                return ApplicationResult<(string, PlatformUserId?, string?)>.Failure(
                    ApplicationErrorCodes.UserNotFound,
                    "Target ExItS user was not found.");
            }

            if (user is not null && user.Id != byPublic.Id)
            {
                return ApplicationResult<(string, PlatformUserId?, string?)>.Failure(
                    DomainErrorCodes.CustomerLinkRequestTargetMismatch,
                    "Target user identity and public ExItS ID do not match.");
            }

            user = byPublic;
        }

        if (user is null)
        {
            return ApplicationResult<(string, PlatformUserId?, string?)>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Target ExItS user was not found.");
        }

        if (user.Status != AccountStatus.Active)
        {
            return ApplicationResult<(string, PlatformUserId?, string?)>.Failure(
                DomainErrorCodes.UserNotActive,
                "Target ExItS user must be active.");
        }

        if (user.IsOrganizationScopedStaff
            || user.HomeOrganizationId is not null
            || !string.IsNullOrWhiteSpace(user.StaffNumber))
        {
            return ApplicationResult<(string, PlatformUserId?, string?)>.Failure(
                DomainErrorCodes.CustomerLinkPersonalIdentityRequired,
                "Customer link targets must be Personal identities, not organization or platform staff.");
        }

        return ApplicationResult<(string, PlatformUserId?, string?)>.Success(
            (user.NormalizedEmail, user.Id, user.PublicUserId));
    }
}

public sealed class ResendCustomerLinkRequest
{
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly CreateCustomerLinkRequest? _createHelper;

    public ResendCustomerLinkRequest(
        ICustomerLinkRequestRepository requests,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IBusinessCustomerRepository? customers = null,
        IPlatformUserRepository? users = null,
        IPlatformOrganizationRepository? organizations = null,
        IPersonalAccountSettingsRepository? personalSettings = null,
        IPersonalInAppNotificationRepository? personalNotifications = null)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
        _clock = clock;
        if (customers is not null)
        {
            _createHelper = new CreateCustomerLinkRequest(
                customers,
                requests,
                unitOfWork,
                clock,
                users,
                organizations,
                personalSettings,
                personalNotifications);
        }
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

            if (request.TargetUserIdentityId is not null && _createHelper is not null)
            {
                await _createHelper
                    .TryCreatePersonalPendingNotificationAsync(
                        request,
                        request.TargetUserIdentityId,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

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
    private readonly IOrganizationInAppNotificationRepository? _orgNotifications;
    private readonly IPlatformUserRepository? _users;
    private readonly IPersonalInAppNotificationRepository? _personalNotifications;

    public DeclineCustomerLinkRequest(
        ICustomerLinkRequestRepository requests,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationInAppNotificationRepository? orgNotifications = null,
        IPlatformUserRepository? users = null,
        IPersonalInAppNotificationRepository? personalNotifications = null)
    {
        _requests = requests;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _orgNotifications = orgNotifications;
        _users = users;
        _personalNotifications = personalNotifications;
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

        return await DeclineCoreAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApplicationResult<CustomerLinkRequestDto>> ExecuteByIdAsync(
        CustomerLinkRequestId requestId,
        PlatformUserId decliningUserId,
        AccountClass accountClass,
        CancellationToken cancellationToken = default)
    {
        if (accountClass != AccountClass.Personal)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(
                ApplicationErrorCodes.AccountScopeDenied,
                "Declining a customer link requires a Personal session.");
        }

        var request = await _requests.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (request is null || request.Status != CustomerLinkRequestStatus.Pending)
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        if (!await IsAuthorizedTargetAsync(request, decliningUserId, cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<CustomerLinkRequestDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        return await DeclineCoreAsync(request, cancellationToken, decliningUserId).ConfigureAwait(false);
    }

    private async Task<ApplicationResult<CustomerLinkRequestDto>> DeclineCoreAsync(
        CustomerLinkRequest request,
        CancellationToken cancellationToken,
        PlatformUserId? decliningUserId = null)
    {
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
            var personalName = "A customer";
            if (_users is not null && request.TargetUserIdentityId is not null)
            {
                var target = await _users.GetByIdAsync(request.TargetUserIdentityId, cancellationToken)
                    .ConfigureAwait(false);
                if (target is not null && !string.IsNullOrWhiteSpace(target.DisplayName))
                {
                    personalName = target.DisplayName;
                }
            }

            var personalRecipient = decliningUserId ?? request.TargetUserIdentityId;
            if (personalRecipient is not null)
            {
                await TryMarkPersonalPendingRequestNotificationReadAsync(
                        request,
                        personalRecipient,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await TryNotifyInviterAsync(
                    request,
                    CustomerLinkNotificationTypes.OrganizationDeclined,
                    title: "Customer link declined",
                    preview: $"{personalName} declined the customer link request.",
                    cancellationToken)
                .ConfigureAwait(false);
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

    private async Task TryMarkPersonalPendingRequestNotificationReadAsync(
        CustomerLinkRequest request,
        PlatformUserId recipientUserId,
        CancellationToken cancellationToken)
    {
        if (_personalNotifications is null)
        {
            return;
        }

        var relatedId = request.Id.Value.ToString("D");
        var notification = await _personalNotifications
            .FindByRecipientRelatedAsync(
                recipientUserId,
                CustomerLinkNotificationTypes.PersonalPendingRequest,
                relatedId,
                cancellationToken)
            .ConfigureAwait(false);
        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.MarkRead(_clock.UtcNow);
        await _personalNotifications.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    private async Task TryNotifyInviterAsync(
        CustomerLinkRequest request,
        string relatedType,
        string title,
        string preview,
        CancellationToken cancellationToken)
    {
        if (_orgNotifications is null || request.InvitedByUserId is null)
        {
            return;
        }

        var relatedId = request.Id.Value.ToString("D");
        var existing = await _orgNotifications
            .FindByRecipientRelatedAsync(request.InvitedByUserId, relatedType, relatedId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var notification = OrganizationInAppNotification.Create(
            request.OrganizationId,
            request.InvitedByUserId,
            title,
            preview,
            relatedType,
            _clock.UtcNow,
            relatedId);
        await _orgNotifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    private async Task<bool> IsAuthorizedTargetAsync(
        CustomerLinkRequest request,
        PlatformUserId userId,
        CancellationToken cancellationToken)
    {
        if (request.IsTargetedTo(userId))
        {
            return true;
        }

        // Targeted invite for someone else — fail closed.
        if (request.TargetUserIdentityId is not null)
        {
            return false;
        }

        // Legacy email-only: require email match.
        if (_users is null)
        {
            return false;
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return user is not null
               && string.Equals(user.NormalizedEmail, request.NormalizedEmail, StringComparison.Ordinal);
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
    private readonly IOrganizationInAppNotificationRepository? _orgNotifications;
    private readonly IPersonalInAppNotificationRepository? _personalNotifications;

    public AcceptCustomerLinkRequest(
        ICustomerLinkRequestRepository requests,
        IBusinessCustomerRepository customers,
        ILinkedCustomerAppUserRepository links,
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOrganizationInAppNotificationRepository? orgNotifications = null,
        IPersonalInAppNotificationRepository? personalNotifications = null)
    {
        _requests = requests;
        _customers = customers;
        _links = links;
        _memberships = memberships;
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _orgNotifications = orgNotifications;
        _personalNotifications = personalNotifications;
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

        var identityGuard = await EnsurePersonalAcceptorAsync(acceptingUserId, accountClass, cancellationToken)
            .ConfigureAwait(false);
        if (!identityGuard.IsSuccess)
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                identityGuard.ErrorCode!,
                identityGuard.ErrorMessage!);
        }

        var user = identityGuard.Value!;
        var hash = CustomerLinkRequest.HashToken(acceptToken);
        var request = await _requests.FindPendingByTokenHashAsync(hash, cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        return await AcceptCoreAsync(request, user, acceptingUserId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ApplicationResult<AcceptCustomerLinkResultDto>> ExecuteByIdAsync(
        CustomerLinkRequestId requestId,
        PlatformUserId acceptingUserId,
        AccountClass accountClass,
        CancellationToken cancellationToken = default)
    {
        var identityGuard = await EnsurePersonalAcceptorAsync(acceptingUserId, accountClass, cancellationToken)
            .ConfigureAwait(false);
        if (!identityGuard.IsSuccess)
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                identityGuard.ErrorCode!,
                identityGuard.ErrorMessage!);
        }

        var user = identityGuard.Value!;
        var request = await _requests.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (request is null || request.Status != CustomerLinkRequestStatus.Pending)
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        if (request.TargetUserIdentityId is not null)
        {
            if (request.TargetUserIdentityId != acceptingUserId)
            {
                return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                    ApplicationErrorCodes.CustomerLinkRequestNotFound,
                    "Customer link request was not found.");
            }
        }
        else if (!string.Equals(user.NormalizedEmail, request.NormalizedEmail, StringComparison.Ordinal))
        {
            return ApplicationResult<AcceptCustomerLinkResultDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        return await AcceptCoreAsync(request, user, acceptingUserId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApplicationResult<PlatformUser>> EnsurePersonalAcceptorAsync(
        PlatformUserId acceptingUserId,
        AccountClass accountClass,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(acceptingUserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<PlatformUser>.Failure(
                DomainErrorCodes.UserNotActive,
                "Accepting a customer link requires an active user.");
        }

        if (accountClass != AccountClass.Personal)
        {
            return ApplicationResult<PlatformUser>.Failure(
                ApplicationErrorCodes.AccountScopeDenied,
                "Accepting a customer link requires a Personal session.");
        }

        if (user.IsOrganizationScopedStaff
            || user.HomeOrganizationId is not null
            || !string.IsNullOrWhiteSpace(user.StaffNumber))
        {
            return ApplicationResult<PlatformUser>.Failure(
                DomainErrorCodes.CustomerLinkPersonalIdentityRequired,
                "Only a Personal identity can accept a customer link.");
        }

        return ApplicationResult<PlatformUser>.Success(user);
    }

    private async Task<ApplicationResult<AcceptCustomerLinkResultDto>> AcceptCoreAsync(
        CustomerLinkRequest request,
        PlatformUser user,
        PlatformUserId acceptingUserId,
        CancellationToken cancellationToken)
    {
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
            await TryMarkPersonalPendingRequestNotificationReadAsync(request, acceptingUserId, cancellationToken)
                .ConfigureAwait(false);
            await TryNotifyInviterAcceptedAsync(request, acceptingUserId, cancellationToken).ConfigureAwait(false);
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
                or DomainErrorCodes.CustomerLinkRequestTargetMismatch
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

    private async Task TryMarkPersonalPendingRequestNotificationReadAsync(
        CustomerLinkRequest request,
        PlatformUserId recipientUserId,
        CancellationToken cancellationToken)
    {
        if (_personalNotifications is null)
        {
            return;
        }

        var relatedId = request.Id.Value.ToString("D");
        var notification = await _personalNotifications
            .FindByRecipientRelatedAsync(
                recipientUserId,
                CustomerLinkNotificationTypes.PersonalPendingRequest,
                relatedId,
                cancellationToken)
            .ConfigureAwait(false);
        if (notification is null || notification.IsRead)
        {
            return;
        }

        notification.MarkRead(_clock.UtcNow);
        await _personalNotifications.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    private async Task TryNotifyInviterAcceptedAsync(
        CustomerLinkRequest request,
        PlatformUserId acceptingUserId,
        CancellationToken cancellationToken)
    {
        if (_orgNotifications is null || request.InvitedByUserId is null)
        {
            return;
        }

        var relatedId = request.Id.Value.ToString("D");
        var relatedType = CustomerLinkNotificationTypes.OrganizationAccepted;
        var existing = await _orgNotifications
            .FindByRecipientRelatedAsync(request.InvitedByUserId, relatedType, relatedId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var personalName = "A customer";
        var acceptor = await _users.GetByIdAsync(acceptingUserId, cancellationToken).ConfigureAwait(false);
        if (acceptor is not null && !string.IsNullOrWhiteSpace(acceptor.DisplayName))
        {
            personalName = acceptor.DisplayName;
        }

        var notification = OrganizationInAppNotification.Create(
            request.OrganizationId,
            request.InvitedByUserId,
            title: "Customer link accepted",
            preview: $"{personalName} accepted the customer link request.",
            relatedType,
            _clock.UtcNow,
            relatedId);
        await _orgNotifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>Personal-session accept by request id (in-app consent).</summary>
public sealed class AcceptCustomerLinkRequestById
{
    private readonly AcceptCustomerLinkRequest _inner;

    public AcceptCustomerLinkRequestById(AcceptCustomerLinkRequest inner) => _inner = inner;

    public Task<ApplicationResult<AcceptCustomerLinkResultDto>> ExecuteAsync(
        CustomerLinkRequestId requestId,
        PlatformUserId acceptingUserId,
        AccountClass accountClass,
        CancellationToken cancellationToken = default) =>
        _inner.ExecuteByIdAsync(requestId, acceptingUserId, accountClass, cancellationToken);
}

/// <summary>Personal-session decline by request id (in-app consent).</summary>
public sealed class DeclineCustomerLinkRequestById
{
    private readonly DeclineCustomerLinkRequest _inner;

    public DeclineCustomerLinkRequestById(DeclineCustomerLinkRequest inner) => _inner = inner;

    public Task<ApplicationResult<CustomerLinkRequestDto>> ExecuteAsync(
        CustomerLinkRequestId requestId,
        PlatformUserId decliningUserId,
        AccountClass accountClass,
        CancellationToken cancellationToken = default) =>
        _inner.ExecuteByIdAsync(requestId, decliningUserId, accountClass, cancellationToken);
}

public sealed class ListPendingCustomerLinkRequestsForPersonalUser
{
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IClock _clock;

    public ListPendingCustomerLinkRequestsForPersonalUser(
        ICustomerLinkRequestRepository requests,
        IPlatformUserRepository users,
        IPlatformOrganizationRepository organizations,
        IClock clock)
    {
        _requests = requests;
        _users = users;
        _organizations = organizations;
        _clock = clock;
    }

    public async Task<ApplicationResult<IReadOnlyList<PersonalPendingCustomerLinkRequestDto>>> ExecuteAsync(
        PlatformUserId userId,
        AccountClass accountClass,
        CancellationToken cancellationToken = default)
    {
        if (accountClass != AccountClass.Personal)
        {
            return ApplicationResult<IReadOnlyList<PersonalPendingCustomerLinkRequestDto>>.Failure(
                ApplicationErrorCodes.AccountScopeDenied,
                "Listing customer link requests requires a Personal session.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<IReadOnlyList<PersonalPendingCustomerLinkRequestDto>>.Failure(
                DomainErrorCodes.UserNotActive,
                "Listing customer link requests requires an active user.");
        }

        if (user.IsOrganizationScopedStaff
            || user.HomeOrganizationId is not null
            || !string.IsNullOrWhiteSpace(user.StaffNumber))
        {
            return ApplicationResult<IReadOnlyList<PersonalPendingCustomerLinkRequestDto>>.Success(
                Array.Empty<PersonalPendingCustomerLinkRequestDto>());
        }

        var pending = await _requests
            .ListPendingForTargetUserAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        var now = _clock.UtcNow;
        var list = new List<PersonalPendingCustomerLinkRequestDto>();
        foreach (var request in pending.Where(r => !r.IsExpired(now)))
        {
            var org = await _organizations.GetByIdAsync(request.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            list.Add(new PersonalPendingCustomerLinkRequestDto(
                request.Id.Value,
                request.OrganizationId.Value,
                org?.DisplayName ?? "Merchant",
                request.BusinessCustomerId.Value,
                nameof(CustomerLinkRequestStatus.Pending),
                request.CreatedAtUtc,
                request.ExpiresAtUtc,
                request.TargetPublicUserId));
        }

        return ApplicationResult<IReadOnlyList<PersonalPendingCustomerLinkRequestDto>>.Success(list);
    }
}

public sealed class CreateBusinessCustomerWithPersonalLink
{
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IBusinessCustomerRepository _customers;
    private readonly CreateCustomerLinkRequest _createLink;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreateBusinessCustomerWithPersonalLink(
        IPlatformOrganizationRepository organizations,
        IBusinessCustomerRepository customers,
        CreateCustomerLinkRequest createLink,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _organizations = organizations;
        _customers = customers;
        _createLink = createLink;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CreateBusinessCustomerWithPersonalLinkResultDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CreateBusinessCustomerRequest customerRequest,
        PlatformUserId? invitedByUserId,
        PlatformUserId? targetUserIdentityId,
        string? publicUserId,
        CancellationToken cancellationToken = default)
    {
        if (targetUserIdentityId is null && string.IsNullOrWhiteSpace(publicUserId))
        {
            return ApplicationResult<CreateBusinessCustomerWithPersonalLinkResultDto>.Failure(
                DomainErrorCodes.PublicUserIdRequired,
                "A target ExItS user identity or public ExItS ID is required.");
        }

        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<CreateBusinessCustomerWithPersonalLinkResultDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<CreateBusinessCustomerWithPersonalLinkResultDto>.Failure(
                DomainErrorCodes.OrganizationNotActive,
                "Business customers can only be created for an active organization.");
        }

        try
        {
            var customer = BusinessCustomer.Create(
                organizationId,
                customerRequest.DisplayName,
                _clock.UtcNow,
                customerRequest.Email,
                customerRequest.Phone,
                customerRequest.Notes,
                customerRequest.OwningProductCode);
            CustomerStaffSeparationGuard.EnsureNotTreatedAsStaff(customer);
            await _customers.AddAsync(customer, cancellationToken).ConfigureAwait(false);

            var linkResult = await _createLink
                .ExecuteAsync(
                    organizationId,
                    customer.Id,
                    email: null,
                    invitedByUserId,
                    targetUserIdentityId,
                    publicUserId,
                    cancellationToken,
                    persist: false,
                    knownCustomer: customer)
                .ConfigureAwait(false);
            if (!linkResult.IsSuccess)
            {
                return ApplicationResult<CreateBusinessCustomerWithPersonalLinkResultDto>.Failure(
                    linkResult.ErrorCode!,
                    linkResult.ErrorMessage!);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ApplicationResult<CreateBusinessCustomerWithPersonalLinkResultDto>.Success(
                new CreateBusinessCustomerWithPersonalLinkResultDto(
                    BusinessCustomerQueryService.Map(customer, isCreditCustomer: false),
                    linkResult.Value!));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CreateBusinessCustomerWithPersonalLinkResultDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListOrganizationInAppNotifications
{
    private readonly IOrganizationInAppNotificationRepository _notifications;

    public ListOrganizationInAppNotifications(IOrganizationInAppNotificationRepository notifications) =>
        _notifications = notifications;

    public async Task<IReadOnlyList<OrganizationInAppNotificationDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId recipientUserIdentityId,
        CancellationToken cancellationToken = default)
    {
        var items = await _notifications
            .ListForRecipientInOrganizationAsync(organizationId, recipientUserIdentityId, take: 50, cancellationToken)
            .ConfigureAwait(false);
        return items.Select(ToDto).ToList();
    }

    internal static OrganizationInAppNotificationDto ToDto(OrganizationInAppNotification notification) =>
        new(
            notification.Id.Value,
            notification.OrganizationId.Value,
            notification.RecipientUserIdentityId.Value,
            notification.Title,
            notification.Preview,
            notification.RelatedType,
            notification.RelatedId,
            notification.IsRead,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);
}

public sealed class MarkOrganizationInAppNotificationRead
{
    private readonly IOrganizationInAppNotificationRepository _notifications;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public MarkOrganizationInAppNotificationRead(
        IOrganizationInAppNotificationRepository notifications,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationInAppNotificationDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId recipientUserIdentityId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _notifications
            .GetByIdAsync(OrganizationInAppNotificationId.From(notificationId), cancellationToken)
            .ConfigureAwait(false);
        if (notification is null
            || notification.OrganizationId != organizationId
            || notification.RecipientUserIdentityId != recipientUserIdentityId)
        {
            return ApplicationResult<OrganizationInAppNotificationDto>.Failure(
                ApplicationErrorCodes.OrganizationNotificationNotFound,
                "Organization notification was not found.");
        }

        notification.MarkRead(_clock.UtcNow);
        await _notifications.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationInAppNotificationDto>.Success(
            ListOrganizationInAppNotifications.ToDto(notification));
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
