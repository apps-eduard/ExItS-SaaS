namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>No-op recovery used when the host does not wire session-grant reissue (e.g. Org Web).</summary>
public sealed class NoOpPlatformAccessTokenRecovery : IPlatformAccessTokenRecovery
{
    public Task<bool> TryReissueAccessTokenAsync(CancellationToken ct = default) =>
        Task.FromResult(false);
}
