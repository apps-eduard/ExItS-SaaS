using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Expenses;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Reporting;

internal static class ReportMath
{
    public static decimal RoundMoney(decimal amount) => SaleMoney.RoundMoney(amount);

    public static ReportPeriodComparisonDto Compare(decimal current, decimal prior, ReportDateRange priorRange)
    {
        var absolute = RoundMoney(current - prior);
        if (prior == 0m)
        {
            return new ReportPeriodComparisonDto(
                priorRange.FromDate,
                priorRange.ToDate,
                absolute,
                PercentageChange: null,
                PercentageAvailable: false);
        }

        var pct = Math.Round((current - prior) / prior * 100m, 2, MidpointRounding.AwayFromZero);
        return new ReportPeriodComparisonDto(
            priorRange.FromDate,
            priorRange.ToDate,
            absolute,
            pct,
            PercentageAvailable: true);
    }

    public static IReadOnlyList<ReportDailyAmountDto> FillDailySeries(
        ReportDateRange range,
        IEnumerable<(DateOnly Day, decimal Amount, int Count)> points)
    {
        var map = points
            .GroupBy(p => p.Day)
            .ToDictionary(
                g => g.Key,
                g => (Amount: RoundMoney(g.Sum(x => x.Amount)), Count: g.Sum(x => x.Count)));

        var series = new List<ReportDailyAmountDto>(range.InclusiveDayCount);
        for (var d = range.FromDate; d <= range.ToDate; d = d.AddDays(1))
        {
            if (map.TryGetValue(d, out var row))
            {
                series.Add(new ReportDailyAmountDto(d, row.Amount, row.Count));
            }
            else
            {
                series.Add(new ReportDailyAmountDto(d, 0m, 0));
            }
        }

        return series;
    }
}

public sealed class DashboardQueryService
{
    private readonly ISaleRepository _sales;
    private readonly IExpenseRepository _expenses;
    private readonly ICreditEntryRepository _credits;
    private readonly IRepaymentRepository _repayments;
    private readonly IInventoryRepository _inventory;
    private readonly IClock _clock;

