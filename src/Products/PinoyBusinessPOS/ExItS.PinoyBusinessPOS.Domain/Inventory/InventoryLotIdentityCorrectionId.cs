using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct InventoryLotIdentityCorrectionId(Guid Value)
{
    public static InventoryLotIdentityCorrectionId New() => new(Guid.NewGuid());

    public static InventoryLotIdentityCorrectionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryLotIdentityCorrectionId,
                "Inventory lot identity correction id cannot be an empty GUID.");
        }

        return new InventoryLotIdentityCorrectionId(value);
    }
}
