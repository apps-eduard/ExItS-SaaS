using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>Typed HTTP client for the PinoyBusinessPOS backend API. Never throws for transport/HTTP failures — all outcomes are reported via <see cref="ApiResult{T}"/>.</summary>
public interface IPosApiClient
{
    Task<ApiResult<T>> GetAsync<T>(string path, CancellationToken ct = default);

    Task<ApiResult<HealthStatusDto>> GetHealthAsync(CancellationToken ct = default);

    /// <summary>General-purpose request. Only <see cref="HttpMethod.Get"/> requests are eligible for the single automatic retry.</summary>
    Task<ApiResult<TResponse>> SendAsync<TResponse>(HttpMethod method, string path, object? body = null, CancellationToken ct = default);
}
