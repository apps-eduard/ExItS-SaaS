using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Application.Parties;

/// <summary>Request-scoped actor facts for customer/supplier branch access (MB2-04).</summary>
public sealed record PartyBranchAccessActor(
    PosRole? PosRole,
    bool OrganizationManagementAuthority,
    Guid? ActingBranchId)
{
    /// <summary>Owner/Admin POS role or Platform org management authority.</summary>
    public bool IsOrganizationGovernance =>
        PosRole is Domain.Permissions.PosRole.Owner or Domain.Permissions.PosRole.Admin
        || OrganizationManagementAuthority;
}

public interface IPartyBranchAccessActorAccessor
{
    PartyBranchAccessActor GetActor();
}

/// <summary>Central party branch-access governance — do not duplicate across endpoints.</summary>
public sealed class PartyBranchAccessGovernanceAuthority
{
    public bool CanBypassBranchFilter(PartyBranchAccessActor actor) =>
        actor.IsOrganizationGovernance;
}
