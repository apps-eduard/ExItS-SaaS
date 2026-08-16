using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct DirectPurchaseReceiptId(Guid Value)
{
    public static DirectPurchaseReceiptId New() => new(Guid.NewGuid());

    public static DirectPurchaseReceiptId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptId,
                "Direct purchase receipt id cannot be empty.");
        }

        return new DirectPurchaseReceiptId(value);
    }
}
