using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

public sealed class WorkspaceSelectionService(
    IProductAccessResolver access,
    IAccessibleBranchResolver branchResolver) : IWorkspaceSelectionService
{
    public async Task<IReadOnlyList<AccessibleOrganizationWorkspace>> ListWorkspacesAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var organizations = await access.ListEligibleOrganizationsAsync(userId, ct).ConfigureAwait(false);
        if (organizations.Count == 0)
        {
            return [];
        }

        var workspaces = new List<AccessibleOrganizationWorkspace>(organizations.Count);
        foreach (var org in organizations.Where(o => o.AccessAllowed))
        {
            var branches = await branchResolver
                .ListAccessibleBranchesAsync(org.OrganizationId, org, ct)
                .ConfigureAwait(false);
            if (branches.Count == 0)
            {
                continue;
            }

            workspaces.Add(new AccessibleOrganizationWorkspace(
                org.OrganizationId,
                org.DisplayName,
                branches));
        }

        return workspaces;
    }

    public async Task<WorkspaceRoutingPlan> ResolveRoutingPlanAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var workspaces = await ListWorkspacesAsync(userId, ct).ConfigureAwait(false);
        if (workspaces.Count == 0)
        {
            var organizations = await access.ListEligibleOrganizationsAsync(userId, ct).ConfigureAwait(false);
            if (organizations.Count == 0)
            {
                return new WorkspaceRoutingPlan(WorkspaceRoutingOutcome.PersonalHome);
            }

            return new WorkspaceRoutingPlan(WorkspaceRoutingOutcome.NoAccessibleBranch);
        }

        if (workspaces.Count == 1 && workspaces[0].Branches.Count == 1)
        {
            var only = workspaces[0];
            return new WorkspaceRoutingPlan(
                WorkspaceRoutingOutcome.AutoSelect,
                only.OrganizationId,
                only.Branches[0].BranchId);
        }

        return new WorkspaceRoutingPlan(WorkspaceRoutingOutcome.ShowChooser);
    }
}
