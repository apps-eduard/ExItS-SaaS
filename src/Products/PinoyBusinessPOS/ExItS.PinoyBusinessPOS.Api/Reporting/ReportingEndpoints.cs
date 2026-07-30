using System.Globalization;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Reporting;

namespace ExItS.PinoyBusinessPOS.Api.Reporting;

/// <summary>
/// Organization-scoped operational dashboard and report endpoints (P8-WP06). Read-only projections
/// from immutable Basic Store records. Development-stage org/commercial headers; cross-org returns
/// are concealed (fail closed). Online-only — no authoritative offline report cache.
/// </summary>
internal static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pos/dashboard", GetDashboard);
        var reports = app.MapGroup("/api/v1/pos/reports");
        reports.MapGet("/sales", GetSalesReport);
        reports.MapGet("/sales/by-product", GetSalesByProduct);
        reports.MapGet("/sales/by-category", GetSalesByCategory);
        reports.MapGet("/utang", GetUtangReport);
        reports.MapGet("/inventory", GetInventoryReport);
        reports.MapGet("/expenses", GetExpensesReport);
        return app;
    }

    private static async Task<IResult> GetDashboard(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        DashboardQueryService dashboard,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewDashboard, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await dashboard.GetAsync(organizationId, from, to, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetSalesReport(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        string? paymentMethod,
        string? status,
        Guid? productId,
        Guid? categoryId,
        Guid? customerId,
        SalesReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewReports, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports
            .GetAsync(organizationId, from, to, paymentMethod, status, productId, categoryId, customerId, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetSalesByProduct(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        string? paymentMethod,
        Guid? productId,
        Guid? categoryId,
        Guid? customerId,
        SalesReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewReports, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports
            .GetAsync(organizationId, from, to, paymentMethod, status: "Completed", productId, categoryId, customerId, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, dto => Results.Ok(dto.ByProduct));
    }

    private static async Task<IResult> GetSalesByCategory(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        string? paymentMethod,
        Guid? categoryId,
        Guid? customerId,
        SalesReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewReports, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports
            .GetAsync(organizationId, from, to, paymentMethod, status: "Completed", productId: null, categoryId, customerId, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, dto => Results.Ok(dto.ByCategory));
    }

    private static async Task<IResult> GetUtangReport(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? customerId,
        UtangReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewReports, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports.GetAsync(organizationId, from, to, customerId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetInventoryReport(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        bool? trackedOnly,
        bool? lowStockOnly,
        string? productStatus,
        InventoryReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewReports, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports
            .GetAsync(organizationId, from, to, trackedOnly ?? true, lowStockOnly, productStatus, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetExpensesReport(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? expenseCategoryId,
        string? paymentMethod,
        string? status,
        ExpensesReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewReports, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports
            .GetAsync(organizationId, from, to, expenseCategoryId, paymentMethod, status, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }

    private static bool TryParseOptionalDates(
        string? fromDate,
        string? toDate,
        out DateOnly? from,
        out DateOnly? to,
        out IResult? problem)
    {
        from = null;
        to = null;
        problem = null;

        if (!string.IsNullOrWhiteSpace(fromDate))
        {
            if (!DateOnly.TryParseExact(fromDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom))
            {
                problem = PosApiResults.Problem(
                    ApplicationErrorCodes.ReportInvalidDateRange,
                    "fromDate must be yyyy-MM-dd.",
                    StatusCodes.Status400BadRequest);
                return false;
            }

            from = parsedFrom;
        }

        if (!string.IsNullOrWhiteSpace(toDate))
        {
            if (!DateOnly.TryParseExact(toDate.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo))
            {
                problem = PosApiResults.Problem(
                    ApplicationErrorCodes.ReportInvalidDateRange,
                    "toDate must be yyyy-MM-dd.",
                    StatusCodes.Status400BadRequest);
                return false;
            }

            to = parsedTo;
        }

        return true;
    }
}
