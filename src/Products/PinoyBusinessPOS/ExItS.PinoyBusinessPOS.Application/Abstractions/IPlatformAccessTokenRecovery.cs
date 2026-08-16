namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Secure one-shot AccessToken reissue from a live Platform session (GrantType=session).
/// Used by the Platform HTTP recovery handler; does not fake local expiry extension.
/// </summary>
public interface IPlatformAccessTokenRecovery
{
    /// <summary>
    /// Attempts a single session-grant token reissue. Returns true only when a new AccessToken
    /// was persisted into the current session store/context. Never clears the session on failure.
    /// </summary>
    Task<bool> TryReissueAccessTokenAsync(CancellationToken ct = default);
}
