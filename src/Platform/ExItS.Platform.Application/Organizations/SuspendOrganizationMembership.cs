using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed class SuspendOrganizationMembership
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SuspendOrganizationMembership(
        IOrganizationMembershipRepository memberships,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _memberships = memberships;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationMembership>> ExecuteAsync(
        OrganizationMembershipId membershipId,
        string? reason = null,
        string? actorReference = null,
        CancellationToken cancellationToken = default)
    {
        var membership = await _memberships.GetByIdAsync(membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        try
        {
            membership.Suspend(_clock.UtcNow, reason, actorReference);
            await _memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
            await _sessions
                .ClearSelectedOrganizationAsync(membership.UserId, membership.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            await _accessTokens
                .ClearOrganizationBindingAsync(membership.UserId, membership.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationMembership>.Success(membership);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationMembership>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ChangeOrganizationRole
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ChangeOrganizationRole(
        IOrganizationMembershipRepository memberships,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _memberships = memberships;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationMembership>> ExecuteAsync(
        OrganizationMembershipId membershipId,
        OrganizationRole role,
        string? actorReference = null,
        CancellationToken cancellationToken = default)
    {
        var membership = await _memberships.GetByIdAsync(membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        try
        {
            membership.ChangeRole(role, _clock.UtcNow, actorReference);
            await _memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationMembership>.Success(membership);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationMembership>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
