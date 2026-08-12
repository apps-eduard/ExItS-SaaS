using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Updates shell sync status from real HTTP outcomes. Device network is not the same as
/// reaching the POS/Platform API (PIN offline, emulator 10.0.2.2 down, connection refused).
/// </summary>
public sealed class PosApiReachabilityHandler(IPosSyncStatusService syncStatus) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            syncStatus.NotifyApiReachability(true);
            return response;
        }
        catch (HttpRequestException)
        {
            syncStatus.NotifyApiReachability(false);
            throw;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            syncStatus.NotifyApiReachability(false);
            throw;
        }
    }
}
