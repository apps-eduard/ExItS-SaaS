using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Payments;
using ExItS.PinoyBusinessPOS.Api.Statements;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
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

var app = builder.Build();

app.UseMiddleware<PosCommercialAccessMiddleware>();

app.MapHealthChecks("/health");
app.MapCustomerEndpoints();
app.MapCreditEndpoints();
app.MapDueDateEndpoints();
app.MapRepaymentEndpoints();
app.MapStatementEndpoints();

// Phase marker: P6-WP06-utang-mvp-closeout

app.Run();

public partial class Program;
