using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Reporting;

public sealed record PosProfitabilityReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? BranchId,
    decimal NetSales,
    string CogsStatus,
    decimal KnownCogs,
    decimal? TotalCogs,
    decimal? GrossProfit,
    decimal? GrossMarginPercent,
    int CompletedSaleCount,
    int CompleteCostSaleCount,
    int PartialCostSaleCount,
    int UnavailableCostSaleCount,
    decimal WasteLossKnownCost,
    string WasteLossCostStatus,
    decimal StockUseKnownCost,
    string StockUseCostStatus,
    decimal CostCompletenessPercent);

/// <summary>
/// Period profitability from immutable sale COGS snapshots. Voided sales excluded from aggregates
/// (SALE_VOID_COGS_POLICY=EXCLUDE_VOIDED_FROM_ACTIVE_AGGREGATES). Waste/stock use shown separately.
/// </summary>
public sealed class ProfitabilityReportService(
    ISaleRepository sales,
    ISaleReturnRepository returns,
    IWasteLossRepository wasteLosses,
    IStockUseRepository stockUses,
    IClock clock)
{
    public async Task<ApplicationResult<PosProfitabilityReportDto>> GetAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId = null,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosProfitabilityReportDto>.Failure(
                rangeResult.ErrorCode!,
                rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);

        var saleTotals = await sales
            .AggregatePeriodAsync(
                org,
                range.FromDate,
                range.ToDate,
                branchId: branchId,
                cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var returnCogs = await returns
            .AggregateReturnCogsForPeriodAsync(org, range.FromDate, range.ToDate, branchId, cancellationToken)
            .ConfigureAwait(false);
        var saleCosts = await sales
            .AggregateCostForProfitabilityAsync(org, range.FromDate, range.ToDate, branchId, cancellationToken)
            .ConfigureAwait(false);
        var wasteCosts = await wasteLosses
            .AggregatePostedCostForPeriodAsync(org, range.FromDate, range.ToDate, branchId, cancellationToken)
            .ConfigureAwait(false);
        var stockUseCosts = await stockUses
            .AggregatePostedCostForPeriodAsync(org, range.FromDate, range.ToDate, branchId, cancellationToken)
            .ConfigureAwait(false);

        var refunds = await returns
            .SumRefundsForPeriodAsync(org, range.FromDate, range.ToDate, branchId, cancellationToken)
            .ConfigureAwait(false);

        var netSales = ReportMath.RoundMoney(saleTotals.CompletedTotal - saleTotals.VoidedTotal - refunds);
        var knownCogs = ReportMath.RoundMoney(
            Math.Max(0m, saleCosts.KnownCogsSum - returnCogs.KnownReturnCogs));

        var cogsStatus = DeriveSalePeriodCogsStatus(
            saleCosts,
            returnCogs.HasUnknownCostReturn);

        decimal? totalCogs = cogsStatus == ProductionCostStatus.Complete ? knownCogs : null;
        decimal? grossProfit = totalCogs is not null ? ReportMath.RoundMoney(netSales - totalCogs.Value) : null;
        decimal? grossMargin = grossProfit is not null && netSales > 0m
            ? ReportMath.RoundMoney(grossProfit.Value / netSales * 100m)
            : null;

        var completeness = saleCosts.CompletedCount == 0
            ? 0m
            : ReportMath.RoundMoney(
                (decimal)saleCosts.CompleteCostCount / saleCosts.CompletedCount * 100m);

        return ApplicationResult<PosProfitabilityReportDto>.Success(
            new PosProfitabilityReportDto(
                range.FromDate,
                range.ToDate,
                branchId,
                netSales,
                ProductionCostStatuses.ToCode(cogsStatus),
                knownCogs,
                totalCogs,
                grossProfit,
                grossMargin,
                saleCosts.CompletedCount,
                saleCosts.CompleteCostCount,
                saleCosts.PartialCostCount,
                saleCosts.UnavailableCostCount,
                wasteCosts.KnownCost,
                DeriveDocumentPeriodCostStatus(wasteCosts),
                stockUseCosts.KnownCost,
                DeriveDocumentPeriodCostStatus(stockUseCosts),
                completeness));
    }

    private static ProductionCostStatus DeriveSalePeriodCogsStatus(
        SaleCostPeriodAggregate saleCosts,
        bool hasUnknownCostReturn)
    {
        if (saleCosts.CompletedCount == 0)
        {
            return ProductionCostStatus.Unavailable;
        }

        if (hasUnknownCostReturn
            || saleCosts.PartialCostCount > 0
            || saleCosts.UnavailableCostCount > 0)
        {
            return saleCosts.CompleteCostCount > 0
                || saleCosts.PartialCostCount > 0
                ? ProductionCostStatus.Partial
                : ProductionCostStatus.Unavailable;
        }

        return saleCosts.CompleteCostCount == saleCosts.CompletedCount
            ? ProductionCostStatus.Complete
            : ProductionCostStatus.Partial;
    }

    private static string DeriveDocumentPeriodCostStatus(InventoryDocumentCostPeriodAggregate aggregate)
    {
        if (aggregate.PostedCount == 0)
        {
            return ProductionCostStatuses.ToCode(ProductionCostStatus.Unavailable);
        }

        if (aggregate.CompleteCostCount == aggregate.PostedCount)
        {
            return ProductionCostStatuses.ToCode(ProductionCostStatus.Complete);
        }

        if (aggregate.CompleteCostCount == 0 && aggregate.PartialCostCount == 0)
        {
            return ProductionCostStatuses.ToCode(ProductionCostStatus.Unavailable);
        }

        return ProductionCostStatuses.ToCode(ProductionCostStatus.Partial);
    }
}
