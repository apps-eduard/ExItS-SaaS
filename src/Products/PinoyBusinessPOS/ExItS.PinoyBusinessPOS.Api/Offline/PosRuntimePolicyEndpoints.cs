using ExItS.PinoyBusinessPOS.Api.Common;

namespace ExItS.PinoyBusinessPOS.Api.Offline;

/// <summary>
/// Exposes POS runtime policy for React PWA UX gates. Server money authorization remains
/// authoritative in <see cref="IPosDeviceTransactionAuthorizer"/>.
/// Enforcement is a server environment setting (not org-scoped).
/// </summary>
internal static class PosRuntimePolicyEndpoints
{
    public static IEndpointRouteBuilder MapPosRuntimePolicyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/pos/runtime/device-authorization", (
            IPosDeviceTransactionAuthorizer deviceAuthorization) =>
            Results.Ok(new
            {
                enforcementEnabled = deviceAuthorization.EnforcementEnabled
            }));

        return app;
    }
}
