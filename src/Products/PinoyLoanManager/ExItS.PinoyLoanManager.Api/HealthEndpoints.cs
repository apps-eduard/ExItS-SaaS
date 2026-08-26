namespace ExItS.PinoyLoanManager.Api;

internal static class HealthEndpoints
{
    public static void MapPlmHealth(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Text("ok", "text/plain"))
            .WithName("Health");
    }
}
