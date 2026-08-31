using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>Request-scoped actor facts for product governance (MB2-01B).</summary>
public sealed record CatalogGovernanceActor(
    PosRole? PosRole,
    bool OrganizationManagementAuthority,
    Guid? ActingBranchId)
{
    /// <summary>Owner/Admin POS role or Platform org management authority.</summary>
    public bool IsOrganizationGovernance =>
        PosRole is Domain.Permissions.PosRole.Owner or Domain.Permissions.PosRole.Admin
        || OrganizationManagementAuthority;
}

public interface ICatalogGovernanceActorAccessor
{
    CatalogGovernanceActor GetActor();
}

/// <summary>Central product governance authority — do not duplicate across endpoints.</summary>
public sealed class CatalogProductGovernanceAuthority
{
    public bool CanEditOrganizationStandardMaster(CatalogGovernanceActor actor) =>
        actor.IsOrganizationGovernance;

    public bool CanMutateOrganizationStandardPrice(CatalogGovernanceActor actor) =>
        actor.IsOrganizationGovernance;

    public bool CanManageStandardAvailability(CatalogGovernanceActor actor) =>
        actor.IsOrganizationGovernance;

    public bool CanPromote(CatalogGovernanceActor actor) =>
        actor.IsOrganizationGovernance;

    public bool CanCreateOrganizationStandard(CatalogGovernanceActor actor) =>
        actor.IsOrganizationGovernance;

    public bool CanOperateBranchLocal(CatalogGovernanceActor actor, PosBranchId? originBranchId)
    {
        if (actor.IsOrganizationGovernance)
        {
            return true;
        }

        if (actor.ActingBranchId is null || originBranchId is null)
        {
            return false;
        }

        return actor.ActingBranchId.Value == originBranchId.Value;
    }

    public bool CanViewBranchLocalInManagement(CatalogGovernanceActor actor, PosBranchId? originBranchId) =>
        CanOperateBranchLocal(actor, originBranchId);

    /// <summary>
    /// Resolves create scope for backward-compatible clients.
    /// Omitted scope: Owner/Admin → OrganizationStandard; branch catalog actor → BranchLocal at acting branch.
    /// </summary>
    public ApplicationResult<(CatalogProductScope Scope, PosBranchId? Origin)> ResolveCreateScope(
        CatalogGovernanceActor actor,
        string? requestedScopeCode)
    {
        CatalogProductScope? requested = null;
        if (!string.IsNullOrWhiteSpace(requestedScopeCode))
        {
            if (!CatalogProductScopes.TryParse(requestedScopeCode, out var parsed))
            {
                return ApplicationResult<(CatalogProductScope, PosBranchId?)>.Failure(
                    DomainErrorCodes.InvalidCatalogProductScope,
                    $"CatalogProductScope must be one of: {string.Join(", ", CatalogProductScopes.Codes)}.");
            }

            requested = parsed;
        }

        if (requested is CatalogProductScope.OrganizationStandard)
        {
            if (!CanCreateOrganizationStandard(actor))
            {
                return ApplicationResult<(CatalogProductScope, PosBranchId?)>.Failure(
                    ApplicationErrorCodes.ProductScopeForbidden,
                    "Only organization Owner/Administrator may create OrganizationStandard products.");
            }

            return ApplicationResult<(CatalogProductScope, PosBranchId?)>.Success(
                (CatalogProductScope.OrganizationStandard, null));
        }

        if (requested is CatalogProductScope.BranchLocal || (requested is null && !actor.IsOrganizationGovernance))
        {
            if (actor.ActingBranchId is null)
            {
                return ApplicationResult<(CatalogProductScope, PosBranchId?)>.Failure(
                    ApplicationErrorCodes.ProductActingBranchRequired,
                    "An acting branch is required to create a BranchLocal product.");
            }

            var origin = PosBranchId.From(actor.ActingBranchId.Value);
            return ApplicationResult<(CatalogProductScope, PosBranchId?)>.Success(
                (CatalogProductScope.BranchLocal, origin));
        }

        // Owner/Admin omitted scope → OrganizationStandard
        return ApplicationResult<(CatalogProductScope, PosBranchId?)>.Success(
            (CatalogProductScope.OrganizationStandard, null));
    }

    public ApplicationResult EnsureCanEditMaster(CatalogGovernanceActor actor, CatalogProduct product)
    {
        if (product.Scope == CatalogProductScope.OrganizationStandard)
        {
            if (!CanEditOrganizationStandardMaster(actor))
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.ProductScopeForbidden,
                    "OrganizationStandard master fields may only be edited by organization Owner/Administrator.");
            }

            return ApplicationResult.Success();
        }

        if (!CanOperateBranchLocal(actor, product.OriginBranchId))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.ProductOriginBranchForbidden,
                "BranchLocal products may only be edited at their origin branch (or by organization governance).");
        }

        return ApplicationResult.Success();
    }

    public ApplicationResult EnsureCanMutateSellingPrice(CatalogGovernanceActor actor, CatalogProduct product)
    {
        if (product.Scope == CatalogProductScope.OrganizationStandard)
        {
            if (!CanMutateOrganizationStandardPrice(actor))
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.ProductScopeForbidden,
                    "OrganizationStandard selling price may only be changed by organization Owner/Administrator until branch pricing (MB2-03).");
            }

            return ApplicationResult.Success();
        }

        if (!CanOperateBranchLocal(actor, product.OriginBranchId))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.ProductOriginBranchForbidden,
                "BranchLocal price may only be changed at the origin branch (or by organization governance).");
        }

        return ApplicationResult.Success();
    }
}
