using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Reporting;

public sealed record PosOperationalOverviewDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal CompletedGrossSales,
    decimal VoidedSales,
    decimal Refunds,
    decimal NetSales,
    int CompletedTransactionCount,
    decimal AverageTransactionValue,
    decimal PreDiscountGrossSales = 0m,
    decimal CommercialDiscountTotal = 0m,
    decimal NetSubtotal = 0m,
    decimal TaxAmount = 0m);

public sealed record PosSalesSummaryReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal CompletedGrossSales,
    decimal VoidedSales,
    decimal CompletedReturnsRefunds,
    decimal NetSales,
    int CompletedTransactionCount,
    decimal AverageTransactionValue,
    decimal PreDiscountGrossSales = 0m,
    decimal CommercialDiscountTotal = 0m,
    decimal NetSubtotal = 0m,
    decimal TaxAmount = 0m);

public sealed record PosSalesByCashierRowDto(
    Guid CashierActorId,
    int CompletedTransactionCount,
    decimal CashCollected,
    decimal GrossSales,
    decimal VoidedSales,
    decimal NetSales,
    decimal PreDiscountGrossSales = 0m,
    decimal CommercialDiscountTotal = 0m);

public sealed record PosSalesByCashierReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<PosSalesByCashierRowDto> Rows);

public sealed record PosPaymentMethodBreakdownDto(
    string PaymentMethod,
    decimal GrossCompleted,
    decimal Voided,
    decimal Refunded,
    decimal Net,
    decimal PreDiscountGross = 0m,
    decimal CommercialDiscountTotal = 0m);

public sealed record PosSalesByPaymentReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<PosPaymentMethodBreakdownDto> Rows);

public sealed record PosSalesByProductRowDto(
    Guid ProductId,
    string ProductName,
    string UnitOfMeasure,
    string SellingMode,
    decimal QuantitySold,
    decimal QuantityReturned,
    decimal NetQuantity,
    decimal GrossSaleAmount,
    decimal RefundAmount,
    decimal NetAmount,
    decimal PreDiscountGrossSaleAmount = 0m,
    decimal CommercialDiscountAmount = 0m);

public sealed record PosSalesByProductReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<PosSalesByProductRowDto> Rows);

public sealed record PosReturnsReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int ReturnCount,
    decimal ReturnedQuantity,
    decimal RefundAmount,
    IReadOnlyList<PosReturnMethodBreakdownDto> ByRefundMethod,
    IReadOnlyList<PosReturnReasonBreakdownDto> ByReason);

public sealed record PosReturnMethodBreakdownDto(string RefundMethod, int Count, decimal Amount, decimal RestockedQty, decimal NotRestockedQty);

public sealed record PosReturnReasonBreakdownDto(string Reason, int Count, decimal Amount);

public sealed record PosShiftSummaryRowDto(
    Guid ShiftId,
    string ShiftNumber,
    Guid ActorId,
    string Status,
    DateOnly BusinessDate,
    decimal OpeningCashAmount,
    bool OpeningCashCounted,
    string EffectiveCashCountMode,
    decimal? ClosingCashAmount,
    decimal? ExpectedCashAmount,
    decimal? CashVarianceAmount,
    string? CashCountState,
    decimal NetCashSales,
    decimal CashRefundsTotal);

public sealed record PosShiftSummaryReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int ShiftCount,
    decimal TotalCashVariance,
    IReadOnlyList<PosShiftSummaryRowDto> Rows);

public sealed record PosCashVarianceReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int ClosedShiftCount,
    decimal TotalAbsoluteVariance,
    decimal TotalSignedVariance,
    IReadOnlyList<PosShiftSummaryRowDto> Rows);

public sealed record PosInventoryStatusReportDto(
    DateOnly AsOfDate,
    int TrackedCount,
    int LowStockCount,
    int OutOfStockCount,
    IReadOnlyList<ReportInventoryStatusRowDto> Rows);

public sealed record PosInventoryMovementsReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int MovementCount,
    IReadOnlyList<ReportMovementTypeTotalDto> ByType,
    IReadOnlyList<PosInventoryMovementRowDto> Rows);

public sealed record PosInventoryMovementRowDto(
    Guid MovementId,
    Guid ProductId,
    string MovementType,
    string SourceType,
    decimal QuantityEffect,
    DateTimeOffset RecordedAtUtc,
    Guid RecordedBy);

public sealed record PosStockCountVarianceRowDto(
    Guid StockCountId,
    string? CountNumber,
    DateOnly CountDate,
    Guid ProductId,
    string? ProductName,
    decimal SystemOnHand,
    decimal CountedQuantity,
    decimal Variance);

