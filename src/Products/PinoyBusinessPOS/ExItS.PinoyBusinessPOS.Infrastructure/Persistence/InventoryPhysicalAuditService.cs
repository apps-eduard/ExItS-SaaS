using ExItS.PinoyBusinessPOS.Application.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

/// <summary>
/// Read-only inventory consistency audit: org/branch sums, reservations, constraints, optional lot totals.
/// </summary>
internal sealed class InventoryPhysicalAuditService : IInventoryPhysicalAudit
{
    private const string Reserved = "Reserved";
    private static readonly Guid EmptyBranchId = Guid.Empty;

    private readonly PosDbContext _db;

    public InventoryPhysicalAuditService(PosDbContext db) => _db = db;

    public async Task<InventoryPhysicalAuditResult> AuditAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var orgOnHandMismatch = 0;
        var orgReservedMismatch = 0;
        var reservationMismatch = 0;
        var negativeOnHand = 0;
        var negativeReserved = 0;
        var overReserved = 0;
        var lotMismatch = 0;
        var movementBranchIssue = 0;
        var unresolvedLegacy = 0;

        var accountsQuery = _db.InventoryAccounts.AsNoTracking().Where(a => a.IsTracked);
        if (organizationId is Guid orgFilter)
        {
            accountsQuery = accountsQuery.Where(a => a.OrganizationId == orgFilter);
        }

        var accounts = await accountsQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
        var orgIds = accounts.Select(a => a.OrganizationId).Distinct().ToList();

        var balancesQuery = _db.InventoryBranchBalances.AsNoTracking();
        if (organizationId is Guid balanceOrg)
        {
            balancesQuery = balancesQuery.Where(b => b.OrganizationId == balanceOrg);
        }

        var balances = await balancesQuery.ToListAsync(cancellationToken).ConfigureAwait(false);
        var balancesByOrgProduct = balances
            .GroupBy(b => (b.OrganizationId, b.ProductId))
            .ToDictionary(g => g.Key, g => g.ToList());

        var activeReserved = await BuildActiveReservationAggregateAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var docByOrgProduct = activeReserved
            .GroupBy(a => (a.OrganizationId, a.ProductId))
            .ToDictionary(g => g.Key, g => g.Sum(x => x.ReservedQuantity));

        foreach (var account in accounts)
        {
            var key = (account.OrganizationId, account.ProductId);
            balancesByOrgProduct.TryGetValue(key, out var productBalances);
            productBalances ??= [];

            var branchOnHandSum = productBalances.Sum(b => b.OnHandQuantity);
            if (branchOnHandSum != account.OnHandQuantity)
            {
                orgOnHandMismatch++;
            }

            var branchReservedSum = productBalances.Sum(b => b.ReservedQuantity);
            if (branchReservedSum != account.ReservedQuantity)
            {
                orgReservedMismatch++;
            }

            var docReserved = docByOrgProduct.GetValueOrDefault(key, 0m);
            if (docReserved != account.ReservedQuantity)
            {
                reservationMismatch++;
            }

            foreach (var balance in productBalances)
            {
                if (balance.OnHandQuantity < 0m)
                {
                    negativeOnHand++;
                }

                if (balance.ReservedQuantity < 0m)
                {
                    negativeReserved++;
                }

                if (balance.ReservedQuantity > balance.OnHandQuantity)
                {
                    overReserved++;
                }
            }
        }

        var expirationProductIds = await _db.CatalogProducts.AsNoTracking()
            .Where(p => p.TracksExpiration)
            .Select(p => new { p.OrganizationId, p.Id })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var expirationSet = expirationProductIds
            .Select(p => (p.OrganizationId, p.Id))
            .ToHashSet();

        foreach (var account in accounts.Where(a => expirationSet.Contains((a.OrganizationId, a.ProductId))))
        {
            var lotSum = await _db.InventoryLots.AsNoTracking()
                .Where(l => l.OrganizationId == account.OrganizationId
                    && l.ProductId == account.ProductId
                    && l.QuantityOnHand > 0m)
                .SumAsync(l => l.QuantityOnHand, cancellationToken)
                .ConfigureAwait(false);
            if (lotSum != account.OnHandQuantity)
            {
                lotMismatch++;
            }
        }

