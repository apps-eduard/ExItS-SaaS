using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public enum InventoryTransferMissingDisposition
{
    /// <summary>
    /// Wait for remaining delivery from source. Closes the missing shortfall on this transfer
    /// and increases family RemainingToDispatch (source Fulfill remaining).
    /// </summary>
    ExpectedLater = 0,
    /// <summary>
    /// Explicit request replacement. Closes missing and increases RemainingToDispatch.
    /// </summary>
    CloseMissing = 1,
    /// <summary>
    /// Accept shortage — closes missing and increases WaivedQty (no RemainingToDispatch).
    /// </summary>
    AcceptShortage = 2
}

public static class InventoryTransferMissingDispositions
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(InventoryTransferMissingDisposition.ExpectedLater),
        nameof(InventoryTransferMissingDisposition.CloseMissing),
        nameof(InventoryTransferMissingDisposition.AcceptShortage)
    ];

    public static string ToCode(InventoryTransferMissingDisposition disposition) => disposition.ToString();

    public static bool TryParse(string? code, out InventoryTransferMissingDisposition disposition)
    {
        disposition = InventoryTransferMissingDisposition.ExpectedLater;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmed = code.Trim();
        var match = Codes.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        disposition = Enum.Parse<InventoryTransferMissingDisposition>(match, ignoreCase: false);
        return true;
    }

    public static InventoryTransferMissingDisposition Parse(string? code)
    {
        if (!TryParse(code, out var disposition))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferMissingDisposition,
                $"Missing disposition must be one of: {string.Join(", ", Codes)}.");
        }

        return disposition;
    }
}
