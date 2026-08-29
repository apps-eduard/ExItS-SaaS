using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct WasteLossId(Guid Value)
{
    public static WasteLossId New() => new(Guid.NewGuid());

    public static WasteLossId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossId,
                "Waste/loss id cannot be empty.");
        }

        return new WasteLossId(value);
    }
}
