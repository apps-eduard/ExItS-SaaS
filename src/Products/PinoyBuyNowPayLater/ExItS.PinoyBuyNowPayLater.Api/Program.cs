using ExItS.PinoyBuyNowPayLater.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.MapBnplHealth();
app.Run();

// Exposes the entry assembly for test hosts without shipping operational probes.
public partial class Program;
