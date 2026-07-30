using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

public readonly record struct GoodsReceiptLineId(Guid Value)
{
    public static GoodsReceiptLineId New() => new(Guid.NewGuid());

    public static GoodsReceiptLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidGoodsReceiptLineId, "Goods receipt line id cannot be empty.");
        }

        return new GoodsReceiptLineId(value);
    }
}
