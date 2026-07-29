using System.Reflection;
using ExItS.Platform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", () => Results.Json(new
{
    service = "ExItS.Platform.Api",
    status = "ok",
    phase = "P2-WP02-identity-organization"
}));

app.MapHealthChecks("/health");

// Touch Infrastructure so the host reference is intentional and loadable.
_ = typeof(AssemblyMarker).GetTypeInfo().Assembly.GetName().Name;

app.Run();

public partial class Program;
