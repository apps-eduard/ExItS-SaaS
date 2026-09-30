using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Organization-scoped customer order number: <c>ORD-YYMMDD-NNN</c>.
/// Allocated server-side on order place.
/// </summary>
public static class CustomerOrderNumbers
{
    public const string Prefix = PosDocumentPrefixes.CustomerOrder;
    public const int MaxLength = PosDocumentNumbers.MaxLength;
    public const long MaxSequence = PosDocumentNumbers.MaxSequence;

    public static string Format(DateOnly businessDate, long sequence) =>
        Map(() => PosDocumentNumbers.Format(Prefix, businessDate, sequence));

    public static string Normalize(string? orderNumber) =>
        Map(() => PosDocumentNumbers.NormalizeRoot(orderNumber, Prefix));

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
            throw new DomainException(DomainErrorCodes.InvalidCustomerOrderNumber, ex.Message);
        }
    }
}