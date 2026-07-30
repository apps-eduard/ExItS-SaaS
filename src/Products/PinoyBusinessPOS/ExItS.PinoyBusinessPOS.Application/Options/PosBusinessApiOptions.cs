namespace ExItS.PinoyBusinessPOS.Application.Options;

/// <summary>Binds to the "PosBusinessApi" section for the PinoyBusinessPOS product API (customers).</summary>
public sealed class PosBusinessApiOptions
{
    public const string SectionName = "PosBusinessApi";

    public string BaseUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 15;
}
