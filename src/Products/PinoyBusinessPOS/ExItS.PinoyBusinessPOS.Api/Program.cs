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
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddPosPersistence(builder.Configuration);

builder.Services.AddScoped<IPosCommercialAccessAccessor, PosCommercialAccessAccessor>();
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
builder.Services.AddScoped<InventoryQueryService>();
builder.Services.AddScoped<EnableInventoryTracking>();
builder.Services.AddScoped<DisableInventoryTracking>();
builder.Services.AddScoped<AdjustInventoryStock>();
builder.Services.AddScoped<ExpenseCategoryQueryService>();
builder.Services.AddScoped<CreateExpenseCategory>();
builder.Services.AddScoped<UpdateExpenseCategory>();
builder.Services.AddScoped<DeactivateExpenseCategory>();
builder.Services.AddScoped<ReactivateExpenseCategory>();
builder.Services.AddScoped<ExpenseQueryService>();
builder.Services.AddScoped<RecordExpense>();
builder.Services.AddScoped<VoidExpense>();
builder.Services.AddScoped<ExpenseSummaryService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.DashboardQueryService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.SalesReportService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.UtangReportService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.InventoryReportService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.ExpensesReportService>();

var app = builder.Build();

app.UseMiddleware<PosCommercialAccessMiddleware>();

app.MapHealthChecks("/health");
app.MapCustomerEndpoints();
app.MapCreditEndpoints();
app.MapCustomerCreditSyncEndpoints();
app.MapDueDateEndpoints();
app.MapRepaymentEndpoints();
app.MapStatementEndpoints();
app.MapCatalogEndpoints();
app.MapSaleEndpoints();
app.MapInventoryEndpoints();
app.MapExpenseEndpoints();
app.MapReportingEndpoints();
app.MapDevOfflineProbeEndpoints();

// Phase marker: P8-WP07-basic-store-closeout

app.Run();

public partial class Program;
