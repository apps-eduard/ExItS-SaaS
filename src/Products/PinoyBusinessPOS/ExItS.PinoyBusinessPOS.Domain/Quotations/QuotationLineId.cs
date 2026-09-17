using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Quotations;

public readonly record struct QuotationLineId(Guid Value)
{
    public static QuotationLineId New() => new(Guid.NewGuid());

    public static QuotationLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidQuotationLineId, "Quotation line id cannot be empty.");
        }

        return new QuotationLineId(value);
    }
}
