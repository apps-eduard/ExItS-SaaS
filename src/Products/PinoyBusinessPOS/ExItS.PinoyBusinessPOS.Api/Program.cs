using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Customers;
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

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapCustomerEndpoints();

// Phase marker: P6-WP01-customers

app.Run();

public partial class Program;
