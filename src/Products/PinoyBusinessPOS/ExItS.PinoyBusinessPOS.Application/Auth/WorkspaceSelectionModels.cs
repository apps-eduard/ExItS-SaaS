namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>Post-sign-in / cold-start workspace routing decision.</summary>
public enum WorkspaceRoutingOutcome
{
    /// <summary>No organization memberships — Personal home.</summary>
    PersonalHome,

    /// <summary>Exactly one organization with one accessible Active branch — auto-bind.</summary>
    AutoSelect,

    /// <summary>User must choose organization and branch.</summary>
    ShowChooser,

    /// <summary>Organizations exist but none have an accessible Active branch.</summary>
    NoAccessibleBranch
}

public sealed record AccessibleWorkspaceBranch(
    Guid BranchId,
    string Name,
    string SecondaryLine,
    bool IsPrimary,
    bool IsActive);

public sealed record AccessibleOrganizationWorkspace(
    Guid OrganizationId,
    string DisplayName,
    IReadOnlyList<AccessibleWorkspaceBranch> Branches);

public sealed record WorkspaceRoutingPlan(
    WorkspaceRoutingOutcome Outcome,
    Guid? AutoOrganizationId = null,
    Guid? AutoBranchId = null);

public sealed record SelectWorkspaceRequest(Guid OrganizationId, Guid BranchId);
