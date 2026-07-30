using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct InventoryReorderChangeId(Guid Value)
{
    public static InventoryReorderChangeId New() => new(Guid.NewGuid());

    public static InventoryReorderChangeId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReorderChangeId,
                "Inventory reorder change id cannot be an empty GUID.");
        }

        return new InventoryReorderChangeId(value);
    }
}
