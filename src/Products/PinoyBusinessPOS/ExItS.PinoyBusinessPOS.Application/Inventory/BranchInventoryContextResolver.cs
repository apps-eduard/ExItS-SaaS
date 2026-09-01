using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>Resolves and validates branch workspace context for inventory operations.</summary>
public sealed class BranchInventoryContextResolver
{
    public const string InventoryBranchRequiredErrorCode = "pos.inventory.branch_required";

    private readonly IOrganizationBranchDirectory _branches;
    private readonly ICatalogGovernanceActorAccessor _actorAccessor;

    public BranchInventoryContextResolver(
        IOrganizationBranchDirectory branches,
        ICatalogGovernanceActorAccessor actorAccessor)
    {
        _branches = branches;
        _actorAccessor = actorAccessor;
    }

    public async Task<ApplicationResult<BranchInventoryContext>> ResolveAsync(
        Guid organizationId,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return ApplicationResult<BranchInventoryContext>.Failure(
                InventoryBranchRequiredErrorCode,
                "A selected branch is required for branch inventory.");
        }

        if (!await _branches.ExistsInOrganizationAsync(organizationId, branchId, cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<BranchInventoryContext>.Failure(
                DomainErrorCodes.InvalidBranchId,
                "The selected branch is not part of this organization.");
        }

        if (!await _branches.IsActiveInOrganizationAsync(organizationId, branchId, cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<BranchInventoryContext>.Failure(
                DomainErrorCodes.InvalidBranchId,
                "The selected branch is not active for inventory operations.");
        }

        var primaryBranchId = await _branches
            .GetPrimaryBranchIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var actor = _actorAccessor.GetActor();
        return ApplicationResult<BranchInventoryContext>.Success(
            new BranchInventoryContext(
                organizationId,
                branchId,
                primaryBranchId,
                actor.IsOrganizationGovernance));
    }

    public bool CanViewProductInManagement(CatalogProduct product, BranchInventoryContext context)
    {
        if (product.Scope != CatalogProductScope.BranchLocal)
        {
            return true;
        }

        if (context.OrganizationGovernance)
        {
            return true;
        }

        return product.OriginBranchId?.Value == context.BranchId;
    }
}
