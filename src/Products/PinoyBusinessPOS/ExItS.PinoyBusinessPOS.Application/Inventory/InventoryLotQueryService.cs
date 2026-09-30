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
    private readonly BranchExpirationPolicyResolver _expirationPolicies;
    private readonly IInventoryLotIdentityEditSupport _identitySupport;
    private readonly IClock _clock;

    public InventoryLotQueryService(
        IInventoryLotRepository lots,
        ICatalogProductRepository products,
        BranchExpirationPolicyResolver expirationPolicies,
        IInventoryLotIdentityEditSupport identitySupport,
        IClock clock)
    {
        _lots = lots;
        _products = products;
        _expirationPolicies = expirationPolicies;
        _identitySupport = identitySupport;
        _clock = clock;
    }

    public async Task<PagedResult<PosInventoryLotDto>> ListAsync(
        Guid organizationId,
        Guid productId,
        bool includeDepleted,
        int? page,
        int? pageSize,
        Guid? branchId = null,
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

        PosBranchId? branch = branchId is { } id && id != Guid.Empty ? PosBranchId.From(id) : null;
        var (items, total) = await _lots
            .ListPagedAsync(orgId, catalogProductId, branch, includeDepleted, skip, take, cancellationToken)
            .ConfigureAwait(false);
        var today = InventoryLot.BusinessDateOf(_clock.UtcNow);
        // LEGACY_COMPAT: product-level warning when no branch context is supplied.
        var warning = branch is PosBranchId warningBranch
            ? (await _expirationPolicies
                .ResolveAsync(orgId, warningBranch, catalogProductId, cancellationToken)
                .ConfigureAwait(false)).EffectiveWarningDays
            : product.EffectiveExpirationWarningDays;

        var editability = await ResolveEditabilityAsync(
                orgId,
                items.Select(l => l.Id.Value).ToArray(),
                cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PosInventoryLotDto>(
            items.Select(l =>
            {
                editability.TryGetValue(l.Id.Value, out var lockReason);
                lockReason ??= InventoryLotIdentityEditPolicy.LockReasonNone;
                return Map(
                    l,
                    today,
                    warning,
                    InventoryLotIdentityEditPolicy.CanEditIdentity(lockReason),
                    lockReason);
            }).ToList(),
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
            .CountExpiryAsync(orgId, today, branch, cancellationToken)
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

        var warningByBranchProduct = new Dictionary<(Guid BranchId, Guid ProductId), int>();
        if (branch is PosBranchId listBranch && productIds.Length > 0)
        {
            var policies = await _expirationPolicies
                .ResolveManyAsync(orgId, listBranch, productIds, cancellationToken)
                .ConfigureAwait(false);
            foreach (var (productKey, policy) in policies)
            {
                warningByBranchProduct[(listBranch.Value, productKey)] = policy.EffectiveWarningDays;
            }
        }
        else
        {
            foreach (var group in items.Where(l => l.BranchId is not null).GroupBy(l => l.BranchId!.Value))
            {
                var ids = group.Select(l => l.ProductId).Distinct().ToList();
                var policies = await _expirationPolicies
                    .ResolveManyAsync(orgId, PosBranchId.From(group.Key), ids, cancellationToken)
                    .ConfigureAwait(false);
                foreach (var (productKey, policy) in policies)
                {
                    warningByBranchProduct[(group.Key, productKey)] = policy.EffectiveWarningDays;
                }
            }
        }

        var mapped = items.Select(lot =>
        {
            products.TryGetValue(lot.ProductId, out var product);
            var warning = lot.BranchId is PosBranchId lotBranch
                && warningByBranchProduct.TryGetValue((lotBranch.Value, lot.ProductId.Value), out var days)
                    ? days
                    : product?.EffectiveExpirationWarningDays ?? InventoryLot.DefaultWarningDays;
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

    public static PosInventoryLotDto Map(
        InventoryLot lot,
        DateOnly today,
        int warningDays,
        bool canEditIdentity = false,
        string? identityLockReason = null) =>
        new(
            lot.Id.Value,
            lot.ProductId.Value,
            lot.BranchId?.Value,
            lot.LotNumber,
            lot.ExpirationDate,
            lot.QuantityOnHand,
            InventoryLotExpiryStatuses.ToCode(lot.ExpiryStatus(today, warningDays)),
            lot.CreatedAtUtc,
            lot.UpdatedAtUtc,
            canEditIdentity,
            identityLockReason ?? InventoryLotIdentityEditPolicy.LockReasonNone);

    private async Task<Dictionary<Guid, string>> ResolveEditabilityAsync(
        PosOrganizationId organizationId,
        IReadOnlyCollection<Guid> lotIds,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, string>();
        if (lotIds.Count == 0)
        {
            return result;
        }

        var movementsByLot = await _identitySupport
            .ListDistinctMovementTypesByLotIdsAsync(organizationId, lotIds, cancellationToken)
            .ConfigureAwait(false);
        var draftRefs = await _identitySupport
            .ListLotIdsReferencedByActiveTransferDraftAsync(organizationId, lotIds, cancellationToken)
            .ConfigureAwait(false);

        foreach (var lotId in lotIds)
        {
            movementsByLot.TryGetValue(lotId, out var types);
            types ??= Array.Empty<StockMovementType>();
            result[lotId] = InventoryLotIdentityEditPolicy.ResolveLockReason(
                types,
                draftRefs.Contains(lotId));
        }

        return result;
    }
}
