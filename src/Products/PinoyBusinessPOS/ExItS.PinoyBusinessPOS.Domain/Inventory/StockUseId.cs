using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct StockUseId(Guid Value)
{
    public static StockUseId New() => new(Guid.NewGuid());

    public static StockUseId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseId,
                "Stock use id cannot be empty.");
        }

        return new StockUseId(value);
    }
}
