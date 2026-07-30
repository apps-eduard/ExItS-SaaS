using ExItS.PinoyBusinessPOS.Application.Abstractions;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// No-op <see cref="ISecureTokenStore"/> implementation.
///
/// NOT USED IN P5-WP01: registered only so the DI container can satisfy the abstraction if a
/// future component requests it; no P5-WP01 code path reads or writes tokens through this type.
/// A real implementation backed by <c>Microsoft.Maui.Storage.SecureStorage</c> must replace this
/// before any authentication work package ships.
/// </summary>
public sealed class NullSecureTokenStore : ISecureTokenStore
{
    public Task<string?> GetAsync(string key, CancellationToken ct = default) => Task.FromResult<string?>(null);

    public Task SetAsync(string key, string value, CancellationToken ct = default) => Task.CompletedTask;

    public Task ClearAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
}
