namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Circuit-scoped session token for Interactive Server API calls.
/// <see cref="IHttpContextAccessor"/> is unreliable after the Blazor circuit starts;
/// this store is filled from <see cref="Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider"/>.
/// </summary>
public sealed class PlatformCircuitSession
{
    public string? SessionToken { get; set; }
}
