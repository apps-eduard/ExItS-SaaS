using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

public readonly record struct PurchaseOrderId(Guid Value)
{
    public static PurchaseOrderId New() => new(Guid.NewGuid());

    public static PurchaseOrderId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPurchaseOrderId, "Purchase order id cannot be empty.");
        }

        return new PurchaseOrderId(value);
    }
}
