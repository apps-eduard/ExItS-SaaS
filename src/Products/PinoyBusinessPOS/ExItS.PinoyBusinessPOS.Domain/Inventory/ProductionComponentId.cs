using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct ProductionComponentId(Guid Value)
{
    public static ProductionComponentId New() => new(Guid.NewGuid());

    public static ProductionComponentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionComponentId,
                "Production component id cannot be empty.");
        }

        return new ProductionComponentId(value);
    }
}
