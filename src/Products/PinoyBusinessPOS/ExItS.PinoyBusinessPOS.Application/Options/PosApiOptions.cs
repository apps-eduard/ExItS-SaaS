namespace ExItS.PinoyBusinessPOS.Application.Options;

/// <summary>Binds to the "PosApi" configuration section consumed by <c>AddPosApiClient</c>.</summary>
public sealed class PosApiOptions
{
    public const string SectionName = "PosApi";

    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 15;
}
