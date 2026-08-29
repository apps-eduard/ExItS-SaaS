using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct StockUseLineId(Guid Value)
{
    public static StockUseLineId New() => new(Guid.NewGuid());

    public static StockUseLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseLineId,
                "Stock use line id cannot be empty.");
        }

        return new StockUseLineId(value);
    }
}
