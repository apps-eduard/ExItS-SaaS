namespace ExItS.PinoyBuyNowPayLater.Domain.Customers;

public sealed class BnplDomainException : Exception
{
    public BnplDomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
