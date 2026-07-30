namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Abstraction over a platform-secure key/value store (e.g. MAUI <c>SecureStorage</c>),
/// reserved for FUTURE authentication/session-token work.
///
/// NOT USED IN P5-WP01: this work package ships no login flow, no credential capture, and no
/// token persistence. No implementation of this interface is registered in dependency
/// injection or invoked by WP01 code. Do not store credentials, passwords, or payment data
/// through this abstraction until an explicit future work package authorizes it.
/// </summary>
public interface ISecureTokenStore
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
    Task ClearAsync(string key, CancellationToken ct = default);
}
