using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed class InventoryLotQueryService
{
    private readonly IInventoryLotRepository _lots;
    private readonly ICatalogProductRepository _products;
    private readonly IClock _clock;

    public InventoryLotQueryService(
        IInventoryLotRepository lots,
        ICatalogProductRepository products,
        IClock clock)
    {
        _lots = lots;
        _products = products;
        _clock = clock;
    }

    public async Task<PagedResult<PosInventoryLotDto>> ListAsync(
        Guid organizationId,
        Guid productId,
        bool includeDepleted,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var orgId = PosOrganizationId.From(organizationId);
        var catalogProductId = CatalogProductId.From(productId);
        var product = await _products.GetByIdAsync(orgId, catalogProductId, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            return new PagedResult<PosInventoryLotDto>([], 0, Math.Max(page ?? 1, 1), take);
        }

        var (items, total) = await _lots
            .ListPagedAsync(orgId, catalogProductId, branchId: null, includeDepleted, skip, take, cancellationToken)
            .ConfigureAwait(false);
        var today = InventoryLot.BusinessDateOf(_clock.UtcNow);
        var warning = product.EffectiveExpirationWarningDays;
        return new PagedResult<PosInventoryLotDto>(
            items.Select(l => Map(l, today, warning)).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static PosInventoryLotDto Map(InventoryLot lot, DateOnly today, int warningDays) =>
        new(
            lot.Id.Value,
            lot.ProductId.Value,
            lot.BranchId?.Value,
            lot.LotNumber,
            lot.ExpirationDate,
            lot.QuantityOnHand,
            InventoryLotExpiryStatuses.ToCode(lot.ExpiryStatus(today, warningDays)),
            lot.CreatedAtUtc,
            lot.UpdatedAtUtc);
}
