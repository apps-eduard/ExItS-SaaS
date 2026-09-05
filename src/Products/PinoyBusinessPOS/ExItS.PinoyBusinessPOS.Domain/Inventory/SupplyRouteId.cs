using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct SupplyRouteId(Guid Value)
{
    public static SupplyRouteId New() => new(Guid.NewGuid());

    public static SupplyRouteId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplyRouteId,
                "Supply route id cannot be an empty GUID.");
        }

        return new SupplyRouteId(value);
    }
}
