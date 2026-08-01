using ExItS.PinoyBusinessPOS.Application.LivePreview;
using ExItS.PinoyBusinessPOS.Api.CashierShifts;
using ExItS.PinoyBusinessPOS.Api.Catalog;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Expenses;
using ExItS.PinoyBusinessPOS.Api.Inventory;
using ExItS.PinoyBusinessPOS.Api.Offline;
using ExItS.PinoyBusinessPOS.Api.Payments;
using ExItS.PinoyBusinessPOS.Api.Reporting;
using ExItS.PinoyBusinessPOS.Api.Sales;
using ExItS.PinoyBusinessPOS.Api.Statements;
using ExItS.PinoyBusinessPOS.Api.Suppliers;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Api.Purchasing;
using ExItS.PinoyBusinessPOS.Api.Registers;
using ExItS.PinoyBusinessPOS.Api.Returns;
using ExItS.PinoyBusinessPOS.Api.Permissions;
using ExItS.PinoyBusinessPOS.Application.Registers;
using ExItS.PinoyBusinessPOS.Infrastructure;
using ExItS.PinoyBusinessPOS.Infrastructure.LivePreview;
using ExItS.PinoyBusinessPOS.Infrastructure.Health;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

PosProductionSecurityGuard.ValidateOrThrow(builder);
if (builder.Configuration.GetValue<bool>("LivePreview:Enabled") && builder.Environment.IsProduction())
{
    throw new InvalidOperationException("LivePreview:Enabled=true is forbidden in Production.");
}

builder.Services.AddProblemDetails();
builder.Services.AddPosHealthChecks();
builder.AddPosSecurity();
builder.AddPosForwardedHeaders();
builder.Services.AddPosPersistence(builder.Configuration);

