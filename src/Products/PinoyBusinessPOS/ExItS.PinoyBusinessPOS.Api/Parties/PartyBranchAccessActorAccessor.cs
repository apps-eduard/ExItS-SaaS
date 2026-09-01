using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Parties;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Api.Parties;

internal sealed class PartyBranchAccessActorAccessor : IPartyBranchAccessActorAccessor
{
    private readonly IHttpContextAccessor _http;

    public PartyBranchAccessActorAccessor(IHttpContextAccessor http) => _http = http;

    public PartyBranchAccessActor GetActor()
    {
        Guid? actingBranch = null;
        var request = _http.HttpContext?.Request;
        if (request is not null
            && PosOrganizationScope.TryGetOptionalBranchId(request, out var branchId)
            && branchId is not null)
        {
            actingBranch = branchId;
        }

        if (!PosRoleRequestContext.HasActorHeader || PosRoleRequestContext.BypassRoleEnforcement)
        {
            return new PartyBranchAccessActor(PosRole.Owner, false, actingBranch);
        }

        return new PartyBranchAccessActor(
            PosRoleRequestContext.CurrentRole,
            PosRoleRequestContext.OrganizationManagementAuthority,
            actingBranch);
    }
}
