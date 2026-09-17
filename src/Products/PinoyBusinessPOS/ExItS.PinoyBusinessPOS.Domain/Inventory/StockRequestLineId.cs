using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct StockRequestLineId(Guid Value)
{
    public static StockRequestLineId New() => new(Guid.NewGuid());

    public static StockRequestLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockRequestLineId,
                "Stock request line id cannot be an empty GUID.");
        }

        return new StockRequestLineId(value);
    }
}
