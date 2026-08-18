using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>
/// Device-local Platform session handles keyed by user id. Used after PIN verifies identity
/// so GrantType=session can reissue an AccessToken for that same user only.
/// </summary>
public sealed class PinRecoverySessionStore(ISecureTokenStore tokens) : IPinRecoverySessionStore
{
    public Task SaveAsync(Guid userId, string platformSessionToken, CancellationToken ct = default)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(platformSessionToken))
        {
            return Task.CompletedTask;
        }

        return tokens.SetAsync(
            SecureTokenKeys.PinRecoveryPlatformSessionFor(userId),
            platformSessionToken.Trim(),
            ct);
    }

    public Task<string?> LoadAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return Task.FromResult<string?>(null);
        }

        return tokens.GetAsync(SecureTokenKeys.PinRecoveryPlatformSessionFor(userId), ct);
    }

    public Task ClearAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return tokens.ClearAsync(SecureTokenKeys.PinRecoveryPlatformSessionFor(userId), ct);
    }
}
