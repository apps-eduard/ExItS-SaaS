using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>Adds the stable installation identity required by POS money-operation authorization.</summary>
public sealed class PosInstallationDeviceHeaderHandler(IDeviceIdentityProvider deviceIdentity) : DelegatingHandler
{
    public const string HeaderName = "X-Pos-Installation-Device-Id";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var deviceId = await deviceIdentity.GetOrCreateDeviceIdAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            request.Headers.Remove(HeaderName);
            request.Headers.TryAddWithoutValidation(HeaderName, deviceId);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
