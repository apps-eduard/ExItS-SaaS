using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public readonly record struct ProductionRunId(Guid Value)
{
    public static ProductionRunId New() => new(Guid.NewGuid());

    public static ProductionRunId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRunId,
                "Production run id cannot be empty.");
        }

        return new ProductionRunId(value);
    }
}
