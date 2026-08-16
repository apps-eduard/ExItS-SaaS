using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct DirectPurchaseReceiptLineId(Guid Value)
{
    public static DirectPurchaseReceiptLineId New() => new(Guid.NewGuid());

    public static DirectPurchaseReceiptLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptLineId,
                "Direct purchase receipt line id cannot be empty.");
        }

        return new DirectPurchaseReceiptLineId(value);
    }
}
