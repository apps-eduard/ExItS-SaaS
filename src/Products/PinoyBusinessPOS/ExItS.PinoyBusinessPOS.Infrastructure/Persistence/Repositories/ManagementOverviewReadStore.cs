using ExItS.PinoyBusinessPOS.Application.Reporting;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class ManagementOverviewReadStore : IManagementOverviewReadStore
{
    private readonly PosDbContext _db;

    public ManagementOverviewReadStore(PosDbContext db) => _db = db;

    public async Task<PosManagementOverviewDto> GetAsync(
        Guid organizationId,
        DateOnly businessDate,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = new DateTimeOffset(businessDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtc = fromUtc.AddDays(1);
        var completed = nameof(SaleStatus.Completed);
        var cash = nameof(SalePaymentMethod.Cash);
        var utang = nameof(SalePaymentMethod.Utang);
        var repaymentActive = nameof(RepaymentStatus.Active);
        var creditActive = nameof(CreditEntryStatus.Active);
        var shiftOpen = nameof(CashierShiftStatus.Open);
        var registerActive = nameof(RegisterStatus.Active);
        var pendingStatuses = new[]
        {
            nameof(InventoryTransferStatus.Draft),
            nameof(InventoryTransferStatus.InTransit),
            nameof(InventoryTransferStatus.PartiallyReceived)
        };

        var todaySales = await _db.Sales.AsNoTracking()
            .Where(s => s.OrganizationId == organizationId
                && s.Status == completed
                && s.RecordedAtUtc >= fromUtc
                && s.RecordedAtUtc < toUtc)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Sum(s => s.Total),
                Count = g.Count(),
                Cash = g.Where(s => s.PaymentMethod == cash).Sum(s => s.Total),
                Utang = g.Where(s => s.PaymentMethod == utang).Sum(s => s.Total)
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var paymentsReceived = await _db.Repayments.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId
                && r.Status == repaymentActive
                && r.RecordedAtUtc >= fromUtc
                && r.RecordedAtUtc < toUtc)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken)
            .ConfigureAwait(false) ?? 0m;

        var creditTotal = await _db.CreditEntries.AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.Status == creditActive)
            .SumAsync(c => (decimal?)c.Amount, cancellationToken)
            .ConfigureAwait(false) ?? 0m;
        var repaymentTotal = await _db.Repayments.AsNoTracking()
            .Where(r => r.OrganizationId == organizationId && r.Status == repaymentActive)
            .SumAsync(r => (decimal?)r.Amount, cancellationToken)
            .ConfigureAwait(false) ?? 0m;
        var openUtang = creditTotal > repaymentTotal ? creditTotal - repaymentTotal : 0m;

        var lowStock = await _db.InventoryAccounts.AsNoTracking()
            .CountAsync(
                a => a.OrganizationId == organizationId
                    && a.IsTracked
                    && a.ReorderLevel != null
                    && a.OnHandQuantity <= a.ReorderLevel,
                cancellationToken)
            .ConfigureAwait(false);

        var nearUntil = businessDate.AddDays(InventoryLot.DefaultWarningDays);
        var expiredLots = await _db.InventoryLots.AsNoTracking()
            .CountAsync(
                l => l.OrganizationId == organizationId
                    && l.QuantityOnHand > 0m
                    && l.ExpirationDate < businessDate,
                cancellationToken)
            .ConfigureAwait(false);
        var nearExpiryLots = await _db.InventoryLots.AsNoTracking()
            .CountAsync(
                l => l.OrganizationId == organizationId
                    && l.QuantityOnHand > 0m
                    && l.ExpirationDate >= businessDate
                    && l.ExpirationDate <= nearUntil,
                cancellationToken)
            .ConfigureAwait(false);

        var pendingTransfers = await _db.InventoryTransfers.AsNoTracking()
            .CountAsync(
                t => t.OrganizationId == organizationId && pendingStatuses.Contains(t.Status),
                cancellationToken)
            .ConfigureAwait(false);
        var openShifts = await _db.CashierShifts.AsNoTracking()
            .CountAsync(
                s => s.OrganizationId == organizationId && s.Status == shiftOpen,
                cancellationToken)
            .ConfigureAwait(false);
        var activeRegisters = await _db.Registers.AsNoTracking()
            .CountAsync(
                r => r.OrganizationId == organizationId && r.Status == registerActive,
                cancellationToken)
            .ConfigureAwait(false);

        return new PosManagementOverviewDto(
            businessDate,
            todaySales?.Total ?? 0m,
            todaySales?.Count ?? 0,
            todaySales?.Cash ?? 0m,
            todaySales?.Utang ?? 0m,
            paymentsReceived,
            openUtang,
            lowStock,
            expiredLots,
            nearExpiryLots,
            pendingTransfers,
            openShifts,
            activeRegisters);
    }
}
