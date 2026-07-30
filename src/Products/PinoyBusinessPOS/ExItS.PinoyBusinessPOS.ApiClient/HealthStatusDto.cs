namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>Normalized shape for the POS API's <c>/health</c> endpoint, which may return a bare status string or a JSON object.</summary>
public sealed record HealthStatusDto(string Status);
