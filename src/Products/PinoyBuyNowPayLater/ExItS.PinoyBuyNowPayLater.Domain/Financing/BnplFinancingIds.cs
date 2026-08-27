namespace ExItS.PinoyBuyNowPayLater.Domain.Financing;

public readonly record struct BnplFinancingApplicationId(Guid Value)
{
    public static BnplFinancingApplicationId New() => new(Guid.NewGuid());

    public static BnplFinancingApplicationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidApplicationId,
                "FinancingApplicationId must be a non-empty Guid.");
        }

        return new BnplFinancingApplicationId(value);
    }

    public override string ToString() => Value.ToString("D");
}

public readonly record struct BnplFinancingOfferId(Guid Value)
{
    public static BnplFinancingOfferId New() => new(Guid.NewGuid());

    public static BnplFinancingOfferId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new BnplFinancingDomainException(
                BnplFinancingErrorCodes.InvalidOfferId,
                "FinancingOfferId must be a non-empty Guid.");
        }

        return new BnplFinancingOfferId(value);
    }

    public override string ToString() => Value.ToString("D");
}
