using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Quotations;

/// <summary>
/// Organization-scoped quotation number: <c>QUO-YYMMDD-NNN</c>.
/// Allocated server-side on quotation create/issue.
/// </summary>
public static class QuotationNumbers
{
    public const string Prefix = PosDocumentPrefixes.Quotation;
    public const int MaxLength = PosDocumentNumbers.MaxLength;
    public const long MaxSequence = PosDocumentNumbers.MaxSequence;

    public static string Format(DateOnly businessDate, long sequence) =>
        Map(() => PosDocumentNumbers.Format(Prefix, businessDate, sequence));

    public static string Normalize(string? quotationNumber) =>
        Map(() => PosDocumentNumbers.NormalizeRoot(quotationNumber, Prefix));

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => PosDocumentNumbers.BusinessDateOf(utcNow);

    private static string Map(Func<string> action)
    {
        try
        {
            return action();
        }
        catch (DomainException ex) when (
            ex.ErrorCode is DomainErrorCodes.InvalidPosDocumentNumber
                or DomainErrorCodes.InvalidPosDocumentChildSequence)
        {
            throw new DomainException(DomainErrorCodes.InvalidQuotationNumber, ex.Message);
        }
    }
}