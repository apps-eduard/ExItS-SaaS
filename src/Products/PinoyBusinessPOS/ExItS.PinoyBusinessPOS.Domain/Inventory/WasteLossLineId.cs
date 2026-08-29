using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct WasteLossLineId(Guid Value)
{
    public static WasteLossLineId New() => new(Guid.NewGuid());

    public static WasteLossLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossLineId,
                "Waste/loss line id cannot be empty.");
        }

        return new WasteLossLineId(value);
    }
}
