using ExItS.PinoyLoanManager.Application.Access;

namespace ExItS.PinoyLoanManager.Api.Access;

/// <summary>
/// Default composition: no approved trusted context transport (D-P12-03 open).
/// Absence of context is never treated as authorized.
/// </summary>
internal sealed class UnavailablePlmAccessContextProvider : IPlmAccessContextProvider
{
    public ValueTask<PlmAccessContext?> GetAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<PlmAccessContext?>(null);
}
