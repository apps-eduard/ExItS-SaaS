using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct StockRequestId(Guid Value)
{
    public static StockRequestId New() => new(Guid.NewGuid());

    public static StockRequestId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestId,
                "Stock request id cannot be an empty GUID.");
        }

        return new StockRequestId(value);
    }
}
