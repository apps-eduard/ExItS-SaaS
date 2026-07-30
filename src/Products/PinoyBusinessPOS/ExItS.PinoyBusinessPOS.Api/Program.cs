using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Credit;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Api.Payments;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddPosPersistence(builder.Configuration);

builder.Services.AddScoped<POSCustomerQueryService>();
builder.Services.AddScoped<CreatePOSCustomer>();
builder.Services.AddScoped<UpdatePOSCustomer>();
builder.Services.AddScoped<DeactivatePOSCustomer>();
builder.Services.AddScoped<ReactivatePOSCustomer>();
builder.Services.AddScoped<CreditEntryQueryService>();
builder.Services.AddScoped<CreateCreditEntry>();
builder.Services.AddScoped<ReverseCreditEntry>();
builder.Services.AddScoped<RepaymentQueryService>();
builder.Services.AddScoped<UtangLedgerQueryService>();
builder.Services.AddScoped<CreateRepayment>();
builder.Services.AddScoped<ReverseRepayment>();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapCustomerEndpoints();
app.MapCreditEndpoints();
app.MapRepaymentEndpoints();

// Phase marker: P6-WP03-payments-and-ledger

app.Run();

public partial class Program;
