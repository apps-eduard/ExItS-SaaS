using ExItS.PinoyBuyNowPayLater.Application.Access;

namespace ExItS.PinoyBuyNowPayLater.Api.Access;

/// <summary>
/// Production default: no trusted BNPL access context until D-P12-03 transport is wired.
/// </summary>
internal sealed class UnavailableBnplAccessContextProvider : IBnplAccessContextProvider
{
    public ValueTask<BnplAccessContext?> GetAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<BnplAccessContext?>(null);
}
