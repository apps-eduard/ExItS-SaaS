namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>
/// Resolves which branches an identity may select for a given organization.
/// Owner path: all Active branches in the organization. Future staff ACL filters here.
/// </summary>
public interface IAccessibleBranchResolver
{
    Task<IReadOnlyList<AccessibleWorkspaceBranch>> ListAccessibleBranchesAsync(
        Guid organizationId,
        EligibleOrganization organization,
        CancellationToken ct = default);
}

/// <summary>
/// Lists eligible organizations and accessible branches; resolves login routing.
/// </summary>
public interface IWorkspaceSelectionService
{
    Task<IReadOnlyList<AccessibleOrganizationWorkspace>> ListWorkspacesAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<WorkspaceRoutingPlan> ResolveRoutingPlanAsync(
        Guid userId,
        CancellationToken ct = default);
}
