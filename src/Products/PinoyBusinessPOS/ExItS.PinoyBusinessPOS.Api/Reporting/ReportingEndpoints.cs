using System.Globalization;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Application.Reporting;

namespace ExItS.PinoyBusinessPOS.Api.Reporting;

/// <summary>
/// Organization-scoped operational dashboard and report endpoints (P8-WP06). Read-only projections
/// from immutable Basic Store records. Development-stage org/commercial headers; cross-org returns
/// are concealed (fail closed). Online-only — no authoritative offline report cache.
/// Report branch scope uses optional query <c>branchId</c> only (acting <c>X-Pos-Branch-Id</c> is ignored).
/// </summary>
internal static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pos/dashboard", GetDashboard);
        app.MapGet("/api/v1/pos/management/overview", GetManagementOverview);
        var reports = app.MapGroup("/api/v1/pos/reports");
        reports.MapGet("/sales", GetSalesReport);
        reports.MapGet("/sales/by-product", GetSalesByProduct);
        reports.MapGet("/sales/by-category", GetSalesByCategory);
        reports.MapGet("/utang", GetUtangReport);
        reports.MapGet("/inventory", GetInventoryReport);
        reports.MapGet("/expenses", GetExpensesReport);
        reports.MapGet("/overview", GetOperationalOverview);
        reports.MapGet("/sales-summary", GetSalesSummary);
        reports.MapGet("/sales-by-cashier", GetSalesByCashier);
        reports.MapGet("/sales-by-payment", GetSalesByPayment);
        reports.MapGet("/sales-by-product", GetSalesByProductOperational);
        reports.MapGet("/returns", GetReturnsReport);
        reports.MapGet("/profitability", GetProfitabilityReport);
        reports.MapGet("/shifts-summary", GetShiftSummary);
        reports.MapGet("/cash-variance", GetCashVariance);
        reports.MapGet("/inventory-status", GetInventoryStatus);
        reports.MapGet("/inventory-movements", GetInventoryMovements);
        reports.MapGet("/stock-count-variance", GetStockCountVariance);
        reports.MapGet("/purchasing-summary", GetPurchasingSummary);
        reports.MapGet("/purchase-outstanding", GetPurchaseOutstanding);
        reports.MapGet("/supplier-purchasing", GetSupplierPurchasing);
        reports.MapGet("/expenses-summary", GetExpenseSummary);
        reports.MapGet("/utang-by-product", GetProductUtangSummary);
        return app;
    }

    private static async Task<IResult> GetDashboard(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? branchId,
        DashboardQueryService dashboard,
        IOrganizationBranchDirectory branches,
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

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await dashboard.GetAsync(organizationId, from, to, branchId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetManagementOverview(
        HttpRequest request,
        ManagementOverviewQueryService overview,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorize(request, access, UtangCapability.ViewDashboard, out var organizationId, out var problem))
        {
            return problem!;
        }

        var dto = await overview.GetAsync(organizationId, ct).ConfigureAwait(false);
        return Results.Ok(dto);
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
        Guid? branchId,
        SalesReportService reports,
        IOrganizationBranchDirectory branches,
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

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports
            .GetAsync(organizationId, from, to, paymentMethod, status, productId, categoryId, customerId, branchId, ct)
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
        Guid? branchId,
        SalesReportService reports,
        IOrganizationBranchDirectory branches,
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

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports
            .GetAsync(organizationId, from, to, paymentMethod, status: "Completed", productId, categoryId, customerId, branchId, ct)
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
        Guid? branchId,
        SalesReportService reports,
        IOrganizationBranchDirectory branches,
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

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports
            .GetAsync(organizationId, from, to, paymentMethod, status: "Completed", productId: null, categoryId, customerId, branchId, ct)
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

    private static async Task<IResult> GetOperationalOverview(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? branchId,
        OperationalReportService reports,
        IOrganizationBranchDirectory branches,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.Overview, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports.GetOverviewAsync(organizationId, from, to, branchId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetSalesSummary(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? branchId,
        OperationalReportService reports,
        IOrganizationBranchDirectory branches,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.SalesSummary, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports.GetSalesSummaryAsync(organizationId, from, to, branchId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetSalesByCashier(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? branchId,
        OperationalReportService reports,
        IOrganizationBranchDirectory branches,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.SalesByCashier, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports.GetSalesByCashierAsync(organizationId, from, to, branchId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetSalesByPayment(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? branchId,
        OperationalReportService reports,
        IOrganizationBranchDirectory branches,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.SalesByPayment, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports.GetSalesByPaymentAsync(organizationId, from, to, branchId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetSalesByProductOperational(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? productId,
        Guid? branchId,
        OperationalReportService reports,
        IOrganizationBranchDirectory branches,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.SalesByProduct, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports.GetSalesByProductAsync(organizationId, from, to, productId, branchId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetReturnsReport(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? branchId,
        OperationalReportService reports,
        IOrganizationBranchDirectory branches,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.Returns, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports.GetReturnsAsync(organizationId, from, to, branchId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetProfitabilityReport(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? branchId,
        ProfitabilityReportService reports,
        IOrganizationBranchDirectory branches,
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

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports
            .GetAsync(organizationId, from, to, branchId, ct)
            .ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetShiftSummary(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        OperationalReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.ShiftSummary, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        PosOrganizationScope.TryGetActorId(request, out var actorId, out _);
        var restrict = OperationalReportService.RestrictShiftActor(PosRoleRequestContext.CurrentRole, actorId);
        var result = await reports.GetShiftSummaryAsync(organizationId, from, to, restrict, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetCashVariance(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        OperationalReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.CashVariance, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        PosOrganizationScope.TryGetActorId(request, out var actorId, out _);
        var restrict = OperationalReportService.RestrictShiftActor(PosRoleRequestContext.CurrentRole, actorId);
        var result = await reports.GetCashVarianceAsync(organizationId, from, to, restrict, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetInventoryStatus(
        HttpRequest request,
        OperationalReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.InventoryStatus, out var organizationId, out var problem))
        {
            return problem!;
        }

        var result = await reports.GetInventoryStatusAsync(organizationId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetInventoryMovements(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        Guid? branchId,
        OperationalReportService reports,
        IOrganizationBranchDirectory branches,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.InventoryMovements, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        if ((problem = await ValidateReportBranchAsync(organizationId, branchId, branches, ct).ConfigureAwait(false)) is not null)
        {
            return problem;
        }

        var result = await reports.GetInventoryMovementsAsync(organizationId, from, to, branchId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetStockCountVariance(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        OperationalReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.StockCountVariance, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports.GetStockCountVarianceAsync(organizationId, from, to, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetPurchasingSummary(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        OperationalReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.PurchasingSummary, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports.GetPurchasingSummaryAsync(organizationId, from, to, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetPurchaseOutstanding(
        HttpRequest request,
        OperationalReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.PurchaseOutstanding, out var organizationId, out var problem))
        {
            return problem!;
        }

        var result = await reports.GetPurchaseOutstandingAsync(organizationId, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetSupplierPurchasing(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        OperationalReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.SupplierPurchasing, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports.GetSupplierPurchasingAsync(organizationId, from, to, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetExpenseSummary(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        OperationalReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.Expenses, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports.GetExpenseSummaryAsync(organizationId, from, to, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult> GetProductUtangSummary(
        HttpRequest request,
        string? fromDate,
        string? toDate,
        OperationalReportService reports,
        IPosCommercialAccessAccessor access,
        CancellationToken ct)
    {
        if (!TryAuthorizeReport(request, access, PosOperationalReportKind.Utang, out var organizationId, out var problem))
        {
            return problem!;
        }

        if (!TryParseOptionalDates(fromDate, toDate, out var from, out var to, out problem))
        {
            return problem!;
        }

        var result = await reports.GetProductUtangSummaryAsync(organizationId, from, to, ct).ConfigureAwait(false);
        return PosApiResults.FromResult(result, Results.Ok);
    }

    private static async Task<IResult?> ValidateReportBranchAsync(
        Guid organizationId,
        Guid? branchId,
        IOrganizationBranchDirectory branches,
        CancellationToken ct)
    {
        var validation = await PosReportBranchScope
            .ValidateOptionalAsync(branches, organizationId, branchId, ct)
            .ConfigureAwait(false);
        if (validation is null)
        {
            return null;
        }

        return PosApiResults.FromResult(validation, () => Results.Ok());
    }

    private static bool TryAuthorizeReport(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        PosOperationalReportKind kind,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!TryAuthorize(request, access, OperationalReportService.CapabilityForReport(kind), out organizationId, out problem))
        {
            return false;
        }

        if (!PosCommercialScope.TryAuthorize(access, UtangCapability.ViewAdvancedReports, out problem))
        {
            return false;
        }

        if (!PosRoleRequestContext.HasActorHeader
            || (PosRoleRequestContext.CurrentRole is null
                && !PosRoleRequestContext.OrganizationManagementAuthority))
        {
            problem = PosApiResults.Problem(
                Domain.Common.DomainErrorCodes.PosRoleRequired,
                "An active POS role assignment is required for operational reports.",
                StatusCodes.Status403Forbidden);
            return false;
        }

        if (PosRoleRequestContext.CurrentRole is { } role)
        {
            if (!OperationalReportService.ActorMayAccessReport(role, kind))
            {
                problem = PosApiResults.Problem(
                    Domain.Common.DomainErrorCodes.PosRoleDenied,
                    "The active POS role cannot access this report.",
                    StatusCodes.Status403Forbidden);
                return false;
            }
        }
        else if (!PosRoleMatrix.AllowsOrganizationManagementReport(
                     PosRoleRequestContext.OrganizationManagementIsExactOwner,
                     kind))
        {
            problem = PosApiResults.Problem(
                Domain.Common.DomainErrorCodes.PosRoleDenied,
                "Organization management authority cannot access this report.",
                StatusCodes.Status403Forbidden);
            return false;
        }

        return true;
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
