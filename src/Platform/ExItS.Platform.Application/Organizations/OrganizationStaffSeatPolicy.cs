using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Staff seats come from the plan entitlement <see cref="FeatureCode.PlanMaxActiveStaff"/>.
/// Active organization owners are excluded. Pending invitations do not consume a seat.
/// A lower plan limit does not remove existing memberships.
/// A missing grant or a null numeric limit means unlimited.
/// </summary>
public sealed record StaffSeatDecision(int ActiveStaffCount, int? StaffLimit, bool IsFull, bool OverLimit);

public sealed class OrganizationStaffSeatPolicy
{
    public const string LimitReachedMessage =
        "Your current plan has reached its staff limit. Upgrade the subscription or remove an existing staff member before adding another.";

    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IEntitlementSnapshotRepository _snapshots;

    public OrganizationStaffSeatPolicy(
        IOrganizationMembershipRepository memberships,
        IEntitlementSnapshotRepository snapshots)
    {
        _memberships = memberships;
        _snapshots = snapshots;
    }

    public async Task<StaffSeatDecision> EvaluateAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var used = await _memberships
            .CountActiveNonOwnerStaffAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var snapshot = await _snapshots
            .GetLatestForOrganizationProductAsync(organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
        int? limit = null;
        if (snapshot is not null)
        {
            var grant = snapshot.Grants.FirstOrDefault(g =>
                g.Enabled && g.FeatureCode.Value == FeatureCode.PlanMaxActiveStaff);
            if (grant is not null)
            {
                limit = grant.NumericLimit;
            }
        }

        var isFull = limit is int cap && used >= cap;
        var overLimit = limit is int max && used > max;
        return new StaffSeatDecision(used, limit, isFull, overLimit);
    }
}
