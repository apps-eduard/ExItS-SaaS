namespace ExItS.PinoyBuyNowPayLater.Application.Access;

public interface IBnplAccessContextProvider
{
    ValueTask<BnplAccessContext?> GetAsync(CancellationToken cancellationToken = default);
}
