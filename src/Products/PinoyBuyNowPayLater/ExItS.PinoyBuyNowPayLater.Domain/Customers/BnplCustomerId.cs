namespace ExItS.PinoyBuyNowPayLater.Domain.Customers;

/// <summary>BNPL-owned customer identity (not Platform, not Commerce).</summary>
public readonly record struct BnplCustomerId(Guid Value)
{
    public static BnplCustomerId New() => new(Guid.NewGuid());

    public static BnplCustomerId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new BnplDomainException(
                BnplCustomerErrorCodes.InvalidCustomerId,
                "CustomerId must be a non-empty Guid.");
        }

        return new BnplCustomerId(value);
    }

    public override string ToString() => Value.ToString("D");
}
