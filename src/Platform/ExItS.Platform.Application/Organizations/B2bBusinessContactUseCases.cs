using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Privacy-safe B2B business contact projection for a Connected seller↔buyer relationship.
/// Eligible: Active membership, Active user (Owner/Administrator/Staff).
/// Phone/email/department/job title come from OrganizationMember business profile only —
/// never PlatformUser personal phone/email.
/// </summary>
public sealed record B2bBusinessContactDto(
    Guid OrganizationMemberId,
    Guid UserId,
    string DisplayName,
    string RoleTitle,
    bool IsOwner,
    string? Department,
    string? Phone,
    string? Email,
    string? EmployeeCode);

/// <summary>
/// Lists Active organization Owner/staff for B2B relationship contact selection.
/// Caller (POS) must verify Connected status before invoking — this use case does not see POS relationships.
/// </summary>
public sealed class ListOrganizationB2bBusinessContacts
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUserRepository _users;
    private readonly IProductLocalRoleGrantRepository? _roleGrants;

    public ListOrganizationB2bBusinessContacts(
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users,
        IProductLocalRoleGrantRepository? roleGrants = null)
    {
        _memberships = memberships;
        _users = users;
        _roleGrants = roleGrants;
    }

    public async Task<ApplicationResult<IReadOnlyList<B2bBusinessContactDto>>> ExecuteAsync(
        Guid organizationId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var org = PlatformOrganizationId.From(organizationId);
        var items = await _memberships
            .ListActiveByOrganizationAsync(org, cancellationToken)
            .ConfigureAwait(false);

        var result = new List<B2bBusinessContactDto>(items.Count);
        foreach (var membership in items)
        {
            var mapped = await TryMapAsync(membership, cancellationToken).ConfigureAwait(false);
            if (mapped is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(search)
                && !MatchesSearch(mapped, search.Trim()))
            {
                continue;
            }

            result.Add(mapped);
        }

        return ApplicationResult<IReadOnlyList<B2bBusinessContactDto>>.Success(
            result
                .OrderByDescending(x => x.IsOwner)
                .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    public async Task<ApplicationResult<B2bBusinessContactDto?>> GetAsync(
        Guid organizationId,
        Guid organizationMemberId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _memberships
            .GetByIdAsync(OrganizationMembershipId.From(organizationMemberId), cancellationToken)
            .ConfigureAwait(false);
        if (membership is null || membership.OrganizationId.Value != organizationId)
        {
            return ApplicationResult<B2bBusinessContactDto?>.Success(null);
        }

        var mapped = await TryMapAsync(membership, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<B2bBusinessContactDto?>.Success(mapped);
    }

    private async Task<B2bBusinessContactDto?> TryMapAsync(
        OrganizationMembership membership,
        CancellationToken cancellationToken)
    {
        // Relationship-contact picker lists all Active org members (Owner/Admin/Staff).
        // IsBusinessContact remains a work-profile preference, not a directory gate —
        // otherwise staff are invisible by default and sellers cannot select them.
        if (membership.Status != MembershipStatus.Active)
        {
            return null;
        }

        var user = await _users.GetByIdAsync(membership.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return null;
        }

        // Staff must belong to this org home; owners are personal identities (HomeOrganizationId null).
        if (user.HomeOrganizationId is PlatformOrganizationId home
            && home.Value != membership.OrganizationId.Value)
        {
            return null;
        }

        var roleTitle = !string.IsNullOrWhiteSpace(membership.JobTitle)
            ? membership.JobTitle!
            : await ResolveRoleTitleAsync(membership, cancellationToken).ConfigureAwait(false);

        return new B2bBusinessContactDto(
            membership.Id.Value,
            membership.UserId.Value,
            user.DisplayName,
            roleTitle,
            IsOwner: membership.Role == OrganizationRole.OrganizationOwner,
            Department: membership.Department,
            Phone: membership.WorkPhone,
            Email: membership.WorkEmail,
            EmployeeCode: user.EmployeeCode);
    }

    private async Task<string> ResolveRoleTitleAsync(
        OrganizationMembership membership,
        CancellationToken cancellationToken)
    {
        if (_roleGrants is not null)
        {
            var grants = await _roleGrants
                .ListActiveByUserOrganizationAsync(
                    membership.OrganizationId,
                    membership.UserId,
                    cancellationToken)
                .ConfigureAwait(false);
            var productLabel = grants
                .Select(g => ProductRoleDisplay.ToDisplayLabel(g.RoleCode))
                .FirstOrDefault(label => !string.IsNullOrWhiteSpace(label));
            if (!string.IsNullOrWhiteSpace(productLabel))
            {
                return productLabel!;
            }
        }

        return OrganizationRoleDisplay.ToDisplayLabel(membership.Role);
    }

    private static bool MatchesSearch(B2bBusinessContactDto contact, string term) =>
        (contact.DisplayName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
        || (contact.RoleTitle?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
        || (contact.Department?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
        || (contact.Email?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
        || (contact.EmployeeCode?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
}
