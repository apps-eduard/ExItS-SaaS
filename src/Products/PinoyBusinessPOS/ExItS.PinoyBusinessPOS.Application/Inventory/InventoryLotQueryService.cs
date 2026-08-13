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

    public async Task<PosExpiringLotPagedResult> ListExpiringAsync(
        Guid organizationId,
        Guid? branchId,
        string? window,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = PosPagination.Normalize(page, pageSize);
        var orgId = PosOrganizationId.From(organizationId);
        var today = InventoryLot.BusinessDateOf(_clock.UtcNow);
        var (expireOnOrAfter, expireOnOrBefore) = ResolveWindow(window, fromDate, toDate, today);
        var branch = branchId is { } id && id != Guid.Empty ? PosBranchId.From(id) : null;

        var (items, total) = await _lots
            .ListExpiringPagedAsync(orgId, branch, expireOnOrBefore, expireOnOrAfter, search, skip, take, cancellationToken)
            .ConfigureAwait(false);
        var counts = await _lots
            .CountExpiryAsync(orgId, today, InventoryLot.DefaultWarningDays, cancellationToken)
            .ConfigureAwait(false);

        var productIds = items.Select(l => l.ProductId).Distinct().ToArray();
        var products = new Dictionary<CatalogProductId, CatalogProduct>();
        foreach (var productId in productIds)
        {
            var product = await _products.GetByIdAsync(orgId, productId, cancellationToken).ConfigureAwait(false);
            if (product is not null)
            {
                products[productId] = product;
            }
        }

        var mapped = items.Select(lot =>
        {
            products.TryGetValue(lot.ProductId, out var product);
            var warning = product?.EffectiveExpirationWarningDays ?? InventoryLot.DefaultWarningDays;
            return new PosExpiringLotDto(
                lot.Id.Value,
                lot.ProductId.Value,
                product?.Name ?? string.Empty,
                product?.Sku,
                lot.BranchId?.Value,
                lot.LotNumber,
                lot.ExpirationDate,
                lot.QuantityOnHand,
                InventoryLotExpiryStatuses.ToCode(lot.ExpiryStatus(today, warning)),
                warning);
        }).ToList();

        return new PosExpiringLotPagedResult(
            mapped,
            total,
            Math.Max(page ?? 1, 1),
            take,
            counts.ExpiredCount,
            counts.NearExpiryCount);
    }

    private static (DateOnly? ExpireOnOrAfter, DateOnly ExpireOnOrBefore) ResolveWindow(
        string? window,
        DateOnly? fromDate,
        DateOnly? toDate,
        DateOnly today)
    {
        var key = string.IsNullOrWhiteSpace(window) ? "Days30" : window.Trim();
        if (string.Equals(key, "Expired", StringComparison.OrdinalIgnoreCase))
        {
            return (null, today.AddDays(-1));
        }

        if (string.Equals(key, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            var from = fromDate ?? today;
            var to = toDate ?? today.AddDays(30);
            return from <= to ? (from, to) : (to, from);
        }

        var days = string.Equals(key, "Days7", StringComparison.OrdinalIgnoreCase) ? 7
            : string.Equals(key, "Days14", StringComparison.OrdinalIgnoreCase) ? 14
            : 30;
        return (null, today.AddDays(days));
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
