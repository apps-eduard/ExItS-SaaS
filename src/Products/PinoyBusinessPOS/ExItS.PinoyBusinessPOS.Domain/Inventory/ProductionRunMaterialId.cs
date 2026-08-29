using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct ProductionRunMaterialId(Guid Value)
{
    public static ProductionRunMaterialId New() => new(Guid.NewGuid());

    public static ProductionRunMaterialId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRunMaterialId,
                "Production run material id cannot be empty.");
        }

        return new ProductionRunMaterialId(value);
    }
}
