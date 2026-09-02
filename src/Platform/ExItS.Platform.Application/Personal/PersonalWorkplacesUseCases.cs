using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Personal;

public sealed record PersonalWorkplaceBranchDto(
    Guid BranchId,
    string Name,
    string Code,
    bool IsPrimary);

/// <summary>
/// Workplace (org staff membership) visible to a linked Personal principal.
/// StaffLogin is the organization-scoped login alias owned by Platform (not reconstructed by clients).
/// </summary>
public sealed record PersonalWorkplaceDto(
    Guid OrganizationId,
    string OrganizationDisplayName,
    string? PublicOrganizationId,
    Guid StaffUserId,
    string StaffLogin,
    Guid MembershipId,
    string MembershipRole,
    string MembershipRoleDisplay,
    string MembershipStatus,
    string? ProductRole,
    string? ProductRoleDisplay,
    IReadOnlyList<PersonalWorkplaceBranchDto> Branches);

/// <summary>
/// Lists organization workplaces for the authenticated Personal user via LinkedPersonalUserId.
/// Does not invent authentication — Open workplace remains staff login with StaffLogin.
/// </summary>
public sealed class ListPersonalWorkplaces
{
    private readonly IPlatformUserRepository _users;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IProductLocalRoleGrantRepository _roleGrants;
    private readonly IOrganizationMembershipBranchAssignmentRepository _assignments;
    private readonly IOrganizationBranchRepository _branches;

    public ListPersonalWorkplaces(
        IPlatformUserRepository users,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        IProductLocalRoleGrantRepository roleGrants,
        IOrganizationMembershipBranchAssignmentRepository assignments,
        IOrganizationBranchRepository branches)
    {
        _users = users;
        _memberships = memberships;
        _organizations = organizations;
        _roleGrants = roleGrants;
        _assignments = assignments;
        _branches = branches;
    }

    public async Task<ApplicationResult<IReadOnlyList<PersonalWorkplaceDto>>> ExecuteAsync(
        PlatformUserId personalUserId,
        CancellationToken cancellationToken = default)
    {
        var staffUsers = await _users
            .ListStaffLinkedToPersonalUserAsync(personalUserId, cancellationToken)
            .ConfigureAwait(false);

        var items = new List<PersonalWorkplaceDto>();
        foreach (var staffUser in staffUsers)
        {
            if (staffUser.Status != AccountStatus.Active
                || staffUser.HomeOrganizationId is null
                || staffUser.LinkedPersonalUserId is null
                || !staffUser.LinkedPersonalUserId.Equals(personalUserId))
            {
                continue;
            }

            var membershipPage = await _memberships
                .ListByUserAsync(staffUser.Id, status: null, skip: 0, take: 20, cancellationToken)
                .ConfigureAwait(false);
            var membership = membershipPage.Items
                .Where(m => m.OrganizationId.Equals(staffUser.HomeOrganizationId))
                .OrderByDescending(m => m.Status == MembershipStatus.Active)
                .ThenByDescending(m => m.UpdatedAtUtc)
                .FirstOrDefault();

            if (membership is null || membership.Status == MembershipStatus.Removed)
            {
                continue;
            }

            var organization = await _organizations
                .GetByIdAsync(membership.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            if (organization is null)
            {
                continue;
            }

            var grants = await _roleGrants
                .ListActiveByUserOrganizationAsync(
                    membership.OrganizationId,
                    staffUser.Id,
                    cancellationToken)
                .ConfigureAwait(false);
            var primaryGrant = grants
                .OrderBy(g => g.RoleCode, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            var productRole = primaryGrant?.RoleCode;
            var productRoleDisplay = string.IsNullOrWhiteSpace(productRole)
                ? null
                : ProductRoleDisplay.ToDisplayLabel(productRole);

            var branches = await ResolveBranchesAsync(membership, cancellationToken).ConfigureAwait(false);

            items.Add(
                new PersonalWorkplaceDto(
                    organization.Id.Value,
                    organization.DisplayName,
                    organization.PublicOrganizationId,
                    staffUser.Id.Value,
                    StaffLoginNameRules.FormatForDisplay(staffUser.NormalizedEmail),
                    membership.Id.Value,
                    membership.Role.ToString(),
                    OrganizationRoleDisplay.ToDisplayLabel(membership.Role),
                    membership.Status.ToString(),
                    productRole,
                    productRoleDisplay,
                    branches));
        }

        items.Sort((a, b) =>
        {
            var byName = string.Compare(
                a.OrganizationDisplayName,
                b.OrganizationDisplayName,
                StringComparison.OrdinalIgnoreCase);
            return byName != 0
                ? byName
                : string.Compare(a.StaffLogin, b.StaffLogin, StringComparison.OrdinalIgnoreCase);
        });

        return ApplicationResult<IReadOnlyList<PersonalWorkplaceDto>>.Success(items);
    }

    private async Task<IReadOnlyList<PersonalWorkplaceBranchDto>> ResolveBranchesAsync(
        OrganizationMembership membership,
        CancellationToken cancellationToken)
    {
        var orgBranches = await _branches
            .ListByOrganizationAsync(membership.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        var active = orgBranches
            .Where(b => b.Status == OrganizationBranchStatus.Active)
            .ToDictionary(b => b.Id.Value);

        IReadOnlyCollection<Guid> selected;
        if (OrganizationBranchAccessService.HasOrganizationWideBranchAccess(membership.Role))
        {
            selected = active.Keys.ToList();
        }
        else
        {
            var rows = await _assignments
                .ListByMembershipAsync(membership.Id, cancellationToken)
                .ConfigureAwait(false);
            selected = rows.Select(r => r.BranchId.Value).ToList();
        }

        return selected
            .Where(active.ContainsKey)
            .Select(id =>
            {
                var branch = active[id];
                return new PersonalWorkplaceBranchDto(
                    id,
                    branch.Name,
                    branch.Code,
                    branch.IsPrimary);
            })
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
