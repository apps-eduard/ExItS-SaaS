using ExItS.PinoyBuyNowPayLater.Api;
using ExItS.PinoyBuyNowPayLater.Api.Access;
using ExItS.PinoyBuyNowPayLater.Api.Customers;
using ExItS.PinoyBuyNowPayLater.Application;
using ExItS.PinoyBuyNowPayLater.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddBnplAccessBoundary();
builder.Services.AddBnplApplication();
builder.Services.AddBnplPersistenceIfConfigured(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();
app.MapBnplHealth();
app.MapBnplAccess();
app.MapBnplCustomers();
app.Run();

// Exposes the entry assembly for test hosts without shipping operational probes.
public partial class Program;
