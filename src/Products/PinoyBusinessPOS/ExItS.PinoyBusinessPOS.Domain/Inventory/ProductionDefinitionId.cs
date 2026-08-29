using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct ProductionDefinitionId(Guid Value)
{
    public static ProductionDefinitionId New() => new(Guid.NewGuid());

    public static ProductionDefinitionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionDefinitionId,
                "Production definition id cannot be empty.");
        }

        return new ProductionDefinitionId(value);
    }
}
