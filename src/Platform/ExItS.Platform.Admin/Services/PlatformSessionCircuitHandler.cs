using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Captures the Platform session token into the circuit scope when Interactive Server starts,
/// while <see cref="IHttpContextAccessor"/> still has the request cookies/claims.
/// Must not throw — a failing circuit handler causes reconnect/refresh loops.
/// </summary>
public sealed class PlatformSessionCircuitHandler(
    PlatformCircuitSession circuitSession,
    IHttpContextAccessor httpContextAccessor) : CircuitHandler
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        try
        {
            var http = httpContextAccessor.HttpContext;
            if (http is null)
            {
                return Task.CompletedTask;
            }

            var token = PlatformBrowserSessionService.ResolveSessionToken(http);
            if (!string.IsNullOrWhiteSpace(token))
            {
                circuitSession.SessionToken = token;
            }
        }
        catch
        {
            // Never fail circuit open.
        }

        return Task.CompletedTask;
    }
}
