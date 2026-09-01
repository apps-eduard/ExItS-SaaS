using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

/// <summary>
/// Application-level twin of <c>ReconcileBranchInventoryReservations</c> for tests and re-runs.
/// Optional <paramref name="organizationId"/> scopes verification to one tenant (recommended for re-runs).
/// </summary>
internal sealed class BranchInventoryReservationCutover : IBranchInventoryReservationCutover
{
    private const string Reserved = "Reserved";

    private readonly PosDbContext _db;

    public BranchInventoryReservationCutover(PosDbContext db) => _db = db;

    public Task<BranchInventoryReservationCutoverResult> AuditAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default) =>
        RunAsync(write: false, organizationId, cancellationToken);

    public Task<BranchInventoryReservationCutoverResult> ReconcileAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default) =>
        RunAsync(write: true, organizationId, cancellationToken);

    private async Task<BranchInventoryReservationCutoverResult> RunAsync(
        bool write,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var salesQuery = _db.Sales.AsNoTracking()
            .Where(s => s.StockReservationState == Reserved);
        if (organizationId is Guid orgFilter)
        {
            salesQuery = salesQuery.Where(s => s.OrganizationId == orgFilter);
        }

        var unresolved = await salesQuery
            .CountAsync(s => s.BranchId == null, cancellationToken)
            .ConfigureAwait(false);
        if (unresolved > 0)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryBranchReservationCutoverUnresolvedSaleBranch,
                "Active Reserved sales require durable BranchId; Main is not assumed.");
        }

        var saleLinesQuery =
            from s in _db.Sales.AsNoTracking()
            join sl in _db.SaleLines.AsNoTracking()
                on new { SaleId = s.Id, s.OrganizationId } equals new { sl.SaleId, sl.OrganizationId }
            join ia in _db.InventoryAccounts.AsNoTracking()
                on new { s.OrganizationId, sl.ProductId } equals new { ia.OrganizationId, ia.ProductId }
            where s.StockReservationState == Reserved
                && s.BranchId != null
                && ia.IsTracked
            select new
            {
                s.OrganizationId,
                BranchId = s.BranchId!.Value,
                sl.ProductId,
                sl.Quantity
            };
        if (organizationId is Guid saleOrg)
        {
            saleLinesQuery = saleLinesQuery.Where(x => x.OrganizationId == saleOrg);
        }

        var saleLines = await saleLinesQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

        var orderLinesQuery =
            from o in _db.CustomerOrders.AsNoTracking()
            join ol in _db.CustomerOrderLines.AsNoTracking()
                on new { OrderId = o.Id, OrganizationId = o.SellerOrganizationId }
                equals new { ol.OrderId, OrganizationId = ol.SellerOrganizationId }
            join ia in _db.InventoryAccounts.AsNoTracking()
                on new { OrganizationId = o.SellerOrganizationId, ol.ProductId }
                equals new { ia.OrganizationId, ia.ProductId }
            where o.StockReservationState == Reserved && ia.IsTracked
            select new
            {
                OrganizationId = o.SellerOrganizationId,
                BranchId = o.FulfillmentBranchId,
                ol.ProductId,
                ol.Quantity
            };
        if (organizationId is Guid orderOrg)
        {
            orderLinesQuery = orderLinesQuery.Where(x => x.OrganizationId == orderOrg);
        }

        var orderLines = await orderLinesQuery.ToListAsync(cancellationToken).ConfigureAwait(false);

        var aggregates = saleLines
            .Concat(orderLines)
            .GroupBy(x => (x.OrganizationId, x.BranchId, x.ProductId))
            .Select(g => new BranchInventoryReservationAggregate(
                g.Key.OrganizationId,
                g.Key.BranchId,
                g.Key.ProductId,
                g.Sum(x => x.Quantity)))
            .OrderBy(a => a.OrganizationId)
            .ThenBy(a => a.BranchId)
            .ThenBy(a => a.ProductId)
            .ToList();

        var saleDocCount = await salesQuery
            .CountAsync(s => s.BranchId != null, cancellationToken)
            .ConfigureAwait(false);

        var ordersQuery = _db.CustomerOrders.AsNoTracking()
            .Where(o => o.StockReservationState == Reserved);
        if (organizationId is Guid orderDocOrg)
        {
            ordersQuery = ordersQuery.Where(o => o.SellerOrganizationId == orderDocOrg);
        }

        var orderDocCount = await ordersQuery.CountAsync(cancellationToken).ConfigureAwait(false);

        var docByOrgProduct = aggregates
            .GroupBy(a => (a.OrganizationId, a.ProductId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ReservedQuantity));

        var orgAccountsQuery = _db.InventoryAccounts.AsNoTracking().Where(a => a.IsTracked);
        if (organizationId is Guid accountOrg)
        {
            orgAccountsQuery = orgAccountsQuery.Where(a => a.OrganizationId == accountOrg);
        }

        var orgReservedRows = await orgAccountsQuery
            .Where(a => a.ReservedQuantity != 0m)
            .Select(a => new { a.OrganizationId, a.ProductId, a.ReservedQuantity })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var row in orgReservedRows)
        {
            var doc = docByOrgProduct.GetValueOrDefault((row.OrganizationId, row.ProductId), 0m);
            if (doc != row.ReservedQuantity)
            {
                throw new DomainException(
                    DomainErrorCodes.InventoryBranchReservationCutoverOrgMismatch,
                    "Organization ReservedQuantity does not equal sum of branch-attributable active reservations.");
            }
        }

        foreach (var pair in docByOrgProduct)
        {
            var orgQty = orgReservedRows
                .FirstOrDefault(a => a.OrganizationId == pair.Key.OrganizationId && a.ProductId == pair.Key.ProductId)
                ?.ReservedQuantity
                ?? 0m;
            if (orgQty != pair.Value)
            {
                throw new DomainException(
                    DomainErrorCodes.InventoryBranchReservationCutoverOrgMismatch,
                    "Organization ReservedQuantity does not equal sum of branch-attributable active reservations.");
            }
        }

        var updated = 0;
        if (aggregates.Count > 0)
        {
            var orgIds = aggregates.Select(a => a.OrganizationId).Distinct().ToList();
            var balances = await _db.InventoryBranchBalances
                .Where(b => orgIds.Contains(b.OrganizationId))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            var byKey = balances.ToDictionary(b => (b.OrganizationId, b.BranchId, b.ProductId));

            foreach (var agg in aggregates)
            {
                if (!byKey.TryGetValue((agg.OrganizationId, agg.BranchId, agg.ProductId), out var balance))
                {
                    throw new DomainException(
                        DomainErrorCodes.InventoryBranchReservationCutoverMissingBalance,
                        "Active reservation targets a branch/product without InventoryBranchBalance; OnHand will not be invented.");
                }

                if (agg.ReservedQuantity > balance.OnHandQuantity)
                {
                    throw new DomainException(
                        DomainErrorCodes.InventoryBranchReservationCutoverOverReserved,
                        "Active reservations exceed branch OnHand; remediating data is required before cutover.");
                }
            }

            if (write)
            {
                var now = DateTimeOffset.UtcNow;
                foreach (var agg in aggregates)
                {
                    var balance = byKey[(agg.OrganizationId, agg.BranchId, agg.ProductId)];
                    if (balance.ReservedQuantity == agg.ReservedQuantity)
                    {
                        continue;
                    }

                    balance.ReservedQuantity = agg.ReservedQuantity;
                    balance.UpdatedAtUtc = now;
                    updated++;
                }

                await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        return new BranchInventoryReservationCutoverResult(
            saleDocCount + orderDocCount,
            aggregates.Count,
            updated,
            aggregates);
    }
}