    public DashboardQueryService(
        ISaleRepository sales,
        IExpenseRepository expenses,
        ICreditEntryRepository credits,
        IRepaymentRepository repayments,
        IInventoryRepository inventory,
        IClock clock)
    {
        _sales = sales;
        _expenses = expenses;
        _credits = credits;
        _repayments = repayments;
        _inventory = inventory;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosDashboardDto>> GetAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, _clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosDashboardDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var orgId = PosOrganizationId.From(organizationId);

        var saleTotals = await _sales
            .AggregatePeriodAsync(orgId, range.FromDate, range.ToDate, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var paymentBreakdownRows = await _sales
            .AggregateCompletedByPaymentAsync(orgId, range.FromDate, range.ToDate, cancellationToken)
            .ConfigureAwait(false);
        var salesDailyRows = await _sales
            .AggregateCompletedByDayAsync(orgId, range.FromDate, range.ToDate, cancellationToken)
            .ConfigureAwait(false);
        var expenses = await _expenses
            .ListForSummaryAsync(orgId, range.FromDate, range.ToDate, cancellationToken)
            .ConfigureAwait(false);

        var recordedExpenses = expenses.Where(e => e.Status == ExpenseStatus.Recorded).ToList();
        var voidedExpenses = expenses.Where(e => e.Status == ExpenseStatus.Voided).ToList();

        var completedTotal = saleTotals.CompletedTotal;
        var cashTotal = saleTotals.CashTotal;
        var gcashTotal = saleTotals.ManualGCashTotal;
        var utangTotal = saleTotals.UtangTotal;
        var expenseTotal = ReportMath.RoundMoney(recordedExpenses.Sum(e => e.Amount));

        var (outstanding, overdue) = await ComputeUtangTotalsAsync(orgId, cancellationToken).ConfigureAwait(false);

        var accounts = await _inventory.ListAllAccountsAsync(orgId, cancellationToken).ConfigureAwait(false);
        var lowStockCount = accounts.Count(a => a.IsLowStock);

        var salesByDay = ReportMath.FillDailySeries(
            range,
            salesDailyRows.Select(s => (s.Day, s.Amount, s.Count)));
        var expensesByDay = ReportMath.FillDailySeries(
            range,
            recordedExpenses.Select(e => (e.ExpenseDate, e.Amount, 1)));
        var salesCountByDay = ReportMath.FillDailySeries(
            range,
            salesDailyRows.Select(s => (s.Day, 0m, s.Count)));

        var paymentBreakdown = paymentBreakdownRows
            .OrderBy(g => g.PaymentMethod, StringComparer.Ordinal)
            .Select(g => new ReportPaymentBreakdownDto(g.PaymentMethod, g.Total, g.Count))
            .ToList();

        var prior = range.PrecedingEqualLengthPeriod();
        var priorSaleTotals = await _sales
            .AggregatePeriodAsync(orgId, prior.FromDate, prior.ToDate, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var priorExpenses = await _expenses
            .ListForSummaryAsync(orgId, prior.FromDate, prior.ToDate, cancellationToken)
            .ConfigureAwait(false);
        var priorCompletedTotal = priorSaleTotals.CompletedTotal;
        var priorExpenseTotal = ReportMath.RoundMoney(
            priorExpenses.Where(e => e.Status == ExpenseStatus.Recorded).Sum(e => e.Amount));

        return ApplicationResult<PosDashboardDto>.Success(
            new PosDashboardDto(
                range.FromDate,
                range.ToDate,
                completedTotal,
                saleTotals.CompletedCount,
                cashTotal,
                gcashTotal,
                utangTotal,
                outstanding,
                overdue,
                expenseTotal,
                lowStockCount,
                saleTotals.VoidedCount,
                voidedExpenses.Count,
                salesByDay,
                expensesByDay,
                paymentBreakdown,
                salesCountByDay,
                ReportMath.Compare(completedTotal, priorCompletedTotal, prior),
                ReportMath.Compare(expenseTotal, priorExpenseTotal, prior)));
    }

    private async Task<(decimal Outstanding, decimal Overdue)> ComputeUtangTotalsAsync(
        PosOrganizationId orgId,
        CancellationToken cancellationToken)
    {
        var credits = await _credits.ListActiveByOrganizationAsync(orgId, cancellationToken).ConfigureAwait(false);
        if (credits.Count == 0)
        {
            return (0m, 0m);
        }

        var repaymentTotals = await _repayments
            .SumActiveAmountsByOrganizationAsync(orgId, cancellationToken)
            .ConfigureAwait(false);
        var effective = CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow);
        decimal outstanding = 0m;
        decimal overdue = 0m;

        foreach (var group in credits.GroupBy(c => c.CustomerId.Value))
        {
            repaymentTotals.TryGetValue(group.Key, out var activeRepayments);
            var activeCredits = ReportMath.RoundMoney(group.Sum(c => c.Amount));
            var customerOutstanding = ReportMath.RoundMoney(activeCredits - activeRepayments);
            if (customerOutstanding > 0m)
            {
                outstanding = ReportMath.RoundMoney(outstanding + customerOutstanding);
            }

            var aged = CreditFifoAging.AgeCredits(group.ToList(), activeRepayments, effective);
            overdue = ReportMath.RoundMoney(overdue + aged.Where(a => a.IsOverdue).Sum(a => a.RemainingUnpaidAmount));
        }

        return (outstanding, overdue);
    }
}

public sealed class SalesReportService
{
    private readonly ISaleRepository _sales;
    private readonly ICatalogProductRepository _products;
    private readonly IProductCategoryRepository _categories;
    private readonly IClock _clock;

