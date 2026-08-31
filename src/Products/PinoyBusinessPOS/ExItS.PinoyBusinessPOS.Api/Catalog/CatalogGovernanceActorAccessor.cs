using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Api.Catalog;

internal sealed class CatalogGovernanceActorAccessor : ICatalogGovernanceActorAccessor
{
    private readonly IHttpContextAccessor _http;

    public CatalogGovernanceActorAccessor(IHttpContextAccessor http) => _http = http;

    public CatalogGovernanceActor GetActor()
    {
        Guid? actingBranch = null;
        var request = _http.HttpContext?.Request;
        if (request is not null
            && PosOrganizationScope.TryGetOptionalBranchId(request, out var branchId)
            && branchId is not null)
        {
            actingBranch = branchId;
        }

        // Legacy Dev/Testing callers without actor header keep OrganizationStandard create defaults.
        if (!PosRoleRequestContext.HasActorHeader || PosRoleRequestContext.BypassRoleEnforcement)
        {
            return new CatalogGovernanceActor(PosRole.Owner, false, actingBranch);
        }

        return new CatalogGovernanceActor(
            PosRoleRequestContext.CurrentRole,
            PosRoleRequestContext.OrganizationManagementAuthority,
            actingBranch);
    }
}
