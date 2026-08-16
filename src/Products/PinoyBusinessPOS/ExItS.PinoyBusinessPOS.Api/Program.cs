using ExItS.PinoyBusinessPOS.Application.LocalValidation;
using ExItS.PinoyBusinessPOS.Api.CashierShifts;
using ExItS.PinoyBusinessPOS.Api.Catalog;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Api.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Api.Expenses;
using ExItS.PinoyBusinessPOS.Api.Inventory;
using ExItS.PinoyBusinessPOS.Api.Offline;
using ExItS.PinoyBusinessPOS.Api.Payments;
using ExItS.PinoyBusinessPOS.Api.Privacy;
using ExItS.PinoyBusinessPOS.Api.Reporting;
using ExItS.PinoyBusinessPOS.Api.Sales;
using ExItS.PinoyBusinessPOS.Api.Statements;
using ExItS.PinoyBusinessPOS.Api.Suppliers;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Options;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Privacy;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Application.Suppliers;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Api.Purchasing;
using ExItS.PinoyBusinessPOS.Api.Registers;
using ExItS.PinoyBusinessPOS.Api.OperationalSetup;
using ExItS.PinoyBusinessPOS.Api.Returns;
using ExItS.PinoyBusinessPOS.Api.Permissions;
using ExItS.PinoyBusinessPOS.Application.Registers;
using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using ExItS.PinoyBusinessPOS.Infrastructure;
using ExItS.PinoyBusinessPOS.Infrastructure.LocalValidation;
using ExItS.PinoyBusinessPOS.Infrastructure.Health;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

PosProductionSecurityGuard.ValidateOrThrow(builder);
if (builder.Configuration.GetValue<bool>("LocalValidation:Enabled") && builder.Environment.IsProduction())
{
    throw new InvalidOperationException("LocalValidation:Enabled=true is forbidden in Production.");
}

builder.Services.AddProblemDetails();
builder.Services.AddHttpContextAccessor();
builder.Services.AddPosHealthChecks();
builder.AddPosSecurity();
builder.AddPosForwardedHeaders();
builder.Services.AddPosPersistence(builder.Configuration);

builder.Services.AddScoped<IPosCommercialAccessAccessor, PosCommercialAccessAccessor>();
builder.Services.Configure<PlatformAuthOptions>(builder.Configuration.GetSection(PlatformAuthOptions.SectionName));
builder.Services.AddHttpClient<IPosDeviceTransactionAuthorizer, PosDeviceTransactionAuthorizer>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddHttpClient<IPlatformTokenIntrospectionClient, PlatformTokenIntrospectionClient>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    // Keep short: a wrong PlatformAuth:BaseUrl must fail fast (not hang catalog detail ~15s → 401).
    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddScoped<POSCustomerQueryService>();
builder.Services.AddScoped<CreatePOSCustomer>();
builder.Services.AddScoped<UpdatePOSCustomer>();
builder.Services.AddScoped<DeactivatePOSCustomer>();
builder.Services.AddScoped<ReactivatePOSCustomer>();
builder.Services.AddScoped<CorrelatePOSCustomerToPlatformBusinessCustomer>();
builder.Services.AddScoped<ClearPOSCustomerPlatformCorrelation>();
builder.Services.AddScoped<LinkPOSCustomerPersonalExItsIdentity>();
builder.Services.AddScoped<LinkPOSCustomerOrganizationExItsIdentity>();
builder.Services.AddScoped<ClearPOSCustomerExItsIdentityLink>();
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
builder.Services.AddScoped<CreatePaymentAttempt>();
builder.Services.AddScoped<CancelPaymentAttempt>();
builder.Services.AddScoped<GetPaymentAttempt>();
builder.Services.AddScoped<ProcessPaymentWebhook>();
builder.Services.AddScoped<ReconcilePaymentAttempt>();
builder.Services.AddScoped<SimulatePaymentOutcome>();
builder.Services.AddScoped<VerifyManualGCashTransfer>();
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
builder.Services.AddScoped<UpdateCatalogProductPrices>();
builder.Services.AddScoped<DeactivateCatalogProduct>();
builder.Services.AddScoped<ReactivateCatalogProduct>();
builder.Services.AddScoped<QueryConnectedBuyerAvailability>();
builder.Services.AddScoped<BulkMutateConnectedBuyerAvailability>();
builder.Services.AddScoped<PreviewDefaultConnectedPoPricing>();
builder.Services.AddScoped<ApplyDefaultConnectedPoPricing>();
builder.Services.AddScoped<GetOrganizationCatalogForPlatformSupport>();
builder.Services.Configure<PlatformSupportOptions>(builder.Configuration.GetSection(PlatformSupportOptions.SectionName));
builder.Services.AddScoped<CatalogImportQueryService>();
builder.Services.AddScoped<GetTemplateImportStatus>();
builder.Services.AddScoped<ListImportedGlobalProducts>();
builder.Services.AddScoped<ImportTemplateBatch>();
builder.Services.AddScoped<ImportSelectedProducts>();
builder.Services.AddScoped<ProcessPosCatalogImportChunk>();
builder.Services.AddHttpClient<IPlatformMerchantCatalogClient, PlatformMerchantCatalogClient>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<IPlatformOrganizationPublicResolve, PlatformOrganizationPublicResolveClient>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<IOrganizationBusinessNotificationPublisher, PlatformOrganizationBusinessNotificationClient>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<ILinkedCustomerPlatformAuthorization, LinkedCustomerPlatformAuthorizationClient>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    // Fail fast: unreachable Platform must deny statement access, not hang.
    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddHttpClient<IPersonalFeatureEntitlementClient, PersonalFeatureEntitlementClient>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddHttpClient<IOrganizationTaxConfigurationCapabilityReader, PlatformOrganizationTaxConfigurationCapabilityClient>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.Configure<PersonalStatementsOptions>(
    builder.Configuration.GetSection(PersonalStatementsOptions.SectionName));