    public SalesReportService(
        ISaleRepository sales,
        ICatalogProductRepository products,
        IProductCategoryRepository categories,
        IClock clock)
    {
        _sales = sales;
        _products = products;
        _categories = categories;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosSalesReportDto>> GetAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        string? paymentMethod = null,
        string? status = null,
        Guid? productId = null,
        Guid? categoryId = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, _clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosSalesReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        SalePaymentMethod? parsedMethod = null;
        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            if (!SalePaymentMethods.TryParse(paymentMethod, out var method))
            {
                return ApplicationResult<PosSalesReportDto>.Failure(
                    ApplicationErrorCodes.DomainViolation,
                    "paymentMethod must be Cash, ManualGCash, or Utang.");
            }

            parsedMethod = method;
        }

        SaleStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<SaleStatus>(status.Trim(), ignoreCase: true, out var st)
                || (st is not SaleStatus.Completed and not SaleStatus.Voided))
            {
                return ApplicationResult<PosSalesReportDto>.Failure(
                    ApplicationErrorCodes.DomainViolation,
                    "status must be Completed or Voided.");
            }

            parsedStatus = st;
        }

        var range = rangeResult.Value!;
        var orgId = PosOrganizationId.From(organizationId);
        var sales = await _sales
            .ListForReportAsync(
                orgId,
                range.FromDate,
                range.ToDate,
                parsedStatus,
                parsedMethod,
                productId,
                customerId,
                cancellationToken)
            .ConfigureAwait(false);

        var productIds = sales.SelectMany(s => s.Lines).Select(l => l.ProductId).Distinct().ToList();
        var products = await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
        var productsById = products.ToDictionary(p => p.Id.Value);

        // Category filter uses current catalog assignment (labels only; line money/qty from snapshots).
        if (categoryId is not null)
        {
            sales = sales
                .Where(s => s.Lines.Any(l =>
                    productsById.TryGetValue(l.ProductId.Value, out var p)
                    && p.CategoryId is not null
                    && p.CategoryId.Value == categoryId.Value))
                .ToList();
        }

        var completed = sales.Where(s => s.Status == SaleStatus.Completed).ToList();
        var voided = sales.Where(s => s.Status == SaleStatus.Voided).ToList();
        var utang = completed.Where(s => s.PaymentMethod == SalePaymentMethod.Utang).ToList();

        var categoryIds = productsById.Values
            .Where(p => p.CategoryId is not null)
            .Select(p => p.CategoryId!)
            .Distinct()
            .ToList();
        var categories = await _categories.ListByIdsAsync(orgId, categoryIds, cancellationToken).ConfigureAwait(false);
        var categoryNames = categories.ToDictionary(c => c.Id.Value, c => c.Name);

        var completedLines = completed.SelectMany(s => s.Lines).ToList();
        if (categoryId is not null)
        {
            completedLines = completedLines
                .Where(l =>
                    productsById.TryGetValue(l.ProductId.Value, out var p)
                    && p.CategoryId is not null
                    && p.CategoryId.Value == categoryId.Value)
                .ToList();
        }

        if (productId is not null)
        {
            completedLines = completedLines.Where(l => l.ProductId.Value == productId.Value).ToList();
        }

        var byProduct = BuildProductRows(completedLines, productsById, categoryNames);
        var byCategory = byProduct
            .GroupBy(r => r.CategoryId)
            .Select(g => new ReportCategorySalesRowDto(
                g.Key,
                g.First().CategoryName ?? (g.Key is null ? "Uncategorized" : g.Key.Value.ToString("D")),
                g.Sum(x => x.Quantity),
                ReportMath.RoundMoney(g.Sum(x => x.SalesAmount)),
                g.Sum(x => x.LineCount)))
            .OrderByDescending(r => r.SalesAmount)
            .ThenBy(r => r.CategoryName, StringComparer.Ordinal)
            .ToList();

        var topQty = byProduct
            .OrderByDescending(r => r.Quantity)
            .ThenByDescending(r => r.SalesAmount)
            .ThenBy(r => r.NameSnapshot, StringComparer.Ordinal)
            .Take(10)
            .ToList();
        var topAmt = byProduct
            .OrderByDescending(r => r.SalesAmount)
            .ThenByDescending(r => r.Quantity)
            .ThenBy(r => r.NameSnapshot, StringComparer.Ordinal)
            .Take(10)
            .ToList();

        var byPayment = completed
            .GroupBy(s => SalePaymentMethods.ToCode(s.PaymentMethod))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new ReportPaymentBreakdownDto(
                g.Key,
                ReportMath.RoundMoney(g.Sum(s => s.Total)),
                g.Count()))
            .ToList();

        var byDay = ReportMath.FillDailySeries(
            range,
            completed.Select(s => (
                DateOnly.FromDateTime(s.RecordedAtUtc.UtcDateTime),
                s.Total,
                1)));

        return ApplicationResult<PosSalesReportDto>.Success(
            new PosSalesReportDto(
                range.FromDate,
                range.ToDate,
                ReportMath.RoundMoney(completed.Sum(s => s.Total)),
                completed.Count,
                ReportMath.RoundMoney(voided.Sum(s => s.Total)),
                voided.Count,
                ReportMath.RoundMoney(utang.Sum(s => s.Total)),
                utang.Count,
                byPayment,
                byProduct,
                byCategory,
                topQty,
                topAmt,
                byDay));
    }

    private static IReadOnlyList<ReportProductSalesRowDto> BuildProductRows(
        IReadOnlyList<SaleLine> lines,
        IReadOnlyDictionary<Guid, CatalogProduct> productsById,
        IReadOnlyDictionary<Guid, string> categoryNames)
    {
        return lines
            .GroupBy(l => l.ProductId.Value)
            .Select(g =>
            {
                var sample = g.First();
                Guid? catId = null;
                string? catName = null;
                if (productsById.TryGetValue(g.Key, out var product) && product.CategoryId is not null)
                {
                    catId = product.CategoryId.Value;
                    categoryNames.TryGetValue(catId.Value, out catName);
                }

                return new ReportProductSalesRowDto(
                    g.Key,
                    sample.NameSnapshot,
                    sample.SkuSnapshot,
                    UnitOfMeasures.ToCode(sample.UnitOfMeasureSnapshot),
                    SellingModes.ToCode(sample.SellingModeSnapshot),
                    g.Sum(l => l.Quantity),
                    ReportMath.RoundMoney(g.Sum(l => l.LineTotal)),
                    g.Count(),
                    catId,
                    catName);
            })
            .OrderByDescending(r => r.SalesAmount)
            .ThenBy(r => r.NameSnapshot, StringComparer.Ordinal)
            .ToList();
    }
}

