namespace ExItS.Platform.Domain.Governance;

/// <summary>
/// Stable action codes for organization governance password step-up grants.
/// Grants are scoped to user, organization, action code, target type, and target id.
/// </summary>
public static class GovernanceCriticalActionCodes
{
    public const string BranchSuspend = "platform.organization.branch.suspend";
    public const string BranchArchive = "platform.organization.branch.archive";
    public const string BranchReactivate = "platform.organization.branch.reactivate";
    public const string BranchSetPrimary = "platform.organization.branch.set_primary";
    public const string MembershipSuspend = "platform.membership.suspend";
    public const string MembershipRevoke = "platform.membership.revoke";
    public const string MembershipRoleChange = "platform.membership.role_change";
    public const string PosDeviceRevoke = "platform.pos_device.revoke";
}

public static class GovernanceStepUpTargetTypes
{
    public const string OrganizationBranch = nameof(Organizations.OrganizationBranch);
    public const string OrganizationMembership = nameof(Organizations.OrganizationMembership);
    public const string PosDevice = nameof(Organizations.PosDevice);
}
