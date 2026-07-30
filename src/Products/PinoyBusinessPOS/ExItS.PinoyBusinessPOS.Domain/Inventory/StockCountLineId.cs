using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct StockCountLineId(Guid Value)
{
    public static StockCountLineId New() => new(Guid.NewGuid());

    public static StockCountLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountLineId,
                "Stock count line id cannot be an empty GUID.");
        }

        return new StockCountLineId(value);
    }
}
