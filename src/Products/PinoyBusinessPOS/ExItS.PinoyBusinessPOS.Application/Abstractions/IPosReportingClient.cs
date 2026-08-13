using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Reporting;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Typed POS dashboard/reports API client. Online-only for P8-WP06 — offline calls fail fast and
/// no local authoritative report cache is used.
/// </summary>
public interface IPosReportingClient
{
    Task<ApiResult<PosDashboardDto>> GetDashboardAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosManagementOverviewDto>> GetManagementOverviewAsync(CancellationToken ct = default);

    Task<ApiResult<PosSalesReportDto>> GetSalesReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        string? paymentMethod = null,
        string? status = null,
        Guid? productId = null,
        Guid? categoryId = null,
        Guid? customerId = null,
        CancellationToken ct = default);

    Task<ApiResult<PosUtangReportDto>> GetUtangReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        Guid? customerId = null,
        CancellationToken ct = default);

    Task<ApiResult<PosInventoryReportDto>> GetInventoryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        bool? trackedOnly = true,
        bool? lowStockOnly = null,
        string? productStatus = null,
        CancellationToken ct = default);

    Task<ApiResult<PosExpensesReportDto>> GetExpensesReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        Guid? expenseCategoryId = null,
        string? paymentMethod = null,
        string? status = null,
        CancellationToken ct = default);

    Task<ApiResult<PosOperationalOverviewDto>> GetOperationalOverviewAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosSalesSummaryReportDto>> GetSalesSummaryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosSalesByPaymentReportDto>> GetSalesByPaymentReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosSalesByProductReportDto>> GetSalesByProductOperationalAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        Guid? productId = null,
        CancellationToken ct = default);

    Task<ApiResult<PosReturnsReportDto>> GetReturnsReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosShiftSummaryReportDto>> GetShiftSummaryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosCashVarianceReportDto>> GetCashVarianceReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosInventoryStatusReportDto>> GetInventoryStatusReportAsync(CancellationToken ct = default);

    Task<ApiResult<PosInventoryMovementsReportDto>> GetInventoryMovementsReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosStockCountVarianceReportDto>> GetStockCountVarianceReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosPurchasingSummaryReportDto>> GetPurchasingSummaryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosPurchaseOutstandingReportDto>> GetPurchaseOutstandingReportAsync(CancellationToken ct = default);

    Task<ApiResult<PosSupplierPurchasingReportDto>> GetSupplierPurchasingReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosExpenseSummaryReportDto>> GetExpenseSummaryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);

    Task<ApiResult<PosProductUtangSummaryReportDto>> GetProductUtangSummaryReportAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken ct = default);
}
