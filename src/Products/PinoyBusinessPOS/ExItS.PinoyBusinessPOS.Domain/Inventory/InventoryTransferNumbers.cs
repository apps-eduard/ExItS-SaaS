using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Organization-scoped transfer number: <c>TR-YYMMDD-NNN</c> for roots,
/// and <c>{root}-Rn</c> for replacement children. Allocated on dispatch.
/// Relationship is stored on <see cref="InventoryTransfer.RootTransferId"/> — never parse numbers to infer family.
/// </summary>
public static class InventoryTransferNumbers
{
    public const string Prefix = PosDocumentPrefixes.InventoryTransfer;
    public const int MaxLength = PosDocumentNumbers.MaxLength;
    public const long MaxSequence = PosDocumentNumbers.MaxSequence;
    public const int MaxReplacementSequence = PosDocumentNumbers.MaxChildSequence;

    public static string Format(DateOnly businessDate, long sequence) =>
        Map(() => PosDocumentNumbers.Format(Prefix, businessDate, sequence));

    public static string FormatReplacement(string rootTransferNumber, int replacementSequence) =>
        Map(() => PosDocumentNumbers.FormatChild(rootTransferNumber, replacementSequence));

    public static string Normalize(string? transferNumber) =>
        Map(() => PosDocumentNumbers.Normalize(transferNumber, Prefix));

    public static string NormalizeRoot(string? transferNumber) =>
        Map(() => PosDocumentNumbers.NormalizeRoot(transferNumber, Prefix));

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => PosDocumentNumbers.BusinessDateOf(utcNow);

    private static string Map(Func<string> action)
    {
        try
        {
            return action();
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.InvalidPosDocumentChildSequence)
        {
            throw new DomainException(DomainErrorCodes.InvalidInventoryTransferReplacementSequence, ex.Message);
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.InvalidPosDocumentNumber)
        {
            throw new DomainException(DomainErrorCodes.InvalidInventoryTransferNumber, ex.Message);
        }
    }
}
