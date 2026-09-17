namespace ExItS.Platform.Domain.Payments;

public readonly record struct SubscriptionPaymentTransactionId(Guid Value)
{
    public static SubscriptionPaymentTransactionId New() => new(Guid.NewGuid());

    public static SubscriptionPaymentTransactionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Subscription payment transaction id is required.", nameof(value));
        }

        return new SubscriptionPaymentTransactionId(value);
    }

    public override string ToString() => Value.ToString("D");
}