public sealed class UtangReportService
{
    private readonly ICreditEntryRepository _credits;
    private readonly IRepaymentRepository _repayments;
    private readonly IPOSCustomerRepository _customers;
    private readonly ISaleRepository _sales;
    private readonly IClock _clock;

    public UtangReportService(
        ICreditEntryRepository credits,
        IRepaymentRepository repayments,
        IPOSCustomerRepository customers,
        ISaleRepository sales,
        IClock clock)
    {
        _credits = credits;
        _repayments = repayments;
        _customers = customers;
        _sales = sales;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosUtangReportDto>> GetAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, _clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosUtangReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var orgId = PosOrganizationId.From(organizationId);
        var effective = CreditFifoAging.EffectiveBusinessDateUtc(_clock.UtcNow);

        var activeCredits = await _credits.ListActiveByOrganizationAsync(orgId, cancellationToken).ConfigureAwait(false);
        if (customerId is not null)
        {
            activeCredits = activeCredits.Where(c => c.CustomerId.Value == customerId.Value).ToList();
        }

        var balances = new List<ReportCustomerBalanceRowDto>();
        decimal outstandingTotal = 0m;
        decimal overdueTotal = 0m;

        var customerIds = activeCredits.Select(c => c.CustomerId).Distinct().ToList();
        var customers = await _customers.ListByIdsAsync(orgId, customerIds, cancellationToken).ConfigureAwait(false);
        var customersById = customers.ToDictionary(c => c.Id.Value);
        var repaymentTotals = await _repayments
            .SumActiveAmountsByOrganizationAsync(orgId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var group in activeCredits.GroupBy(c => c.CustomerId.Value))
        {
            if (!customersById.TryGetValue(group.Key, out var customer))
            {
                continue;
            }

            repaymentTotals.TryGetValue(group.Key, out var activeRepayments);
            var activeCreditTotal = ReportMath.RoundMoney(group.Sum(c => c.Amount));
            var outstanding = ReportMath.RoundMoney(activeCreditTotal - activeRepayments);
            var aged = CreditFifoAging.AgeCredits(group.ToList(), activeRepayments, effective);
            var overdueRows = aged.Where(a => a.IsOverdue).ToList();
            var overdueAmount = ReportMath.RoundMoney(overdueRows.Sum(a => a.RemainingUnpaidAmount));

            if (outstanding > 0m)
            {
                outstandingTotal = ReportMath.RoundMoney(outstandingTotal + outstanding);
                balances.Add(
                    new ReportCustomerBalanceRowDto(
                        customer.Id.Value,
                        customer.DisplayName,
                        outstanding,
                        overdueAmount,
                        overdueRows.Count,
                        overdueRows
                            .Select(a => a.CurrentDueDate)
                            .Where(d => d is not null)
                            .Cast<DateOnly>()
                            .OrderBy(d => d)
                            .Cast<DateOnly?>()
                            .FirstOrDefault()));
            }

            overdueTotal = ReportMath.RoundMoney(overdueTotal + overdueAmount);
        }

        balances = balances
            .OrderByDescending(b => b.OutstandingAmount)
            .ThenBy(b => b.DisplayName, StringComparer.Ordinal)
            .ToList();
        var overdueCustomers = balances
            .Where(b => b.OverdueAmount > 0m)
            .OrderByDescending(b => b.OverdueAmount)
            .ThenBy(b => b.DisplayName, StringComparer.Ordinal)
            .ToList();

        var periodCredits = await _credits
            .ListRecordedInRangeAsync(orgId, range.FromDate, range.ToDate, cancellationToken)
            .ConfigureAwait(false);
        var periodRepayments = await _repayments
            .ListRecordedInRangeAsync(orgId, range.FromDate, range.ToDate, cancellationToken)
            .ConfigureAwait(false);
        if (customerId is not null)
        {
            periodCredits = periodCredits.Where(c => c.CustomerId.Value == customerId.Value).ToList();
            periodRepayments = periodRepayments.Where(r => r.CustomerId.Value == customerId.Value).ToList();
        }

        var activePeriodCredits = periodCredits.Where(c => c.Status == CreditEntryStatus.Active).ToList();
        var activePeriodRepayments = periodRepayments.Where(r => r.Status == RepaymentStatus.Active).ToList();

        var utangSales = await _sales
            .ListForReportAsync(
                orgId,
                range.FromDate,
                range.ToDate,
                SaleStatus.Completed,
                SalePaymentMethod.Utang,
                customerId: customerId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<PosUtangReportDto>.Success(
            new PosUtangReportDto(
                range.FromDate,
                range.ToDate,
                outstandingTotal,
                overdueTotal,
                balances.Count,
                overdueCustomers.Count,
                ReportMath.RoundMoney(activePeriodCredits.Sum(c => c.Amount)),
                activePeriodCredits.Count,
                ReportMath.RoundMoney(activePeriodRepayments.Sum(r => r.Amount)),
                activePeriodRepayments.Count,
                ReportMath.RoundMoney(utangSales.Sum(s => s.Total)),
                utangSales.Count,
                balances,
                overdueCustomers));
    }
}

public sealed class InventoryReportService
{
    private readonly IInventoryRepository _inventory;
    private readonly ICatalogProductRepository _products;
    private readonly IClock _clock;

