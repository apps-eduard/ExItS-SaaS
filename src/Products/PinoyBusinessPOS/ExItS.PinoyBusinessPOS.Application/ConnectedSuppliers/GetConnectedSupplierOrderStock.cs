using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record ConnectedOrderStockRequest(IReadOnlyList<Guid> SupplierProductIds);

public sealed record ConnectedOrderStockItemDto(
    Guid SupplierProductId,
    bool IsTracked,
    decimal AvailableBaseQuantity);

public sealed record ConnectedOrderStockDto(
    Guid RelationshipId,
    Guid? SupplierBranchId,
    string? SupplierBranchName,
    IReadOnlyList<ConnectedOrderStockItemDto> Items);

/// <summary>
/// Buyer-safe read of supplier-branch available stock for connected PO product picking.
/// Does not mutate inventory.
/// </summary>
public sealed class GetConnectedSupplierOrderStock
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IInventoryRepository _inventory;
    private readonly IInventoryBranchBalanceRepository _balances;
    private readonly IOrganizationBranchDirectory? _branches;
    private readonly IPosCommercialAccessAccessor _access;

    public GetConnectedSupplierOrderStock(
        IConnectedSupplierRelationshipRepository relationships,
        IInventoryRepository inventory,
        IInventoryBranchBalanceRepository balances,
        IPosCommercialAccessAccessor access,
        IOrganizationBranchDirectory? branches = null)
    {
        _relationships = relationships;
        _inventory = inventory;
        _balances = balances;
        _access = access;
        _branches = branches;
    }

    public async Task<ApplicationResult<ConnectedOrderStockDto>> ExecuteAsync(
        Guid buyerOrganizationId,
        Guid relationshipId,
        ConnectedOrderStockRequest request,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewPurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedOrderStockDto>(
                gate.ErrorCode!,
                gate.ErrorMessage!);
        }

        var buyer = PosOrganizationId.From(buyerOrganizationId);
        var relationship = await _relationships
            .GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null
            || relationship.BuyerOrganizationId != buyer
            || relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedOrderStockDto>(
                ConnectedSupplierErrorCodes.NotFound,
                "Active relationship was not found.");
        }

        var productIds = (request.SupplierProductIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var snapshots = await ConnectedPurchaseOrderSupplierStock
            .LoadSnapshotsAsync(
                relationship.SupplierOrganizationId,
                relationship.SupplierBranchId,
                productIds,
                _inventory,
                _balances,
                _branches,
                cancellationToken)
            .ConfigureAwait(false);

        var items = productIds
            .Select(id =>
            {
                if (!snapshots.TryGetValue(id, out var snapshot))
                {
                    return new ConnectedOrderStockItemDto(id, IsTracked: false, AvailableBaseQuantity: 0m);
                }

                return new ConnectedOrderStockItemDto(
                    id,
                    snapshot.IsTracked,
                    snapshot.AvailableBaseQuantity);
            })
            .ToList();

        return ApplicationResult<ConnectedOrderStockDto>.Success(
            new ConnectedOrderStockDto(
                relationship.Id.Value,
                relationship.SupplierBranchId,
                relationship.SupplierBranchNameSnapshot,
                items));
    }
}
