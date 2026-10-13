using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

/// <summary>Organization → Personal customer-link eligibility outcomes (org-scoped; not a public directory).</summary>
public static class CustomerLinkEligibilityStatuses
{
    public const string Eligible = "Eligible";
    public const string OwnerOfOrganization = "OwnerOfOrganization";
    public const string OrganizationStaff = "OrganizationStaff";
    public const string AlreadyLinked = "AlreadyLinked";
    public const string PendingInvitation = "PendingInvitation";
    public const string BlockedOrUnavailable = "BlockedOrUnavailable";
    public const string InvalidTarget = "InvalidTarget";
}

public sealed record CustomerLinkEligibilityDto(
    string Status,
    string Message,
    string? PublicUserId = null,
    string? DisplayName = null,
    Guid? UserIdentityId = null,
    Guid? ExistingBusinessCustomerId = null,
    Guid? ExistingPendingRequestId = null,
    string? RelationshipContext = null);

/// <summary>
/// Single authoritative eligibility evaluator for Organization → ExItS Personal customer linking.
/// Used by preflight API and <see cref="CreateCustomerLinkRequest"/>.
/// </summary>
public sealed class EvaluateCustomerLinkEligibility
{
    private readonly IPlatformUserRepository _users;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly ILinkedCustomerAppUserRepository _links;
    private readonly IPersonalOrganizationConnectionBlockRepository? _blocks;
    private readonly IProductLocalRoleGrantRepository? _productRoles;
    private readonly IClock _clock;

    public EvaluateCustomerLinkEligibility(
        IPlatformUserRepository users,
        IOrganizationMembershipRepository memberships,
        ICustomerLinkRequestRepository requests,
        ILinkedCustomerAppUserRepository links,
        IClock clock,
        IPersonalOrganizationConnectionBlockRepository? blocks = null,
        IProductLocalRoleGrantRepository? productRoles = null)
    {
        _users = users;
        _memberships = memberships;
        _requests = requests;
        _links = links;
        _clock = clock;
        _blocks = blocks;
        _productRoles = productRoles;
    }

    public async Task<ApplicationResult<CustomerLinkEligibilityDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        string publicUserIdOrQrPayload,
        BusinessCustomerId? currentBusinessCustomerId = null,
        PlatformUserId? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolvePersonalTargetAsync(publicUserIdOrQrPayload, cancellationToken)
            .ConfigureAwait(false);
        if (!resolved.IsSuccess)
        {
            return ApplicationResult<CustomerLinkEligibilityDto>.Success(
                new CustomerLinkEligibilityDto(
                    CustomerLinkEligibilityStatuses.InvalidTarget,
                    resolved.ErrorMessage ?? "This ExItS account isn't available for linking."));
        }