builder.Services.AddScoped<AuthorizeLinkedCustomerStatementAccess>();
builder.Services.AddScoped<GetLinkedCustomerStatementSummary>();
builder.Services.AddScoped<ListLinkedCustomerRecentActivity>();
builder.Services.AddScoped<ListLinkedCustomerOpenDebtActivity>();
builder.Services.AddScoped<ListLinkedCustomerOlderSettledActivity>();
builder.Services.AddScoped<GetLinkedCustomerSaleReceipt>();
builder.Services.AddHostedService<ExItS.PinoyBusinessPOS.Infrastructure.Catalog.PosCatalogImportBackgroundService>();
builder.Services.AddScoped<SaleQueryService>();
builder.Services.AddScoped<CheckoutSale>();
builder.Services.AddScoped<VoidSale>();
builder.Services.AddScoped<CustomerOrderQueryService>();
builder.Services.AddScoped<PlaceCustomerOrder>();
builder.Services.AddScoped<QuoteCustomerOrderDelivery>();
builder.Services.AddScoped<AcceptCustomerOrder>();
builder.Services.AddScoped<RejectCustomerOrder>();
builder.Services.AddScoped<CancelCustomerOrder>();
builder.Services.AddScoped<AdvanceCustomerOrderFulfillment>();
builder.Services.AddScoped<CompleteCustomerOrder>();
builder.Services.AddHttpClient<ICustomerOrderBranchDirectory, PosCustomerOrderBranchDirectory>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(3);
});
builder.Services.AddScoped<SaleReturnQueryService>();
builder.Services.AddScoped<ProcessSaleReturn>();
builder.Services.AddScoped<InventoryQueryService>();
builder.Services.AddScoped<InventoryLotQueryService>();
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
builder.Services.AddScoped<InventoryTransferQueryService>();
builder.Services.AddScoped<CreateInventoryTransfer>();
builder.Services.AddScoped<DispatchInventoryTransfer>();
builder.Services.AddScoped<ReceiveInventoryTransfer>();
builder.Services.AddScoped<CancelInventoryTransfer>();
builder.Services.AddHttpClient<IOrganizationBranchDirectory, PosOrganizationBranchDirectory>((provider, client) =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PlatformAuthOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(3);
});
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
builder.Services.AddScoped<RequestConnection>();
builder.Services.AddScoped<RespondConnection>();
builder.Services.AddScoped<DisconnectConnectedSupplier>();
builder.Services.AddScoped<ListRelationships>();
builder.Services.AddScoped<ExposeProduct>();
builder.Services.AddScoped<UpdateExposure>();
builder.Services.AddScoped<ListExposures>();
builder.Services.AddScoped<ListBuyerProductShares>();
builder.Services.AddScoped<ListEligibleProductsForSharing>();
builder.Services.AddScoped<QueryBuyerProductShares>();
builder.Services.AddScoped<SetBuyerProductShares>();
builder.Services.AddScoped<UpsertBuyerProductShare>();
builder.Services.AddScoped<ConfirmBuyerProductSharing>();
builder.Services.AddScoped<BulkMutateBuyerProductShares>();
builder.Services.AddScoped<PreviewBuyerProductPricing>();
builder.Services.AddScoped<ApplyBuyerProductPricing>();
builder.Services.AddScoped<SearchExposedCatalog>();
builder.Services.AddScoped<LinkProduct>();
builder.Services.AddScoped<CreateBuyerProductAndLink>();
builder.Services.AddScoped<SuggestBuyerProductMatches>();
builder.Services.AddScoped<UnlinkProduct>();
builder.Services.AddScoped<ListLinks>();
builder.Services.AddScoped<SyncLinkedProductsDelta>();
builder.Services.AddScoped<SupplierIncomingOrderQuery>();
builder.Services.AddScoped<GetIncomingOrder>();
builder.Services.AddScoped<RespondIncomingOrder>();
builder.Services.AddScoped<AcceptIncoming>();
builder.Services.AddScoped<DeclineIncoming>();
builder.Services.AddScoped<StartPreparingIncomingOrder>();
builder.Services.AddScoped<MarkIncomingOrderFulfilled>();
builder.Services.AddScoped<RevalidateConnectedPoDraft>();
builder.Services.AddScoped<RegisterQueryService>();
builder.Services.AddScoped<CreateRegister>();
builder.Services.AddScoped<UpdateRegister>();
builder.Services.AddScoped<ActivateRegister>();
builder.Services.AddScoped<DeactivateRegister>();
builder.Services.AddScoped<GetOperationalSetupQuery>();
builder.Services.AddScoped<CompleteOperationalSetup>();
builder.Services.AddScoped<UpdateOperationalSetup>();
builder.Services.AddScoped<GetOrganizationPrivacyReadiness>();
builder.Services.AddScoped<ListCashDenominationsQuery>();
builder.Services.AddScoped<ReplaceCashDenominations>();
builder.Services.AddScoped<PurchaseOrderQueryService>();
builder.Services.AddScoped<GoodsReceiptQueryService>();
builder.Services.AddScoped<CreatePurchaseOrder>();
builder.Services.AddScoped<UpdatePurchaseOrder>();
builder.Services.AddScoped<SubmitPurchaseOrder>();
builder.Services.AddScoped<CancelPurchaseOrder>();
builder.Services.AddScoped<ReceivePurchaseOrder>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.DashboardQueryService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.ManagementOverviewQueryService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.SalesReportService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.UtangReportService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.InventoryReportService>();
builder.Services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.ExpensesReportService>();
builder.Services.AddScoped<PosRoleAssignmentQueryService>();
builder.Services.AddScoped<AssignPosRole>();
builder.Services.Configure<PosLocalValidationOptions>(builder.Configuration.GetSection(PosLocalValidationOptions.SectionName));
builder.Services.AddScoped<InitializePosLocalValidationRoles>();
builder.Services.AddHttpClient("LocalValidationPlatformApi", (sp, client) =>
{
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<PosLocalValidationOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.PlatformApiBaseUrl))
    {
        client.BaseAddress = new Uri(opts.PlatformApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHostedService<PosLocalValidationHostedService>();
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
app.MapPaymentAttemptEndpoints();
app.MapStatementEndpoints();
app.MapLinkedCustomerStatementEndpoints();
app.MapCatalogEndpoints();
app.MapCatalogImportEndpoints();
app.MapPlatformSupportCatalogEndpoints();
app.MapSaleEndpoints();
app.MapCustomerOrderEndpoints();
app.MapSaleReturnEndpoints();
app.MapInventoryEndpoints();
app.MapExpenseEndpoints();
app.MapSupplierEndpoints();
app.MapConnectedSupplierEndpoints();
app.MapRegisterEndpoints();
app.MapOperationalSetupEndpoints();
app.MapPrivacyReadinessEndpoints();
app.MapPurchaseOrderEndpoints();
app.MapCashierShiftEndpoints();
app.MapPermissionEndpoints();
app.MapReportingEndpoints();
app.MapDevOfflineProbeEndpoints();

// Phase marker: P10-WP08-phase-10-closeout

app.Run();

public partial class Program;
