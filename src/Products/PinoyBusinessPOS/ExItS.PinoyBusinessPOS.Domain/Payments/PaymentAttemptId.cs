using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

public readonly record struct PaymentAttemptId(Guid Value)
{
    public static PaymentAttemptId New() => new(Guid.NewGuid());

    public static PaymentAttemptId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPaymentAttemptId,
                "Payment attempt id cannot be empty.");
        }

        return new PaymentAttemptId(value);
    }

    public override string ToString() => Value.ToString("D");
}
