namespace ExItS.PinoyPawnManager.Api;

internal static class HealthEndpoints
{
    public static void MapPpmHealth(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Text("ok", "text/plain"))
            .WithName("Health");
    }
}