        var target = resolved.Value!;
        return await EvaluateResolvedAsync(
                organizationId,
                target,
                currentBusinessCustomerId,
                actorUserId,
                cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Customer links attach to the Personal ExItS user. An organization staff login is resolved
    /// to its linked Personal user when that correlation exists.
    /// </summary>
    public async Task<ApplicationResult<PlatformUser>> ResolveCanonicalPersonalAsync(
        PlatformUser target,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (IsPlatformStaffIdentity(target))
        {
            return ApplicationResult<PlatformUser>.Failure(
                DomainErrorCodes.CustomerLinkPersonalIdentityRequired,
                "Customer link targets must be Personal identities, not platform staff.");
        }

        if (!IsOrganizationStaffLogin(target))
        {
            return ApplicationResult<PlatformUser>.Success(target);
        }

        if (target.LinkedPersonalUserId is null)
        {
            return ApplicationResult<PlatformUser>.Failure(
                DomainErrorCodes.CustomerLinkPersonalIdentityRequired,
                "This staff login has no linked Personal ExItS account. Use their Personal ExItS ID.");
        }

        var personal = await _users
            .GetByIdAsync(target.LinkedPersonalUserId, cancellationToken)
            .ConfigureAwait(false);
        if (personal is null
            || personal.Status != AccountStatus.Active
            || IsOrganizationStaffLogin(personal)
            || IsPlatformStaffIdentity(personal))
        {
            return ApplicationResult<PlatformUser>.Failure(
                DomainErrorCodes.CustomerLinkPersonalIdentityRequired,
                "This staff login has no linked Personal ExItS account. Use their Personal ExItS ID.");
        }

        return ApplicationResult<PlatformUser>.Success(personal);
    }

    public async Task<ApplicationResult<CustomerLinkEligibilityDto>> EvaluateResolvedAsync(
        PlatformOrganizationId organizationId,
        PlatformUser target,
        BusinessCustomerId? currentBusinessCustomerId = null,
        PlatformUserId? actorUserId = null,
        CancellationToken cancellationToken = default)
    {
        _ = actorUserId;
        var canonical = await ResolveCanonicalPersonalAsync(target, cancellationToken).ConfigureAwait(false);
        if (!canonical.IsSuccess)
        {
            return ApplicationResult<CustomerLinkEligibilityDto>.Success(
                new CustomerLinkEligibilityDto(
                    CustomerLinkEligibilityStatuses.InvalidTarget,
                    canonical.ErrorMessage ?? "This ExItS account isn't available for linking."));
        }

        target = canonical.Value!;

        if (_blocks is not null
            && await CustomerConnectionBlockSupport
                .IsBlockedAsync(_blocks, target.Id, organizationId, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<CustomerLinkEligibilityDto>.Success(
                new CustomerLinkEligibilityDto(
                    CustomerLinkEligibilityStatuses.BlockedOrUnavailable,
                    "This ExItS account isn't available for linking.",
                    target.PublicUserId,
                    target.DisplayName,
                    target.Id.Value));
        }

        var relationship = await DescribeRelationshipAsync(target, organizationId, cancellationToken)
            .ConfigureAwait(false);

        var activeLink = await _links
            .FindActiveByUserAndOrganizationAsync(target.Id, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (activeLink is not null)
        {
            return ApplicationResult<CustomerLinkEligibilityDto>.Success(
                new CustomerLinkEligibilityDto(
                    CustomerLinkEligibilityStatuses.AlreadyLinked,
                    "This ExItS account is already linked to a customer in this business.",
                    target.PublicUserId,
                    target.DisplayName,
                    target.Id.Value,
                    ExistingBusinessCustomerId: activeLink.BusinessCustomerId.Value));
        }

        var pending = await _requests
            .FindPendingByOrganizationAndTargetUserAsync(organizationId, target.Id, cancellationToken)
            .ConfigureAwait(false);
        if (pending is not null)
        {
            if (pending.IsExpired(_clock.UtcNow))
            {
                try
                {
                    pending.MarkExpired(_clock.UtcNow);
                    await _requests.UpdateAsync(pending, cancellationToken).ConfigureAwait(false);
                }
                catch (DomainException)
                {
                    // Already transitioned.
                }
            }
            else if (currentBusinessCustomerId is null
                     || pending.BusinessCustomerId != currentBusinessCustomerId)
            {
                return ApplicationResult<CustomerLinkEligibilityDto>.Success(
                    new CustomerLinkEligibilityDto(
                        CustomerLinkEligibilityStatuses.PendingInvitation,
                        "An invitation has already been sent to this person.",
                        target.PublicUserId,
                        target.DisplayName,
                        target.Id.Value,
                        ExistingBusinessCustomerId: pending.BusinessCustomerId.Value,
                        ExistingPendingRequestId: pending.Id.Value));
            }
        }

        return ApplicationResult<CustomerLinkEligibilityDto>.Success(
            new CustomerLinkEligibilityDto(
                CustomerLinkEligibilityStatuses.Eligible,
                "Eligible to invite.",
                target.PublicUserId,
                target.DisplayName,
                target.Id.Value,
                RelationshipContext: relationship));
    }

    private async Task<string?> DescribeRelationshipAsync(
        PlatformUser personal,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var personalMembership = await _memberships
            .FindActiveByUserAndOrganizationAsync(personal.Id, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (personalMembership?.Role == OrganizationRole.OrganizationOwner)
        {
            return OrganizationRoleDisplay.Owner;
        }

        var linkedStaff = await _users
            .FindActiveStaffByHomeOrgAndLinkedPersonalUserIdAsync(organizationId, personal.Id, cancellationToken)
            .ConfigureAwait(false);
        var authorityUserId = linkedStaff?.Id ?? personalMembership?.UserId;
        var authorityRole = personalMembership?.Role;
        if (linkedStaff is not null)
        {
            var staffMembership = await _memberships
                .FindActiveByUserAndOrganizationAsync(linkedStaff.Id, organizationId, cancellationToken)
                .ConfigureAwait(false);
            authorityRole = staffMembership?.Role ?? authorityRole;
            authorityUserId = linkedStaff.Id;
        }

        if (authorityRole is null && linkedStaff is null)
        {
            return null;
        }

        if (authorityRole == OrganizationRole.OrganizationAdministrator)
        {
            return OrganizationRoleDisplay.Administrator;
        }

        if (_productRoles is not null && authorityUserId is not null)
        {
            var grant = await _productRoles
                .FindActiveByUserOrganizationProductAsync(
                    organizationId,
                    authorityUserId,
                    ProductCode.PinoyBusinessPos,
                    cancellationToken)
                .ConfigureAwait(false);
            if (grant is not null && grant.Status == ProductLocalRoleGrantStatus.Active)
            {
                var label = ProductRoleDisplay.ToDisplayLabel(grant.RoleCode);
                if (!string.IsNullOrWhiteSpace(label)
                    && !string.Equals(label, ProductRoleDisplay.PosOwner, StringComparison.Ordinal))
                {
                    return label;
                }
            }
        }

        return OrganizationRoleDisplay.Staff;
    }

    private static bool IsOrganizationStaffLogin(PlatformUser user) =>
        user.IsOrganizationScopedStaff || user.HomeOrganizationId is not null;

    private static bool IsPlatformStaffIdentity(PlatformUser user) =>
        !IsOrganizationStaffLogin(user) && !string.IsNullOrWhiteSpace(user.StaffNumber);

    /// <summary>Maps eligibility status to create-path failure when not Eligible.</summary>
    public static ApplicationResult<T> ToCreateFailure<T>(CustomerLinkEligibilityDto eligibility)
    {
        return eligibility.Status switch
        {
            CustomerLinkEligibilityStatuses.OwnerOfOrganization =>
                ApplicationResult<T>.Failure(
                    ApplicationErrorCodes.CustomerLinkOwnerSelf,
                    eligibility.Message),
            CustomerLinkEligibilityStatuses.OrganizationStaff =>
                ApplicationResult<T>.Failure(
                    ApplicationErrorCodes.CustomerLinkOrganizationStaff,
                    eligibility.Message),
            CustomerLinkEligibilityStatuses.AlreadyLinked =>
                ApplicationResult<T>.Failure(
                    ApplicationErrorCodes.CustomerLinkRequestConflict,
                    eligibility.Message),
            CustomerLinkEligibilityStatuses.PendingInvitation =>
                ApplicationResult<T>.Failure(
                    ApplicationErrorCodes.CustomerLinkPendingExists,
                    eligibility.Message),
            CustomerLinkEligibilityStatuses.BlockedOrUnavailable =>
                ApplicationResult<T>.Failure(
                    ApplicationErrorCodes.CustomerConnectionUnavailable,
                    CustomerConnectionBlockSupport.OrgUnavailableMessage),
            _ => ApplicationResult<T>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestConflict,
                eligibility.Message)
        };
    }

    private async Task<ApplicationResult<PlatformUser>> ResolvePersonalTargetAsync(
        string publicUserIdOrQrPayload,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(publicUserIdOrQrPayload))
        {
            return ApplicationResult<PlatformUser>.Failure(
                DomainErrorCodes.InvalidExItsQrPayload,
                "Enter an ExItS ID or scan a Personal QR.");
        }

        string publicUserId;
        try
        {
            var trimmed = publicUserIdOrQrPayload.Trim();
            if (trimmed.StartsWith("exits://", StringComparison.OrdinalIgnoreCase)
                && ExItsQrEnvelope.TryParse(trimmed, out var parsed)
                && parsed is not null)
            {
                if (parsed.Purpose != ExItsQrPurpose.Personal)
                {
                    return ApplicationResult<PlatformUser>.Failure(
                        DomainErrorCodes.InvalidExItsQrPurpose,
                        "Scan their Personal QR, not a Business QR.");
                }

                publicUserId = PublicUserIdRules.Normalize(parsed.Subject);
            }
            else
            {
                publicUserId = PublicUserIdRules.Normalize(trimmed);
            }
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformUser>.Failure(ex.ErrorCode, ex.Message);
        }

        var user = await _users.GetByPublicUserIdAsync(publicUserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<PlatformUser>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "We couldn't find this ExItS account.");
        }

        return ApplicationResult<PlatformUser>.Success(user);
    }
}
