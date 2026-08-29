using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Reporting;

public sealed class SaleReportAggregateEquivalenceTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset T0 = new(2026, 8, 10, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AggregatePeriod_Matches_InMemoryListForReportTotals()
    {
        var sales = BuildSales();
        var repo = new InMemorySaleReportRepository(sales);
        var from = DateOnly.FromDateTime(T0.UtcDateTime);
        var to = from.AddDays(2);

        var aggregate = await repo.AggregatePeriodAsync(Org, from, to);
        var loaded = await repo.ListForReportAsync(Org, from, to);
        var completed = loaded.Where(s => s.Status == SaleStatus.Completed).ToList();
        var voided = loaded.Where(s => s.Status == SaleStatus.Voided).ToList();

        Assert.Equal(SaleMoney.RoundMoney(completed.Sum(s => s.Total)), aggregate.CompletedTotal);
        Assert.Equal(completed.Count, aggregate.CompletedCount);
        Assert.Equal(SaleMoney.RoundMoney(voided.Sum(s => s.Total)), aggregate.VoidedTotal);
        Assert.Equal(voided.Count, aggregate.VoidedCount);
        Assert.Equal(
            SaleMoney.RoundMoney(completed.Where(s => s.PaymentMethod == SalePaymentMethod.Cash).Sum(s => s.Total)),
            aggregate.CashTotal);
        Assert.Equal(
            SaleMoney.RoundMoney(completed.Where(s => s.PaymentMethod == SalePaymentMethod.ManualGCash).Sum(s => s.Total)),
            aggregate.ManualGCashTotal);
        Assert.Equal(
            SaleMoney.RoundMoney(completed.Where(s => s.PaymentMethod == SalePaymentMethod.Utang).Sum(s => s.Total)),
            aggregate.UtangTotal);
    }

    [Fact]
    public async Task AggregateCompletedByPayment_Matches_GroupByOnLoadedSales()
    {
        var sales = BuildSales();
        var repo = new InMemorySaleReportRepository(sales);
        var from = DateOnly.FromDateTime(T0.UtcDateTime);
        var to = from.AddDays(2);

        var aggregate = await repo.AggregateCompletedByPaymentAsync(Org, from, to);
        var loaded = (await repo.ListForReportAsync(Org, from, to))
            .Where(s => s.Status == SaleStatus.Completed)
            .GroupBy(s => SalePaymentMethods.ToCode(s.PaymentMethod))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new SalePaymentAggregate(
                g.Key,
                SaleMoney.RoundMoney(g.Sum(x => x.Total)),
                g.Count()))
            .ToList();

        Assert.Equal(loaded.Count, aggregate.Count);
        for (var i = 0; i < loaded.Count; i++)
        {
            Assert.Equal(loaded[i].PaymentMethod, aggregate[i].PaymentMethod);
            Assert.Equal(loaded[i].Total, aggregate[i].Total);
            Assert.Equal(loaded[i].Count, aggregate[i].Count);
        }
    }

    private static List<Sale> BuildSales()
    {
        var product = CatalogProductId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
        return
        [
            CompletedSale("S-001", SalePaymentMethod.Cash, 100m, T0, product),
            CompletedSale("S-002", SalePaymentMethod.ManualGCash, 50m, T0.AddHours(1), product),
            CompletedSale("S-003", SalePaymentMethod.Utang, 75m, T0.AddDays(1), product),
            VoidedSale("S-004", 40m, T0.AddDays(1).AddHours(2), product)
        ];
    }

    private static Sale CompletedSale(
        string number,
        SalePaymentMethod method,
        decimal amount,
        DateTimeOffset at,
        CatalogProductId productId)
    {
        var saleId = SaleId.New();
        var line = SaleLine.Create(
            saleId,
            Org,
            1,
            new SaleLineDraft(productId, "Item", "SKU1", null, UnitOfMeasure.Piece, amount, 1m));
        return Sale.Rehydrate(
            saleId,
            Org,
            number,
            SaleStatus.Completed,
            method,
            line.LineTotal,
            line.LineTotal,
            0m,
            method == SalePaymentMethod.Cash ? line.LineTotal : null,
            method == SalePaymentMethod.Cash ? 0m : null,
            null,
            at,
            Actor,
            null,
            null,
            null,
            at,
            [line],
            cashierShiftId: CashierShiftId.New());
    }

    private static Sale VoidedSale(string number, decimal amount, DateTimeOffset at, CatalogProductId productId)
    {
        var sale = CompletedSale(number, SalePaymentMethod.Cash, amount, at, productId);
        sale.Void("test void", Actor, at.AddMinutes(5));
        return sale;
    }

    private sealed class InMemorySaleReportRepository(IReadOnlyList<Sale> items) : ISaleRepository
    {
        public Task<Sale?> GetByIdAsync(PosOrganizationId organizationId, SaleId saleId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Sale?> FindBySaleNumberAsync(PosOrganizationId organizationId, string saleNumber, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<(IReadOnlyList<Sale> Items, int TotalCount)> ListAsync(
            PosOrganizationId organizationId, SaleFilter filter, int skip, int take, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Sale>> ListForReportAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            SaleStatus? status = null,
            SalePaymentMethod? paymentMethod = null,
            Guid? productId = null,
            Guid? customerId = null,
            CancellationToken cancellationToken = default)
        {
            var from = new DateTimeOffset(fromDateUtc.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var exclusiveTo = new DateTimeOffset(toDateUtc.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            IEnumerable<Sale> q = items.Where(s =>
                s.OrganizationId == organizationId
                && s.RecordedAtUtc >= from
                && s.RecordedAtUtc < exclusiveTo);
            if (status is not null)
            {
                q = q.Where(s => s.Status == status);
            }

            if (paymentMethod is not null)
            {
                q = q.Where(s => s.PaymentMethod == paymentMethod);
            }

            if (customerId is not null)
            {
                q = q.Where(s => s.CustomerId?.Value == customerId);
            }

            return Task.FromResult<IReadOnlyList<Sale>>(q.ToList());
        }

        public async Task<SalePeriodAggregate> AggregatePeriodAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            SaleStatus? status = null,
            SalePaymentMethod? paymentMethod = null,
            Guid? customerId = null,
            Guid? branchId = null,
            CancellationToken cancellationToken = default)
        {
            var loaded = await ListForReportAsync(
                    organizationId, fromDateUtc, toDateUtc, status, paymentMethod, null, customerId, cancellationToken)
                .ConfigureAwait(false);
            if (branchId is not null)
            {
                loaded = loaded.Where(s => s.BranchId?.Value == branchId).ToList();
            }
            var completed = loaded.Where(s => s.Status == SaleStatus.Completed).ToList();
            var voided = loaded.Where(s => s.Status == SaleStatus.Voided).ToList();
            return new SalePeriodAggregate(
                SaleMoney.RoundMoney(completed.Sum(s => s.Total)),
                completed.Count,
                SaleMoney.RoundMoney(voided.Sum(s => s.Total)),
                voided.Count,
                SaleMoney.RoundMoney(completed.Where(s => s.PaymentMethod == SalePaymentMethod.Cash).Sum(s => s.Total)),
                SaleMoney.RoundMoney(completed.Where(s => s.PaymentMethod == SalePaymentMethod.ManualGCash).Sum(s => s.Total)),
                SaleMoney.RoundMoney(completed.Where(s => s.PaymentMethod == SalePaymentMethod.Utang).Sum(s => s.Total)),
                completed.Count(s => s.PaymentMethod == SalePaymentMethod.Utang));
        }

        public Task<SaleCostPeriodAggregate> AggregateCostForProfitabilityAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            Guid? branchId = null,
            CancellationToken cancellationToken = default)
        {
            var from = new DateTimeOffset(fromDateUtc.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var exclusiveTo = new DateTimeOffset(toDateUtc.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            var q = items.Where(s =>
                s.OrganizationId == organizationId
                && s.RecordedAtUtc >= from
                && s.RecordedAtUtc < exclusiveTo
                && s.Status == SaleStatus.Completed);
            if (branchId is not null)
            {
                q = q.Where(s => s.BranchId?.Value == branchId);
            }

            var list = q.ToList();
            var complete = list.Count(s => s.CostStatus == ProductionCostStatus.Complete);
            var partial = list.Count(s => s.CostStatus == ProductionCostStatus.Partial);
            var unavailable = list.Count(s => s.CostStatus == ProductionCostStatus.Unavailable);
            var known = SaleMoney.RoundMoney(list
                .Where(s => s.CostStatus is ProductionCostStatus.Complete or ProductionCostStatus.Partial)
                .Sum(s => s.TotalCostSnapshot ?? 0m));
            return Task.FromResult(new SaleCostPeriodAggregate(list.Count, complete, partial, unavailable, known));
        }

        public async Task<IReadOnlyList<SalePaymentAggregate>> AggregateCompletedByPaymentAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default)
        {
            var loaded = await ListForReportAsync(organizationId, fromDateUtc, toDateUtc, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return loaded
                .Where(s => s.Status == SaleStatus.Completed)
                .GroupBy(s => SalePaymentMethods.ToCode(s.PaymentMethod))
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new SalePaymentAggregate(g.Key, SaleMoney.RoundMoney(g.Sum(x => x.Total)), g.Count()))
                .ToList();
        }

        public Task<IReadOnlyList<SaleDailyAggregate>> AggregateCompletedByDayAsync(
            PosOrganizationId organizationId,
            DateOnly fromDateUtc,
            DateOnly toDateUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Sale> CheckoutAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            Func<string, Sale> createSale,
            Func<Sale, CancellationToken, Task>? afterSaleCreated = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Sale sale, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(Sale sale, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<string> ReserveNextSaleNumberAsync(
            PosOrganizationId organizationId,
            DateOnly businessDateUtc,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> HasReturnsForSaleAsync(
            PosOrganizationId organizationId,
            SaleId saleId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
