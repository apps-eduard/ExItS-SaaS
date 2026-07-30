using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

public readonly record struct PurchaseOrderLineId(Guid Value)
{
    public static PurchaseOrderLineId New() => new(Guid.NewGuid());

    public static PurchaseOrderLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPurchaseOrderLineId, "Purchase order line id cannot be empty.");
        }

        return new PurchaseOrderLineId(value);
    }
}