builder.Services.AddScoped<IPosCommercialAccessAccessor, PosCommercialAccessAccessor>();
builder.Services.Configure<PlatformAuthOptions>(builder.Configuration.GetSection(PlatformAuthOptions.SectionName));
builder.Services.AddHttpClient<IPlatformTokenIntrospectionClient, PlatformTokenIntrospectionClient>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddScoped<POSCustomerQueryService>();
builder.Services.AddScoped<CreatePOSCustomer>();
builder.Services.AddScoped<UpdatePOSCustomer>();
builder.Services.AddScoped<DeactivatePOSCustomer>();
builder.Services.AddScoped<ReactivatePOSCustomer>();
builder.Services.AddScoped<CreditEntryQueryService>();
builder.Services.AddScoped<CreateCreditEntry>();
builder.Services.AddScoped<ReverseCreditEntry>();
builder.Services.AddScoped<SetCreditDueDate>();
builder.Services.AddScoped<CreditDueDateHistoryQuery>();
builder.Services.AddScoped<OverdueQueryService>();
builder.Services.AddScoped<RepaymentQueryService>();
builder.Services.AddScoped<UtangLedgerQueryService>();
builder.Services.AddScoped<CreateRepayment>();
builder.Services.AddScoped<ReverseRepayment>();
builder.Services.AddScoped<ICustomerStatementService, CustomerStatementService>();
builder.Services.AddScoped<IRepaymentReceiptService, RepaymentReceiptService>();
builder.Services.AddScoped<ProductCategoryQueryService>();
builder.Services.AddScoped<CreateProductCategory>();
builder.Services.AddScoped<UpdateProductCategory>();
builder.Services.AddScoped<DeactivateProductCategory>();
builder.Services.AddScoped<ReactivateProductCategory>();
builder.Services.AddScoped<CatalogProductQueryService>();
builder.Services.AddScoped<CreateCatalogProduct>();
builder.Services.AddScoped<UpdateCatalogProduct>();
builder.Services.AddScoped<DeactivateCatalogProduct>();
builder.Services.AddScoped<ReactivateCatalogProduct>();
builder.Services.AddScoped<SaleQueryService>();
builder.Services.AddScoped<CheckoutSale>();
builder.Services.AddScoped<VoidSale>();
builder.Services.AddScoped<SaleReturnQueryService>();
builder.Services.AddScoped<ProcessSaleReturn>();
builder.Services.AddScoped<InventoryQueryService>();
builder.Services.AddScoped<EnableInventoryTracking>();
builder.Services.AddScoped<DisableInventoryTracking>();
builder.Services.AddScoped<AdjustInventoryStock>();
builder.Services.AddScoped<SetInventoryReorderConfiguration>();
builder.Services.AddScoped<InventoryReconciliationQuery>();
builder.Services.AddScoped<StockCountQueryService>();
builder.Services.AddScoped<CreateStockCount>();
builder.Services.AddScoped<UpdateStockCountDraft>();
builder.Services.AddScoped<UpdateStockCountInProgress>();
builder.Services.AddScoped<StartStockCount>();
builder.Services.AddScoped<CompleteStockCount>();
builder.Services.AddScoped<CancelStockCount>();
builder.Services.AddScoped<CashierShiftQueryService>();
builder.Services.AddScoped<OpenCashierShift>();
builder.Services.AddScoped<CloseCashierShift>();
builder.Services.AddScoped<CancelCashierShift>();
builder.Services.AddScoped<RecordCashierShiftMovement>();
builder.Services.AddScoped<ExpenseCategoryQueryService>();
builder.Services.AddScoped<CreateExpenseCategory>();
builder.Services.AddScoped<UpdateExpenseCategory>();
builder.Services.AddScoped<DeactivateExpenseCategory>();
builder.Services.AddScoped<ReactivateExpenseCategory>();
builder.Services.AddScoped<ExpenseQueryService>();
builder.Services.AddScoped<RecordExpense>();
builder.Services.AddScoped<VoidExpense>();
builder.Services.AddScoped<ExpenseSummaryService>();
builder.Services.AddScoped<SupplierQueryService>();
builder.Services.AddScoped<CreateSupplier>();
builder.Services.AddScoped<UpdateSupplier>();
builder.Services.AddScoped<ActivateSupplier>();
builder.Services.AddScoped<DeactivateSupplier>();
builder.Services.AddScoped<RegisterQueryService>();
builder.Services.AddScoped<CreateRegister>();
builder.Services.AddScoped<UpdateRegister>();
builder.Services.AddScoped<ActivateRegister>();
builder.Services.AddScoped<DeactivateRegister>();
builder.Services.AddScoped<PurchaseOrderQueryService>();
builder.Services.AddScoped<GoodsReceiptQueryService>();
builder.Services.AddScoped<CreatePurchaseOrder>();
builder.Services.AddScoped<UpdatePurchaseOrder>();
builder.Services.AddScoped<SubmitPurchaseOrder>();
builder.Services.AddScoped<CancelPurchaseOrder>();
builder.Services.AddScoped<ReceivePurchaseOrder>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.DashboardQueryService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.SalesReportService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.UtangReportService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.InventoryReportService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.ExpensesReportService>();
builder.Services.AddScoped<PosRoleAssignmentQueryService>();
builder.Services.AddScoped<AssignPosRole>();
builder.Services.Configure<PosLivePreviewOptions>(builder.Configuration.GetSection(PosLivePreviewOptions.SectionName));
builder.Services.AddScoped<InitializePosLivePreviewRoles>();
builder.Services.AddHttpClient("LivePreviewPlatformApi", (sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PosLivePreviewOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.PlatformApiBaseUrl))
    {
        client.BaseAddress = new Uri(opts.PlatformApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHostedService<PosLivePreviewHostedService>();
builder.Services.AddScoped<RevokePosRole>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.OperationalReportService>();

var app = builder.Build();

app.UsePosForwardedHeaders();
app.UsePosSecurity();
app.UseMiddleware<PosPlatformBearerMiddleware>();
app.UseMiddleware<PosCommercialAccessMiddleware>();
app.UseMiddleware<PosRoleResolutionMiddleware>();

app.MapPosRootEndpoint();
app.MapPosHealthEndpoints();
app.MapCustomerEndpoints();
app.MapCreditEndpoints();
app.MapCustomerCreditSyncEndpoints();
app.MapDueDateEndpoints();
app.MapRepaymentEndpoints();
app.MapStatementEndpoints();
app.MapCatalogEndpoints();
app.MapSaleEndpoints();
app.MapSaleReturnEndpoints();
app.MapInventoryEndpoints();
app.MapExpenseEndpoints();
app.MapSupplierEndpoints();
app.MapRegisterEndpoints();
app.MapPurchaseOrderEndpoints();
app.MapCashierShiftEndpoints();
app.MapPermissionEndpoints();
app.MapReportingEndpoints();
app.MapDevOfflineProbeEndpoints();

// Phase marker: P10-WP08-phase-10-closeout

app.Run();

public partial class Program;
