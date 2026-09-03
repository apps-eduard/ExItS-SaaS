using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

/// <summary>
/// Branch grouping metadata for a caller, resolved from Platform in one authorization-filtered read.
/// Branches absent from this list are not visible to the caller and must not contribute to any total.
/// </summary>
public sealed record AuthorizedBranchGrouping(
    Guid BranchId,
    string BranchName,
    Guid? AreaId,
    string? AreaName);

public interface IAuthorizedBranchGroupingDirectory
{
    Task<IReadOnlyList<AuthorizedBranchGrouping>> ListAuthorizedAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}

public sealed record PosInventoryBranchRollupDto(
    Guid BranchId,
    string BranchName,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity);

/// <summary>
/// Derived area subtotal. Never persisted: moving a branch between areas changes this projection only.
/// </summary>
public sealed record PosInventoryAreaRollupDto(
    Guid? AreaId,
    string? AreaName,
    bool IsUnassigned,
    decimal OnHandQuantity,
    decimal ReservedQuantity,
    decimal AvailableQuantity,
    IReadOnlyList<PosInventoryBranchRollupDto> Branches);

public sealed record PosInventoryStockRollupDto(
    Guid ProductId,
    string ProductName,
    string UnitOfMeasure,
    bool IsTracked,
    decimal OrganizationOnHandQuantity,
    decimal OrganizationReservedQuantity,
    decimal OrganizationAvailableQuantity,
    bool HasAreas,
    IReadOnlyList<PosInventoryAreaRollupDto> Areas);

/// <summary>
/// Hierarchical Organization → Area → Branch stock read (AREA-02).
/// Organization values stay the authoritative <see cref="InventoryAccount"/> figures; area values are
/// derived sums of authorized branch balances only. Read-only: no stock authority lives on an area.
/// </summary>
public sealed class InventoryStockRollupQuery
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly IAuthorizedBranchGroupingDirectory _grouping;

    public InventoryStockRollupQuery(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IInventoryBranchBalanceRepository balances,
        IAuthorizedBranchGroupingDirectory grouping)
    {
        _inventory = inventory;
        _products = products;
        _balances = balances;
        _grouping = grouping;
    }

    public async Task<ApplicationResult<PosInventoryStockRollupDto>> GetProductAsync(
        Guid organizationId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return ApplicationResult<PosInventoryStockRollupDto>.Failure(
                ApplicationErrorCodes.InventoryProductNotFound,
                "Product was not found.");
        }

        var account = await _inventory
            .GetByProductIdAsync(orgId, catalogProductId, cancellationToken)
            .ConfigureAwait(false);
        if (account is null || !account.IsTracked)
        {
            return ApplicationResult<PosInventoryStockRollupDto>.Success(
                new PosInventoryStockRollupDto(
                    productId,
                    product.Name,
                    product.UnitOfMeasure.ToString(),
                    IsTracked: false,
                    0m,
                    0m,
                    0m,
                    HasAreas: false,
                    []));
        }

        var authorized = await _grouping
            .ListAuthorizedAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var groupingByBranchId = authorized
            .GroupBy(b => b.BranchId)
            .ToDictionary(g => g.Key, g => g.First());

        var balances = await _balances
            .ListByProductIdsAsync(orgId, [catalogProductId], cancellationToken)
            .ConfigureAwait(false);

        var visibleRows = balances
            .Where(b => groupingByBranchId.ContainsKey(b.BranchId.Value))
            .Select(b =>
            {
                var branch = groupingByBranchId[b.BranchId.Value];
                return new
                {
                    branch.AreaId,
                    branch.AreaName,
                    Row = new PosInventoryBranchRollupDto(
                        b.BranchId.Value,
                        branch.BranchName,
                        b.OnHandQuantity,
                        b.ReservedQuantity,
                        b.OnHandQuantity - b.ReservedQuantity)
                };
            })
            .ToList();

        var areas = visibleRows
            .GroupBy(x => x.AreaId)
            .Select(group => new PosInventoryAreaRollupDto(
                group.Key,
                group.Key is null ? null : group.Select(x => x.AreaName).FirstOrDefault(name => name is not null),
                IsUnassigned: group.Key is null,
                group.Sum(x => x.Row.OnHandQuantity),
                group.Sum(x => x.Row.ReservedQuantity),
                group.Sum(x => x.Row.OnHandQuantity) - group.Sum(x => x.Row.ReservedQuantity),
                group
                    .Select(x => x.Row)
                    .OrderBy(row => row.BranchName, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .OrderBy(area => area.IsUnassigned)
            .ThenBy(area => area.AreaName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ApplicationResult<PosInventoryStockRollupDto>.Success(
            new PosInventoryStockRollupDto(
                productId,
                product.Name,
                product.UnitOfMeasure.ToString(),
                IsTracked: true,
                account.OnHandQuantity,
                account.ReservedQuantity,
                account.AvailableQuantity,
                areas.Any(area => !area.IsUnassigned),
                areas));
    }
}
