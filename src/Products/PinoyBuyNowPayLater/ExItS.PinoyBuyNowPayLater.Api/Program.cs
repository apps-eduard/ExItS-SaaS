using ExItS.PinoyBuyNowPayLater.Api;
using ExItS.PinoyBuyNowPayLater.Api.Access;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddBnplAccessBoundary();

var app = builder.Build();
app.UseExceptionHandler();
app.MapBnplHealth();
app.MapBnplAccess();
app.Run();

// Exposes the entry assembly for test hosts without shipping operational probes.
public partial class Program;
