using ExItS.PinoyLoanManager.Api;
using ExItS.PinoyLoanManager.Api.Access;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddPlmAccessBoundary();

var app = builder.Build();
app.UseExceptionHandler();
app.MapPlmHealth();
app.Run();

// Exposes the entry assembly for test hosts without shipping operational probes.
public partial class Program;
