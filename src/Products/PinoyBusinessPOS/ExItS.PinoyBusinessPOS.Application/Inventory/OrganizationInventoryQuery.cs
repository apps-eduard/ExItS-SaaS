using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Organization-level inventory aggregate with per-branch breakdown (MB2-02C).
/// Independent of workspace branch selection.
/// </summary>
public sealed class OrganizationInventoryQuery
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryBranchBalanceRepository _balances;

    public OrganizationInventoryQuery(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IInventoryBranchBalanceRepository balances)
    {
        _inventory = inventory;
        _products = products;
        _balances = balances;
    }

    public async Task<ApplicationResult<PosOrganizationInventoryProductDto>> GetProductAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<PosOrganizationInventoryProductDto>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        var account = await _inventory
            .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        if (account is null || !account.IsTracked)
        {
            return ApplicationResult<PosOrganizationInventoryProductDto>.Failure(
                DomainErrorCodes.InventoryNotTracked,
                "Inventory is not tracked for this product.");
        }

        var branchBalances = await _balances
            .ListByProductIdsAsync(orgId, [catalogProductId], cancellationToken)
            .ConfigureAwait(false);

        var branches = branchBalances
            .OrderBy(b => b.BranchId.Value)
            .Select(b => new PosInventoryBranchBreakdownDto(
                b.BranchId.Value,
                b.OnHandQuantity,
                b.ReservedQuantity,
                b.AvailableQuantity))
            .ToList();

        return ApplicationResult<PosOrganizationInventoryProductDto>.Success(
            new PosOrganizationInventoryProductDto(
                productId,
                product.Name,
                product.UnitOfMeasure.ToString(),
                account.OnHandQuantity,
                account.ReservedQuantity,
                account.AvailableQuantity,
                branches));
    }
}
