using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct StockCountId(Guid Value)
{
    public static StockCountId New() => new(Guid.NewGuid());

    public static StockCountId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountId,
                "Stock count id cannot be an empty GUID.");
        }

        return new StockCountId(value);
    }
}
