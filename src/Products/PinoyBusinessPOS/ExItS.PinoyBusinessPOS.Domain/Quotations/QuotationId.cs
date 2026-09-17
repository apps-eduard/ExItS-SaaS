using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Quotations;

public readonly record struct QuotationId(Guid Value)
{
    public static QuotationId New() => new(Guid.NewGuid());

    public static QuotationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidQuotationId, "Quotation id cannot be empty.");
        }

        return new QuotationId(value);
    }
}