        var legacyNullMovements = await _db.StockMovements.AsNoTracking()
            .Where(m => m.BranchId == null)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
        unresolvedLegacy = legacyNullMovements;

        var orgCount = organizationId is Guid singleOrg ? 1 : orgIds.Count;
        return new InventoryPhysicalAuditResult(
            orgCount,
            accounts.Count,
            balances.Count,
            orgOnHandMismatch,
            orgReservedMismatch,
            reservationMismatch,
            negativeOnHand,
            negativeReserved,
            overReserved,
            lotMismatch,
            movementBranchIssue,
            unresolvedLegacy,
            orgOnHandMismatch == 0
                && orgReservedMismatch == 0
                && reservationMismatch == 0
                && negativeOnHand == 0
                && negativeReserved == 0
                && overReserved == 0
                && lotMismatch == 0);
    }

    private async Task<IReadOnlyList<ActiveReservationRow>> BuildActiveReservationAggregateAsync(
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        var salesQuery = _db.Sales.AsNoTracking().Where(s => s.StockReservationState == Reserved);
        if (organizationId is Guid orgFilter)
        {
            salesQuery = salesQuery.Where(s => s.OrganizationId == orgFilter);
        }

        if (await salesQuery.AnyAsync(s => s.BranchId == null, cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "Active Reserved sales with unresolved BranchId detected during audit.");
        }

        var ordersQuery = _db.CustomerOrders.AsNoTracking()
            .Where(o => o.StockReservationState == Reserved);
        if (organizationId is Guid orderOrg)
        {
            ordersQuery = ordersQuery.Where(o => o.SellerOrganizationId == orderOrg);
        }

        if (await ordersQuery.AnyAsync(o => o.FulfillmentBranchId == EmptyBranchId, cancellationToken)
                .ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "Active Reserved customer orders with unresolved FulfillmentBranchId detected during audit.");
        }

        var saleLines = await (
                from s in _db.Sales.AsNoTracking()
                join sl in _db.SaleLines.AsNoTracking()
                    on new { SaleId = s.Id, s.OrganizationId } equals new { sl.SaleId, sl.OrganizationId }
                join ia in _db.InventoryAccounts.AsNoTracking()
                    on new { s.OrganizationId, sl.ProductId } equals new { ia.OrganizationId, ia.ProductId }
                where s.StockReservationState == Reserved
                    && s.BranchId != null
                    && ia.IsTracked
                select new ActiveReservationRow(s.OrganizationId, s.BranchId!.Value, sl.ProductId, sl.Quantity))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (organizationId is Guid saleOrg)
        {
            saleLines = saleLines.Where(x => x.OrganizationId == saleOrg).ToList();
        }

        var orderLines = await (
                from o in _db.CustomerOrders.AsNoTracking()
                join ol in _db.CustomerOrderLines.AsNoTracking()
                    on new { OrderId = o.Id, OrganizationId = o.SellerOrganizationId }
                    equals new { ol.OrderId, OrganizationId = ol.SellerOrganizationId }
                join ia in _db.InventoryAccounts.AsNoTracking()
                    on new { OrganizationId = o.SellerOrganizationId, ol.ProductId }
                    equals new { ia.OrganizationId, ia.ProductId }
                where o.StockReservationState == Reserved
                    && o.FulfillmentBranchId != EmptyBranchId
                    && ia.IsTracked
                select new ActiveReservationRow(
                    o.SellerOrganizationId,
                    o.FulfillmentBranchId,
                    ol.ProductId,
                    ol.Quantity))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (organizationId is Guid orderOrgFilter)
        {
            orderLines = orderLines.Where(x => x.OrganizationId == orderOrgFilter).ToList();
        }

        return saleLines
            .Concat(orderLines)
            .GroupBy(x => (x.OrganizationId, x.BranchId, x.ProductId))
            .Select(g => new ActiveReservationRow(
                g.Key.OrganizationId,
                g.Key.BranchId,
                g.Key.ProductId,
                g.Sum(x => x.ReservedQuantity)))
            .ToList();
    }

    private sealed record ActiveReservationRow(
        Guid OrganizationId,
        Guid BranchId,
        Guid ProductId,
        decimal ReservedQuantity);
}
