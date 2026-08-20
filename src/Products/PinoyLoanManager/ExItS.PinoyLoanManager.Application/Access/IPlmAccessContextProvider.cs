namespace ExItS.PinoyLoanManager.Application.Access;

/// <summary>
/// Resolves the trusted PLM request access context.
/// Transport (headers, cookies, tokens, Platform DB) is intentionally unspecified — D-P12-03 / R-091.
/// </summary>
public interface IPlmAccessContextProvider
{
    /// <summary>
    /// Returns a trusted context when an approved transport has supplied one; otherwise null (unavailable).
    /// Null never means authorized.
    /// </summary>
    ValueTask<PlmAccessContext?> GetAsync(CancellationToken cancellationToken = default);
}