    public InventoryReportService(
        IInventoryRepository inventory,
        ICatalogProductRepository products,
        IClock clock)
    {
        _inventory = inventory;
        _products = products;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosInventoryReportDto>> GetAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        bool? trackedOnly = true,
        bool? lowStockOnly = null,
        string? productStatus = null,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, _clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosInventoryReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var orgId = PosOrganizationId.From(organizationId);
        var accounts = await _inventory.ListAllAccountsAsync(orgId, cancellationToken).ConfigureAwait(false);
        var productIds = accounts.Select(a => a.ProductId).ToList();
        var products = await _products.ListByIdsAsync(orgId, productIds, cancellationToken).ConfigureAwait(false);
        var productsById = products.ToDictionary(p => p.Id.Value);
        var summaries = await _inventory
            .GetMovementSummariesAsync(orgId, productIds, cancellationToken)
            .ConfigureAwait(false);

        var rows = new List<ReportInventoryStatusRowDto>();
        foreach (var account in accounts)
        {
            if (!productsById.TryGetValue(account.ProductId.Value, out var product))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(productStatus)
                && !string.Equals(product.Status.ToString(), productStatus.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (trackedOnly == true && !account.IsTracked)
            {
                continue;
            }

            if (trackedOnly == false && account.IsTracked)
            {
                continue;
            }

            summaries.TryGetValue(account.ProductId.Value, out var summary);
            var isOut = account.IsTracked && account.OnHandQuantity <= 0m;
            var row = new ReportInventoryStatusRowDto(
                product.Id.Value,
                product.Name,
                product.Sku,
                account.IsTracked,
                account.OnHandQuantity,
                account.ReorderLevel,
                account.IsLowStock,
                isOut,
                summary.LatestAt);

            if (lowStockOnly == true && !row.IsLowStock)
            {
                continue;
            }

            rows.Add(row);
        }

        rows = rows
            .OrderBy(r => r.ProductName, StringComparer.Ordinal)
            .ThenBy(r => r.CatalogProductId)
            .ToList();

        var tracked = rows.Where(r => r.IsTracked).ToList();
        var low = tracked.Where(r => r.IsLowStock).ToList();
        var outOfStock = tracked.Where(r => r.IsOutOfStock).ToList();

        var movements = await _inventory
            .ListMovementsForReportAsync(orgId, range.FromDate, range.ToDate, cancellationToken)
            .ConfigureAwait(false);
        var byType = movements
            .GroupBy(m => StockMovementTypes.ToCode(m.MovementType))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new ReportMovementTypeTotalDto(
                g.Key,
                g.Sum(m => m.QuantityEffect),
                g.Count()))
            .ToList();

        DateTimeOffset? latest = null;
        if (movements.Count > 0)
        {
            latest = movements.Max(m => m.RecordedAtUtc);
        }
        else
        {
            var movementDates = tracked
                .Select(r => r.LatestMovementAtUtc)
                .Where(d => d is not null)
                .Select(d => d!.Value)
                .ToList();
            if (movementDates.Count > 0)
            {
                latest = movementDates.Max();
            }
        }

        return ApplicationResult<PosInventoryReportDto>.Success(
            new PosInventoryReportDto(
                range.FromDate,
                range.ToDate,
                tracked.Count,
                low.Count,
                outOfStock.Count,
                latest,
                byType,
                tracked,
                low,
                outOfStock));
    }
}

public sealed class ExpensesReportService
{
    private readonly IExpenseRepository _expenses;
    private readonly IExpenseCategoryRepository _categories;
    private readonly IClock _clock;

