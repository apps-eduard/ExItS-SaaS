namespace ExItS.PinoyBuyNowPayLater.Api;

internal static class HealthEndpoints
{
    public static void MapBnplHealth(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Text("ok", "text/plain"))
            .WithName("Health");
    }
}
