using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed class SuspendOrganizationMembership
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IClock _clock;

    public SuspendOrganizationMembership(IOrganizationMembershipRepository memberships, IClock clock)
    {
        _memberships = memberships;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationMembership>> ExecuteAsync(
        OrganizationMembershipId membershipId,
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
            membership.Suspend(_clock.UtcNow);
            await _memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationMembership>.Success(membership);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationMembership>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