public sealed record PosStockCountVarianceReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int CompletedCount,
    int VarianceLineCount,
    IReadOnlyList<PosStockCountVarianceRowDto> Rows);

public sealed record PosPurchasingSummaryReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int OrderCount,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal OutstandingQuantity,
    IReadOnlyList<PosPurchasingStatusBreakdownDto> ByStatus);

public sealed record PosPurchasingStatusBreakdownDto(string Status, int Count, decimal OrderedQty, decimal ReceivedQty, decimal OutstandingQty);

public sealed record PosPurchaseOutstandingRowDto(
    Guid PurchaseOrderId,
    string? PoNumber,
    Guid SupplierId,
    string? SupplierName,
    string Status,
    DateOnly OrderDate,
    decimal OutstandingQuantity);

public sealed record PosPurchaseOutstandingReportDto(
    DateOnly AsOfDate,
    int OutstandingOrderCount,
    decimal OutstandingQuantity,
    IReadOnlyList<PosPurchaseOutstandingRowDto> Rows);

public sealed record PosSupplierPurchasingRowDto(
    Guid SupplierId,
    string? SupplierName,
    int OrderCount,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal OutstandingQuantity);

public sealed record PosSupplierPurchasingReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<PosSupplierPurchasingRowDto> Rows);

public sealed record PosExpenseSummaryReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal RecordedTotal,
    decimal VoidedTotal,
    int RecordedCount,
    int VoidedCount,
    IReadOnlyList<ExpenseCategoryReportRowDto> ByCategory,
    IReadOnlyList<ReportPaymentBreakdownDto> ByPaymentMethod);

public sealed record PosProductUtangSummaryReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal UtangSalesTotal,
    int UtangSaleCount,
    decimal OutstandingTotal,
    decimal OverdueTotal,
    IReadOnlyList<PosSalesByProductRowDto> ByProduct);

