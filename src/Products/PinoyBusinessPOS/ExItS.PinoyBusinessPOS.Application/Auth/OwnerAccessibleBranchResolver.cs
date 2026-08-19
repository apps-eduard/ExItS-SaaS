using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>
/// Resolves accessible branches via Platform List Branches (server filters by role and assignments).
/// </summary>
public sealed class OwnerAccessibleBranchResolver(IPlatformAccessClient platform) : IAccessibleBranchResolver
{
    public async Task<IReadOnlyList<AccessibleWorkspaceBranch>> ListAccessibleBranchesAsync(
        Guid organizationId,
        EligibleOrganization organization,
        CancellationToken ct = default)
    {
        if (!organization.AccessAllowed)
        {
            return [];
        }

        var branches = await platform.GetBranchesAsync(organizationId, ct).ConfigureAwait(false);
        if (!branches.IsSuccess || branches.Data is null)
        {
            return [];
        }

        return branches.Data
            .Where(b => string.Equals(b.Status, "Active", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .Select(MapBranch)
            .ToList();
    }

    internal static AccessibleWorkspaceBranch MapBranch(OrganizationBranchDto branch)
    {
        var secondary = ResolveSecondaryLine(branch);
        return new AccessibleWorkspaceBranch(
            branch.Id,
            branch.Name,
            secondary,
            branch.IsPrimary,
            string.Equals(branch.Status, "Active", StringComparison.OrdinalIgnoreCase));
    }

    public static string ResolveSecondaryLine(OrganizationBranchDto branch)
    {
        if (!string.IsNullOrWhiteSpace(branch.City))
        {
            return branch.City.Trim();
        }

        if (!string.IsNullOrWhiteSpace(branch.Region))
        {
            return branch.Region.Trim();
        }

        if (branch.CustomerOrderingReady)
        {
            return "Active";
        }

        return "Setup required";
    }
}