    public ExpensesReportService(
        IExpenseRepository expenses,
        IExpenseCategoryRepository categories,
        IClock clock)
    {
        _expenses = expenses;
        _categories = categories;
        _clock = clock;
    }

    public async Task<ApplicationResult<PosExpensesReportDto>> GetAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? expenseCategoryId = null,
        string? paymentMethod = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, _clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosExpensesReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        ExpensePaymentMethod? parsedMethod = null;
        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            if (!ExpensePaymentMethods.TryParse(paymentMethod, out var method))
            {
                return ApplicationResult<PosExpensesReportDto>.Failure(
                    ApplicationErrorCodes.DomainViolation,
                    "paymentMethod must be Cash or ManualGCash.");
            }

            parsedMethod = method;
        }

        ExpenseStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<ExpenseStatus>(status.Trim(), ignoreCase: true, out var st)
                || (st is not ExpenseStatus.Recorded and not ExpenseStatus.Voided))
            {
                return ApplicationResult<PosExpensesReportDto>.Failure(
                    ApplicationErrorCodes.DomainViolation,
                    "status must be Recorded or Voided.");
            }

            parsedStatus = st;
        }

        var range = rangeResult.Value!;
        var orgId = PosOrganizationId.From(organizationId);
        var expenses = (await _expenses
                .ListForSummaryAsync(orgId, range.FromDate, range.ToDate, cancellationToken)
                .ConfigureAwait(false))
            .AsEnumerable();

        if (expenseCategoryId is not null)
        {
            expenses = expenses.Where(e => e.CategoryId.Value == expenseCategoryId.Value);
        }

        if (parsedMethod is not null)
        {
            expenses = expenses.Where(e => e.PaymentMethod == parsedMethod.Value);
        }

        if (parsedStatus is not null)
        {
            expenses = expenses.Where(e => e.Status == parsedStatus.Value);
        }

        var list = expenses
            .OrderByDescending(e => e.ExpenseDate)
            .ThenByDescending(e => e.RecordedAtUtc)
            .ThenBy(e => e.ExpenseNumber, StringComparer.Ordinal)
            .ToList();

        var recorded = list.Where(e => e.Status == ExpenseStatus.Recorded).ToList();
        var voided = list.Where(e => e.Status == ExpenseStatus.Voided).ToList();

        var categoryIds = list.Select(e => e.CategoryId).Distinct().ToList();
        var categories = await _categories.ListByIdsAsync(orgId, categoryIds, cancellationToken).ConfigureAwait(false);
        var categoryNames = categories.ToDictionary(c => c.Id.Value, c => (string?)c.Name);
        foreach (var id in categoryIds)
        {
            categoryNames.TryAdd(id.Value, null);
        }

        var byCategory = recorded
            .GroupBy(e => e.CategoryId.Value)
            .Select(g => new ExpenseCategoryReportRowDto(
                g.Key,
                categoryNames.GetValueOrDefault(g.Key),
                ExpenseMoney.RoundMoney(g.Sum(e => e.Amount)),
                g.Count()))
            .OrderByDescending(r => r.TotalAmount)
            .ThenBy(r => r.CategoryName, StringComparer.Ordinal)
            .ToList();

        var byPayment = recorded
            .GroupBy(e => ExpensePaymentMethods.ToCode(e.PaymentMethod))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new ReportPaymentBreakdownDto(
                g.Key,
                ExpenseMoney.RoundMoney(g.Sum(e => e.Amount)),
                g.Count()))
            .ToList();

        var byDay = ReportMath.FillDailySeries(
            range,
            recorded.Select(e => (e.ExpenseDate, e.Amount, 1)));

        var details = list
            .Select(e => new ReportExpenseDetailRowDto(
                e.Id.Value,
                e.ExpenseNumber,
                e.CategoryId.Value,
                categoryNames.GetValueOrDefault(e.CategoryId.Value),
                e.Status.ToString(),
                ExpensePaymentMethods.ToCode(e.PaymentMethod),
                e.Amount,
                e.Description,
                e.Payee,
                e.ExpenseDate,
                e.RecordedAtUtc))
            .ToList();

        return ApplicationResult<PosExpensesReportDto>.Success(
            new PosExpensesReportDto(
                range.FromDate,
                range.ToDate,
                ExpenseMoney.RoundMoney(recorded.Sum(e => e.Amount)),
                ExpenseMoney.RoundMoney(voided.Sum(e => e.Amount)),
                recorded.Count,
                voided.Count,
                byPayment,
                byCategory,
                byDay,
                details));
    }
}