public sealed class OperationalReportService(
    ISaleRepository sales,
    ISaleReturnRepository returns,
    ICashierShiftRepository shifts,
    IInventoryRepository inventory,
    IStockCountRepository stockCounts,
    ICatalogProductRepository products,
    IPurchaseOrderRepository purchaseOrders,
    ISupplierRepository suppliers,
    IExpenseRepository expenses,
    IExpenseCategoryRepository expenseCategories,
    UtangReportService utangReports,
    IClock clock)
{
    public async Task<ApplicationResult<PosSalesSummaryReportDto>> GetSalesSummaryAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosSalesSummaryReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var saleTotals = await sales
            .AggregatePeriodAsync(org, range.FromDate, range.ToDate, branchId: branchId, cancellationToken: ct)
            .ConfigureAwait(false);
        var returnRows = await ListReturnsInRangeAsync(org, range, branchId, ct).ConfigureAwait(false);

        var completedGross = saleTotals.CompletedTotal;
        var voidedTotal = saleTotals.VoidedTotal;
        var refunds = ReportMath.RoundMoney(returnRows.Sum(r => r.TotalRefundAmount));
        // CompletedTotal already excludes voided sales — do not subtract VoidedTotal again.
        // Discounts are already reflected in Sale.Total; never subtract DiscountTotal again.
        var net = ReportMath.RoundMoney(completedGross - refunds);
        var count = saleTotals.CompletedCount;
        var avg = count == 0 ? 0m : ReportMath.RoundMoney(completedGross / count);

        return ApplicationResult<PosSalesSummaryReportDto>.Success(
            new PosSalesSummaryReportDto(
                range.FromDate,
                range.ToDate,
                completedGross,
                voidedTotal,
                refunds,
                net,
                count,
                avg,
                saleTotals.CompletedGrossSubtotal,
                saleTotals.CompletedDiscountTotal,
                saleTotals.CompletedNetSubtotal,
                saleTotals.CompletedTaxAmount));
    }

    public async Task<ApplicationResult<PosSalesByCashierReportDto>> GetSalesByCashierAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosSalesByCashierReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var saleRows = await sales
            .ListForReportAsync(org, range.FromDate, range.ToDate, branchId: branchId, cancellationToken: ct)
            .ConfigureAwait(false);

        var rows = saleRows
            .GroupBy(s => s.RecordedBy)
            .Select(g =>
            {
                var completed = g.Where(s => s.Status == SaleStatus.Completed).ToList();
                var voided = g.Where(s => s.Status == SaleStatus.Voided).ToList();
                var gross = ReportMath.RoundMoney(completed.Sum(s => s.Total));
                var voidedTotal = ReportMath.RoundMoney(voided.Sum(s => s.Total));
                var cash = ReportMath.RoundMoney(
                    completed
                        .Where(s => s.PaymentMethod == SalePaymentMethod.Cash)
                        .Sum(s => s.Total));
                var preDiscount = ReportMath.RoundMoney(completed.Sum(s => s.GrossSubtotal));
                var discount = ReportMath.RoundMoney(completed.Sum(s => s.DiscountTotal));
                return new PosSalesByCashierRowDto(
                    g.Key,
                    completed.Count,
                    cash,
                    gross,
                    voidedTotal,
                    ReportMath.RoundMoney(gross),
                    preDiscount,
                    discount);
            })
            .OrderByDescending(r => r.GrossSales)
            .ThenBy(r => r.CashierActorId)
            .ToList();

        return ApplicationResult<PosSalesByCashierReportDto>.Success(
            new PosSalesByCashierReportDto(range.FromDate, range.ToDate, rows));
    }

    public async Task<ApplicationResult<PosSalesByPaymentReportDto>> GetSalesByPaymentAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosSalesByPaymentReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var paymentAgg = await sales
            .AggregateCompletedByPaymentAsync(org, range.FromDate, range.ToDate, branchId, ct)
            .ConfigureAwait(false);
        var returnRows = await ListReturnsInRangeAsync(org, range, branchId, ct).ConfigureAwait(false);

        var completedByMethod = paymentAgg.ToDictionary(p => p.PaymentMethod, StringComparer.Ordinal);
        var methods = new[] { SalePaymentMethod.Cash, SalePaymentMethod.ManualGCash, SalePaymentMethod.Utang };
        var rows = new List<PosPaymentMethodBreakdownDto>();
        foreach (var method in methods)
        {
            var code = SalePaymentMethods.ToCode(method);
            completedByMethod.TryGetValue(code, out var completedRow);
            var completed = completedRow?.Total ?? 0m;
            var preDiscount = completedRow?.GrossSubtotal ?? 0m;
            var discount = completedRow?.DiscountTotal ?? 0m;
            var voidedSales = await sales
                .AggregatePeriodAsync(
                    org,
                    range.FromDate,
                    range.ToDate,
                    SaleStatus.Voided,
                    method,
                    branchId: branchId,
                    cancellationToken: ct)
                .ConfigureAwait(false);
            var voided = voidedSales.VoidedTotal;
            var refunded = returnRows.Where(r => r.RefundMethod == method).Sum(r => r.TotalRefundAmount);
            rows.Add(
                new PosPaymentMethodBreakdownDto(
                    code,
                    completed,
                    ReportMath.RoundMoney(voided),
                    ReportMath.RoundMoney(refunded),
                    ReportMath.RoundMoney(completed - refunded),
                    ReportMath.RoundMoney(preDiscount),
                    ReportMath.RoundMoney(discount)));
        }

        return ApplicationResult<PosSalesByPaymentReportDto>.Success(
            new PosSalesByPaymentReportDto(range.FromDate, range.ToDate, rows));
    }

    public async Task<ApplicationResult<PosSalesByProductReportDto>> GetSalesByProductAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? productId,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosSalesByProductReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var saleRows = await sales.ListForReportAsync(
                org,
                range.FromDate,
                range.ToDate,
                status: SaleStatus.Completed,
                productId: productId,
                branchId: branchId,
                cancellationToken: ct)
            .ConfigureAwait(false);
        var returnRows = await ListReturnsInRangeAsync(org, range, branchId, ct).ConfigureAwait(false);

        var sold = new Dictionary<Guid, PosSalesByProductRowDto>();
        foreach (var sale in saleRows)
        {
            foreach (var line in sale.Lines)
            {
                if (productId is Guid pid && line.ProductId.Value != pid)
                {
                    continue;
                }

                sold.TryGetValue(line.ProductId.Value, out var existing);
                existing ??= new PosSalesByProductRowDto(
                    line.ProductId.Value,
                    line.NameSnapshot,
                    UnitOfMeasures.ToCode(line.UnitOfMeasureSnapshot),
                    SellingModes.ToCode(line.SellingModeSnapshot),
                    0m, 0m, 0m, 0m, 0m, 0m);
                sold[line.ProductId.Value] = existing with
                {
                    QuantitySold = existing.QuantitySold + line.Quantity,
                    GrossSaleAmount = ReportMath.RoundMoney(existing.GrossSaleAmount + line.LineTotal),
                    PreDiscountGrossSaleAmount = ReportMath.RoundMoney(
                        existing.PreDiscountGrossSaleAmount + line.GrossLineTotal),
                    CommercialDiscountAmount = ReportMath.RoundMoney(
                        existing.CommercialDiscountAmount + line.TotalLineDiscount)
                };
            }
        }

        foreach (var ret in returnRows)
        {
            foreach (var line in ret.Lines)
            {
                if (productId is Guid pid && line.ProductId.Value != pid)
                {
                    continue;
                }

                sold.TryGetValue(line.ProductId.Value, out var existing);
                existing ??= new PosSalesByProductRowDto(
                    line.ProductId.Value,
                    line.ProductNameSnapshot,
                    UnitOfMeasures.ToCode(line.UomSnapshot),
                    nameof(SellingMode.PerItem),
                    0m, 0m, 0m, 0m, 0m, 0m);
                sold[line.ProductId.Value] = existing with
                {
                    QuantityReturned = existing.QuantityReturned + line.QuantityReturned,
                    RefundAmount = ReportMath.RoundMoney(existing.RefundAmount + line.RefundAmount)
                };
            }
        }

        var rows = sold.Values
            .Select(r => r with
            {
                NetQuantity = r.QuantitySold - r.QuantityReturned,
                NetAmount = ReportMath.RoundMoney(r.GrossSaleAmount - r.RefundAmount)
            })
            .OrderBy(r => r.ProductName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ApplicationResult<PosSalesByProductReportDto>.Success(
            new PosSalesByProductReportDto(range.FromDate, range.ToDate, rows));
    }

    public async Task<ApplicationResult<PosReturnsReportDto>> GetReturnsAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosReturnsReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var returnRows = await ListReturnsInRangeAsync(org, range, branchId, ct).ConfigureAwait(false);

        var byMethod = returnRows
            .GroupBy(r => SalePaymentMethods.ToCode(r.RefundMethod))
            .Select(g => new PosReturnMethodBreakdownDto(
                g.Key,
                g.Count(),
                ReportMath.RoundMoney(g.Sum(x => x.TotalRefundAmount)),
                g.SelectMany(x => x.Lines).Where(l => l.RestockDisposition == Domain.Returns.RestockDisposition.ReturnToStock)
                    .Sum(l => l.QuantityReturned),
                g.SelectMany(x => x.Lines).Where(l => l.RestockDisposition == Domain.Returns.RestockDisposition.DoNotRestock)
                    .Sum(l => l.QuantityReturned)))
            .OrderBy(r => r.RefundMethod, StringComparer.Ordinal)
            .ToList();

        var byReason = returnRows
            .GroupBy(r => r.Reason)
            .Select(g => new PosReturnReasonBreakdownDto(
                g.Key,
                g.Count(),
                ReportMath.RoundMoney(g.Sum(x => x.TotalRefundAmount))))
            .OrderByDescending(r => r.Amount)
            .ThenBy(r => r.Reason, StringComparer.Ordinal)
            .ToList();

        return ApplicationResult<PosReturnsReportDto>.Success(
            new PosReturnsReportDto(
                range.FromDate,
                range.ToDate,
                returnRows.Count,
                returnRows.SelectMany(r => r.Lines).Sum(l => l.QuantityReturned),
                ReportMath.RoundMoney(returnRows.Sum(r => r.TotalRefundAmount)),
                byMethod,
                byReason));
    }

    public async Task<ApplicationResult<PosOperationalOverviewDto>> GetOverviewAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        var summary = await GetSalesSummaryAsync(organizationId, fromDate, toDate, branchId, ct).ConfigureAwait(false);
        if (!summary.IsSuccess)
        {
            return ApplicationResult<PosOperationalOverviewDto>.Failure(summary.ErrorCode!, summary.ErrorMessage!);
        }

        var s = summary.Value!;
        return ApplicationResult<PosOperationalOverviewDto>.Success(
            new PosOperationalOverviewDto(
                s.FromDate,
                s.ToDate,
                s.CompletedGrossSales,
                s.VoidedSales,
                s.CompletedReturnsRefunds,
                s.NetSales,
                s.CompletedTransactionCount,
                s.AverageTransactionValue,
                s.PreDiscountGrossSales,
                s.CommercialDiscountTotal,
                s.NetSubtotal,
                s.TaxAmount));
    }

    public async Task<ApplicationResult<PosShiftSummaryReportDto>> GetShiftSummaryAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? restrictToActorId,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosShiftSummaryReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var (items, _) = await shifts.ListAsync(
                org,
                new CashierShiftFilter(
                    ActorId: restrictToActorId,
                    FromBusinessDate: range.FromDate,
                    ToBusinessDate: range.ToDate),
                0,
                5_000,
                ct)
            .ConfigureAwait(false);

        var rows = new List<PosShiftSummaryRowDto>(items.Count);
        foreach (var shift in items.OrderByDescending(s => s.BusinessDate).ThenByDescending(s => s.OpenedAtUtc))
        {
            var totals = await shifts.GetSalesTotalsAsync(org, shift.Id, ct).ConfigureAwait(false);
            rows.Add(ToShiftRow(shift, totals));
        }

        return ApplicationResult<PosShiftSummaryReportDto>.Success(
            new PosShiftSummaryReportDto(
                range.FromDate,
                range.ToDate,
                rows.Count,
                ReportMath.RoundMoney(rows.Where(r => r.CashVarianceAmount is not null).Sum(r => r.CashVarianceAmount!.Value)),
                rows));
    }

    public async Task<ApplicationResult<PosCashVarianceReportDto>> GetCashVarianceAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? restrictToActorId,
        CancellationToken ct = default)
    {
        var summary = await GetShiftSummaryAsync(organizationId, fromDate, toDate, restrictToActorId, ct)
            .ConfigureAwait(false);
        if (!summary.IsSuccess)
        {
            return ApplicationResult<PosCashVarianceReportDto>.Failure(summary.ErrorCode!, summary.ErrorMessage!);
        }

        var closed = summary.Value!.Rows
            .Where(r => string.Equals(r.Status, nameof(CashierShiftStatus.Closed), StringComparison.Ordinal)
                        && r.CashVarianceAmount is not null)
            .ToList();
        var signed = ReportMath.RoundMoney(closed.Sum(r => r.CashVarianceAmount!.Value));
        var absolute = ReportMath.RoundMoney(closed.Sum(r => Math.Abs(r.CashVarianceAmount!.Value)));

        return ApplicationResult<PosCashVarianceReportDto>.Success(
            new PosCashVarianceReportDto(
                summary.Value.FromDate,
                summary.Value.ToDate,
                closed.Count,
                absolute,
                signed,
                closed));
    }

    public async Task<ApplicationResult<PosInventoryStatusReportDto>> GetInventoryStatusAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var org = PosOrganizationId.From(organizationId);
        var accounts = await inventory.ListAllAccountsAsync(org, ct).ConfigureAwait(false);
        var productIds = accounts.Select(a => a.ProductId).ToList();
        var catalog = await products.ListByIdsAsync(org, productIds, ct).ConfigureAwait(false);
        var byId = catalog.ToDictionary(p => p.Id.Value);
        var summaries = await inventory.GetMovementSummariesAsync(org, productIds, ct).ConfigureAwait(false);

        var rows = new List<ReportInventoryStatusRowDto>();
        foreach (var account in accounts.Where(a => a.IsTracked))
        {
            if (!byId.TryGetValue(account.ProductId.Value, out var product))
            {
                continue;
            }

            summaries.TryGetValue(account.ProductId.Value, out var summary);
            rows.Add(new ReportInventoryStatusRowDto(
                product.Id.Value,
                product.Name,
                product.Sku,
                account.IsTracked,
                account.OnHandQuantity,
                account.ReorderLevel,
                account.IsLowStock,
                account.OnHandQuantity <= 0m,
                summary.LatestAt));
        }

        rows = rows.OrderBy(r => r.ProductName, StringComparer.Ordinal).ToList();
        return ApplicationResult<PosInventoryStatusReportDto>.Success(
            new PosInventoryStatusReportDto(
                DateOnly.FromDateTime(clock.UtcNow.UtcDateTime),
                rows.Count,
                rows.Count(r => r.IsLowStock),
                rows.Count(r => r.IsOutOfStock),
                rows));
    }

    public async Task<ApplicationResult<PosInventoryMovementsReportDto>> GetInventoryMovementsAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        Guid? branchId = null,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosInventoryMovementsReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var movements = await inventory
            .ListMovementsForReportAsync(org, range.FromDate, range.ToDate, branchId, ct)
            .ConfigureAwait(false);

        var byType = movements
            .GroupBy(m => StockMovementTypes.ToCode(m.MovementType))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new ReportMovementTypeTotalDto(g.Key, g.Sum(m => m.QuantityEffect), g.Count()))
            .ToList();

        var rows = movements
            .OrderByDescending(m => m.RecordedAtUtc)
            .Select(m => new PosInventoryMovementRowDto(
                m.Id.Value,
                m.ProductId.Value,
                StockMovementTypes.ToCode(m.MovementType),
                StockMovementSourceTypes.ToCode(m.SourceType),
                m.QuantityEffect,
                m.RecordedAtUtc,
                m.RecordedBy))
            .ToList();

        return ApplicationResult<PosInventoryMovementsReportDto>.Success(
            new PosInventoryMovementsReportDto(range.FromDate, range.ToDate, rows.Count, byType, rows));
    }

    public async Task<ApplicationResult<PosStockCountVarianceReportDto>> GetStockCountVarianceAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosStockCountVarianceReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var (counts, _) = await stockCounts
            .ListAsync(org, new StockCountFilter(Status: nameof(StockCountStatus.Completed)), 0, 5_000, ct)
            .ConfigureAwait(false);

        var inRange = counts
            .Where(c => c.CountDate >= range.FromDate && c.CountDate <= range.ToDate)
            .ToList();

        var productIds = inRange.SelectMany(c => c.Lines.Select(l => l.ProductId)).Distinct().ToList();
        var catalog = await products.ListByIdsAsync(org, productIds, ct).ConfigureAwait(false);
        var names = catalog.ToDictionary(p => p.Id.Value, p => p.Name);

        var rows = new List<PosStockCountVarianceRowDto>();
        foreach (var count in inRange)
        {
            foreach (var line in count.Lines)
            {
                if (line.Variance is null or 0m
                    || line.SystemOnHandSnapshot is null
                    || line.CountedQuantity is null)
                {
                    continue;
                }

                names.TryGetValue(line.ProductId.Value, out var name);
                rows.Add(new PosStockCountVarianceRowDto(
                    count.Id.Value,
                    count.CountNumber,
                    count.CountDate,
                    line.ProductId.Value,
                    name,
                    line.SystemOnHandSnapshot.Value,
                    line.CountedQuantity.Value,
                    line.Variance.Value));
            }
        }

        rows = rows
            .OrderByDescending(r => Math.Abs(r.Variance))
            .ThenBy(r => r.CountDate)
            .ToList();

        return ApplicationResult<PosStockCountVarianceReportDto>.Success(
            new PosStockCountVarianceReportDto(
                range.FromDate,
                range.ToDate,
                inRange.Count,
                rows.Count,
                rows));
    }

    public async Task<ApplicationResult<PosPurchasingSummaryReportDto>> GetPurchasingSummaryAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosPurchasingSummaryReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var (orders, _) = await purchaseOrders
            .ListAsync(
                org,
                new PurchaseOrderFilter(FromOrderDate: range.FromDate, ToOrderDate: range.ToDate),
                0,
                5_000,
                ct)
            .ConfigureAwait(false);

        var byStatus = orders
            .GroupBy(o => o.Status.ToString())
            .Select(g => new PosPurchasingStatusBreakdownDto(
                g.Key,
                g.Count(),
                g.SelectMany(o => o.Lines).Sum(l => l.OrderedQty),
                g.SelectMany(o => o.Lines).Sum(l => l.ReceivedQty),
                g.SelectMany(o => o.Lines).Sum(l => l.OutstandingQty)))
            .OrderBy(r => r.Status, StringComparer.Ordinal)
            .ToList();

        return ApplicationResult<PosPurchasingSummaryReportDto>.Success(
            new PosPurchasingSummaryReportDto(
                range.FromDate,
                range.ToDate,
                orders.Count,
                orders.SelectMany(o => o.Lines).Sum(l => l.OrderedQty),
                orders.SelectMany(o => o.Lines).Sum(l => l.ReceivedQty),
                orders.SelectMany(o => o.Lines).Sum(l => l.OutstandingQty),
                byStatus));
    }

    public async Task<ApplicationResult<PosPurchaseOutstandingReportDto>> GetPurchaseOutstandingAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var org = PosOrganizationId.From(organizationId);
        var (orders, _) = await purchaseOrders
            .ListAsync(org, new PurchaseOrderFilter(), 0, 5_000, ct)
            .ConfigureAwait(false);

        var outstanding = orders
            .Where(o => o.Status is PurchaseOrderStatus.Ordered or PurchaseOrderStatus.PartiallyReceived)
            .Where(o => o.Lines.Any(l => l.OutstandingQty > 0m))
            .ToList();

        var supplierIds = outstanding.Select(o => o.SupplierId).Distinct().ToList();
        var supplierNames = new Dictionary<Guid, string?>();
        foreach (var id in supplierIds)
        {
            var supplier = await suppliers.GetByIdAsync(org, id, ct).ConfigureAwait(false);
            supplierNames[id.Value] = supplier?.Name;
        }

        var rows = outstanding
            .Select(o => new PosPurchaseOutstandingRowDto(
                o.Id.Value,
                o.PoNumber,
                o.SupplierId.Value,
                supplierNames.GetValueOrDefault(o.SupplierId.Value),
                o.Status.ToString(),
                o.OrderDate,
                o.Lines.Sum(l => l.OutstandingQty)))
            .OrderByDescending(r => r.OutstandingQuantity)
            .ThenBy(r => r.OrderDate)
            .ToList();

        return ApplicationResult<PosPurchaseOutstandingReportDto>.Success(
            new PosPurchaseOutstandingReportDto(
                DateOnly.FromDateTime(clock.UtcNow.UtcDateTime),
                rows.Count,
                rows.Sum(r => r.OutstandingQuantity),
                rows));
    }

    public async Task<ApplicationResult<PosSupplierPurchasingReportDto>> GetSupplierPurchasingAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosSupplierPurchasingReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var (orders, _) = await purchaseOrders
            .ListAsync(
                org,
                new PurchaseOrderFilter(FromOrderDate: range.FromDate, ToOrderDate: range.ToDate),
                0,
                5_000,
                ct)
            .ConfigureAwait(false);

        var supplierIds = orders.Select(o => o.SupplierId).Distinct().ToList();
        var names = new Dictionary<Guid, string?>();
        foreach (var id in supplierIds)
        {
            var supplier = await suppliers.GetByIdAsync(org, id, ct).ConfigureAwait(false);
            names[id.Value] = supplier?.Name;
        }

        var rows = orders
            .GroupBy(o => o.SupplierId.Value)
            .Select(g => new PosSupplierPurchasingRowDto(
                g.Key,
                names.GetValueOrDefault(g.Key),
                g.Count(),
                g.SelectMany(o => o.Lines).Sum(l => l.OrderedQty),
                g.SelectMany(o => o.Lines).Sum(l => l.ReceivedQty),
                g.SelectMany(o => o.Lines).Sum(l => l.OutstandingQty)))
            .OrderByDescending(r => r.OrderedQuantity)
            .ThenBy(r => r.SupplierName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return ApplicationResult<PosSupplierPurchasingReportDto>.Success(
            new PosSupplierPurchasingReportDto(range.FromDate, range.ToDate, rows));
    }

    public async Task<ApplicationResult<PosExpenseSummaryReportDto>> GetExpenseSummaryAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct = default)
    {
        var rangeResult = ReportDateRange.Resolve(fromDate, toDate, clock);
        if (!rangeResult.IsSuccess)
        {
            return ApplicationResult<PosExpenseSummaryReportDto>.Failure(rangeResult.ErrorCode!, rangeResult.ErrorMessage!);
        }

        var range = rangeResult.Value!;
        var org = PosOrganizationId.From(organizationId);
        var list = await expenses.ListForSummaryAsync(org, range.FromDate, range.ToDate, ct).ConfigureAwait(false);
        var recorded = list.Where(e => e.Status == Domain.Expenses.ExpenseStatus.Recorded).ToList();
        var voided = list.Where(e => e.Status == Domain.Expenses.ExpenseStatus.Voided).ToList();

        var categoryIds = recorded.Select(e => e.CategoryId).Distinct().ToList();
        var categories = await expenseCategories.ListByIdsAsync(org, categoryIds, ct).ConfigureAwait(false);
        var categoryNames = categories.ToDictionary(c => c.Id.Value, c => (string?)c.Name);

        var byCategory = recorded
            .GroupBy(e => e.CategoryId.Value)
            .Select(g => new ExpenseCategoryReportRowDto(
                g.Key,
                categoryNames.GetValueOrDefault(g.Key),
                ReportMath.RoundMoney(g.Sum(e => e.Amount)),
                g.Count()))
            .OrderByDescending(r => r.TotalAmount)
            .ToList();

        var byPayment = recorded
            .GroupBy(e => Domain.Expenses.ExpensePaymentMethods.ToCode(e.PaymentMethod))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new ReportPaymentBreakdownDto(
                g.Key,
                ReportMath.RoundMoney(g.Sum(e => e.Amount)),
                g.Count()))
            .ToList();

        return ApplicationResult<PosExpenseSummaryReportDto>.Success(
            new PosExpenseSummaryReportDto(
                range.FromDate,
                range.ToDate,
                ReportMath.RoundMoney(recorded.Sum(e => e.Amount)),
                ReportMath.RoundMoney(voided.Sum(e => e.Amount)),
                recorded.Count,
                voided.Count,
                byCategory,
                byPayment));
    }

    public async Task<ApplicationResult<PosProductUtangSummaryReportDto>> GetProductUtangSummaryAsync(
        Guid organizationId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct = default)
    {
        var byProduct = await GetSalesByProductAsync(organizationId, fromDate, toDate, productId: null, branchId: null, ct)
            .ConfigureAwait(false);
        if (!byProduct.IsSuccess)
        {
            return ApplicationResult<PosProductUtangSummaryReportDto>.Failure(byProduct.ErrorCode!, byProduct.ErrorMessage!);
        }

        var utang = await utangReports.GetAsync(organizationId, fromDate, toDate, cancellationToken: ct)
            .ConfigureAwait(false);
        if (!utang.IsSuccess)
        {
            return ApplicationResult<PosProductUtangSummaryReportDto>.Failure(utang.ErrorCode!, utang.ErrorMessage!);
        }

        var range = byProduct.Value!;
        var org = PosOrganizationId.From(organizationId);
        var utangSales = await sales.ListForReportAsync(
                org,
                range.FromDate,
                range.ToDate,
                SaleStatus.Completed,
                SalePaymentMethod.Utang,
                cancellationToken: ct)
            .ConfigureAwait(false);

        var productRows = new Dictionary<Guid, PosSalesByProductRowDto>();
        foreach (var sale in utangSales)
        {
            foreach (var line in sale.Lines)
            {
                productRows.TryGetValue(line.ProductId.Value, out var existing);
                existing ??= new PosSalesByProductRowDto(
                    line.ProductId.Value,
                    line.NameSnapshot,
                    UnitOfMeasures.ToCode(line.UnitOfMeasureSnapshot),
                    SellingModes.ToCode(line.SellingModeSnapshot),
                    0m, 0m, 0m, 0m, 0m, 0m);
                productRows[line.ProductId.Value] = existing with
                {
                    QuantitySold = existing.QuantitySold + line.Quantity,
                    GrossSaleAmount = ReportMath.RoundMoney(existing.GrossSaleAmount + line.LineTotal),
                    NetQuantity = existing.QuantitySold + line.Quantity,
                    NetAmount = ReportMath.RoundMoney(existing.GrossSaleAmount + line.LineTotal)
                };
            }
        }

        var u = utang.Value!;
        return ApplicationResult<PosProductUtangSummaryReportDto>.Success(
            new PosProductUtangSummaryReportDto(
                range.FromDate,
                range.ToDate,
                ReportMath.RoundMoney(utangSales.Sum(s => s.Total)),
                utangSales.Count,
                u.ActiveCustomerOutstanding,
                u.OverdueAmount,
                productRows.Values.OrderByDescending(r => r.NetAmount).ToList()));
    }

    public static bool ActorMayAccessReport(PosRole? role, PosOperationalReportKind kind)
    {
        if (role is null)
        {
            return false;
        }

        return PosRoleMatrix.AllowsReport(role.Value, kind);
    }

    public static UtangCapability CapabilityForReport(PosOperationalReportKind kind) => kind switch
    {
        PosOperationalReportKind.ShiftSummary or PosOperationalReportKind.CashVariance =>
            UtangCapability.ViewShifts,
        PosOperationalReportKind.InventoryStatus
            or PosOperationalReportKind.InventoryMovements
            or PosOperationalReportKind.StockCountVariance =>
            UtangCapability.ViewInventory,
        PosOperationalReportKind.PurchasingSummary
            or PosOperationalReportKind.PurchaseOutstanding
            or PosOperationalReportKind.SupplierPurchasing =>
            UtangCapability.ViewPurchasing,
        PosOperationalReportKind.Expenses => UtangCapability.ViewExpenses,
        _ => UtangCapability.ViewReports
    };

    public static Guid? RestrictShiftActor(PosRole? role, Guid? requestActorId) =>
        role is PosRole.Cashier ? requestActorId : null;

    private static PosShiftSummaryRowDto ToShiftRow(CashierShift shift, CashierShiftSalesTotals totals) =>
        new(
            shift.Id.Value,
            shift.ShiftNumber,
            shift.ActorId,
            shift.Status.ToString(),
            shift.BusinessDate,
            shift.OpeningCashAmount,
            shift.OpeningCashCounted,
            shift.EffectiveCashCountMode.ToString(),
            shift.ClosingCashAmount,
            shift.ExpectedCashAmountSnapshot,
            shift.CashVarianceAmount,
            shift.Status == CashierShiftStatus.Closed
                ? CashCountModes.ClosingState(shift.EffectiveClosingCashCountMode, shift.ClosingCashAmount)
                : CashCountModes.OpeningState(shift.EffectiveOpeningCashCountMode, shift.OpeningCashCounted),
            totals.NetCashSales,
            totals.CashRefundsTotal);

    private async Task<IReadOnlyList<Domain.Returns.SaleReturn>> ListReturnsInRangeAsync(
        PosOrganizationId org,
        ReportDateRange range,
        Guid? branchId,
        CancellationToken ct)
    {
        var (items, _) = await returns.ListAsync(org, new SaleReturnFilter(null, null), 0, 10_000, ct)
            .ConfigureAwait(false);
        var inRange = items
            .Where(r => r.ReturnDate >= range.FromDate && r.ReturnDate <= range.ToDate)
            .ToList();

        if (branchId is null || inRange.Count == 0)
        {
            return inRange;
        }

        var saleIds = inRange.Select(r => r.SaleId.Value).Distinct().ToList();
        var branchSaleIds = await sales
            .ListSaleIdsInBranchAsync(org, saleIds, branchId.Value, ct)
            .ConfigureAwait(false);
        return inRange.Where(r => branchSaleIds.Contains(r.SaleId.Value)).ToList();
    }
}
