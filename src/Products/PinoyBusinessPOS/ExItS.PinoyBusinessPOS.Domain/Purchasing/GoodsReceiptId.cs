using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

public readonly record struct GoodsReceiptId(Guid Value)
{
    public static GoodsReceiptId New() => new(Guid.NewGuid());

    public static GoodsReceiptId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidGoodsReceiptId, "Goods receipt id cannot be empty.");
        }

        return new GoodsReceiptId(value);
    }
}
