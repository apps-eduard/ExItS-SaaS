using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Reporting;

public static class ProductProfitabilityRankBy
{
    public const string GrossProfitDesc = "grossProfitDesc";
    public const string GrossProfitAsc = "grossProfitAsc";
    public const string NetSalesDesc = "netSalesDesc";
    public const string GrossMarginDesc = "grossMarginDesc";

    public static string Normalize(string? rankBy) =>
        rankBy?.Trim() switch
        {
            GrossProfitAsc => GrossProfitAsc,
            NetSalesDesc => NetSalesDesc,
            GrossMarginDesc => GrossMarginDesc,
            _ => GrossProfitDesc
        };
}

public sealed record PosProductProfitabilityRowDto(
    Guid ProductId,
    string ProductName,
    string? Sku,
    string UnitOfMeasure,
    decimal QuantitySold,
    decimal QuantityReturned,
    decimal NetQuantity,
    decimal SalesBeforeDiscounts,
    decimal CommercialDiscounts,
    decimal NetSales,
    decimal RefundAmount,
    decimal KnownCogs,
    string CogsStatus,
    decimal? GrossProfit,
    decimal? GrossMarginPercent,
    decimal CostCompletenessPercent);

public sealed record PosProductProfitabilityReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    Guid? BranchId,
    string RankBy,
    IReadOnlyList<PosProductProfitabilityRowDto> Rows);

/// <summary>
/// Per-product profitability ranking from immutable sale/return line snapshots.
/// Waste/Loss and Stock Use are never included. Gross Profit only when COGS is Complete.
/// </summary>
public sealed class ProductProfitabilityReportService(
    ISaleRepository sales,
    ISaleReturnRepository returns,
    IClock clock)
{
    public async Task<ApplicationResult<PosProductProfitabilityReportDto>> GetAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId = null,
        string? rankBy = null,
        CancellationToken cancellationToken = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosProductProfitabilityReportDto>.Failure(
                rangeResult.ErrorCode!,
                rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var rank = ProductProfitabilityRankBy.Normalize(rankBy);

        var saleRows = await sales
            .AggregateProductProfitabilitySalesAsync(
                org, range.FromDate, range.ToDate, branchId, cancellationToken)
            .ConfigureAwait(false);
        var returnRows = await returns
            .AggregateProductProfitabilityReturnsAsync(
                org, range.FromDate, range.ToDate, branchId, cancellationToken)
            .ConfigureAwait(false);

        var returnsByProduct = returnRows.ToDictionary(r => r.ProductId);

        var products = new HashSet<Guid>(saleRows.Select(s => s.ProductId));
        foreach (var ret in returnRows)
        {
            products.Add(ret.ProductId);
        }

        var saleByProduct = saleRows.ToDictionary(s => s.ProductId);
        var rows = new List<PosProductProfitabilityRowDto>(products.Count);

        foreach (var productId in products)
        {
            saleByProduct.TryGetValue(productId, out var sale);
            returnsByProduct.TryGetValue(productId, out var ret);

            var quantitySold = sale?.QuantitySold ?? 0m;
            var quantityReturned = ret?.QuantityReturned ?? 0m;
            var salesBefore = sale?.SalesBeforeDiscounts ?? 0m;
            var discounts = sale?.CommercialDiscounts ?? 0m;
            var netLineSales = sale?.NetLineSales ?? 0m;
            var refunds = ret?.RefundAmount ?? 0m;
            var netSales = ReportMath.RoundMoney(netLineSales - refunds);

            var knownSaleCogs = sale?.KnownCogsSum ?? 0m;
            var knownReturnCogs = ret?.KnownReturnCogs ?? 0m;
            var knownCogs = ReportMath.RoundMoney(Math.Max(0m, knownSaleCogs - knownReturnCogs));

            var knownQty = sale?.KnownCostQuantity ?? 0m;
            var unknownQty = sale?.UnknownCostQuantity ?? 0m;
            var hasUnknownReturn = ret?.HasUnknownCostReturn ?? false;

            var cogsStatus = DeriveProductCogsStatus(
                quantitySold,
                knownQty,
                unknownQty,
                hasUnknownReturn);

            decimal? grossProfit = null;
            decimal? grossMargin = null;
            if (cogsStatus == ProductionCostStatus.Complete)
            {
                grossProfit = ReportMath.RoundMoney(netSales - knownCogs);
                if (netSales > 0m)
                {
                    grossMargin = ReportMath.RoundMoney(grossProfit.Value / netSales * 100m);
                }
            }

            var completeness = quantitySold <= 0m
                ? (hasUnknownReturn ? 0m : 100m)
                : ReportMath.RoundMoney(knownQty / quantitySold * 100m);

            rows.Add(
                new PosProductProfitabilityRowDto(
                    productId,
                    sale?.ProductName ?? "Product",
                    sale?.Sku,
                    sale?.UnitOfMeasure ?? "Piece",
                    quantitySold,
                    quantityReturned,
                    quantitySold - quantityReturned,
                    salesBefore,
                    discounts,
                    netSales,
                    refunds,
                    knownCogs,
                    ProductionCostStatuses.ToCode(cogsStatus),
                    grossProfit,
                    grossMargin,
                    completeness));
        }

        var ordered = RankRows(rows, rank);

        return ApplicationResult<PosProductProfitabilityReportDto>.Success(
            new PosProductProfitabilityReportDto(
                range.FromDate,
                range.ToDate,
                branchId,
                rank,
                ordered));
    }

    private static ProductionCostStatus DeriveProductCogsStatus(
        decimal quantitySold,
        decimal knownCostQuantity,
        decimal unknownCostQuantity,
        bool hasUnknownReturnCost)
    {
        if (quantitySold <= 0m)
        {
            return ProductionCostStatus.Unavailable;
        }

        if (unknownCostQuantity <= 0m
            && !hasUnknownReturnCost
            && knownCostQuantity >= quantitySold)
        {
            return ProductionCostStatus.Complete;
        }

        if (knownCostQuantity > 0m)
        {
            return ProductionCostStatus.Partial;
        }

        return ProductionCostStatus.Unavailable;
    }

    private static IReadOnlyList<PosProductProfitabilityRowDto> RankRows(
        IReadOnlyList<PosProductProfitabilityRowDto> rows,
        string rankBy)
    {
        IEnumerable<PosProductProfitabilityRowDto> query = rows;
        query = rankBy switch
        {
            ProductProfitabilityRankBy.GrossProfitAsc => query
                .OrderBy(r => r.GrossProfit is null)
                .ThenBy(r => r.GrossProfit)
                .ThenBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase),
            ProductProfitabilityRankBy.NetSalesDesc => query
                .OrderByDescending(r => r.NetSales)
                .ThenBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase),
            ProductProfitabilityRankBy.GrossMarginDesc => query
                .OrderBy(r => r.GrossMarginPercent is null)
                .ThenByDescending(r => r.GrossMarginPercent)
                .ThenBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase),
            _ => query
                .OrderBy(r => r.GrossProfit is null)
                .ThenByDescending(r => r.GrossProfit)
                .ThenBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase)
        };

        return query.ToList();
    }
}
