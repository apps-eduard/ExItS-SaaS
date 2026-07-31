using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>Whether returned stock is put back on hand.</summary>
public enum RestockDisposition
{
    ReturnToStock = 0,
    DoNotRestock = 1
}

public static class RestockDispositions
{
    public const int CodeMaxLength = 32;

    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(RestockDisposition.ReturnToStock),
        nameof(RestockDisposition.DoNotRestock)
    ];

    public static string ToCode(RestockDisposition disposition) => disposition.ToString();

    public static bool TryParse(string? code, out RestockDisposition disposition)
    {
        disposition = RestockDisposition.ReturnToStock;
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

        disposition = Enum.Parse<RestockDisposition>(match, ignoreCase: false);
        return true;
    }

    public static RestockDisposition Parse(string? code)
    {
        if (!TryParse(code, out var disposition))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnRestockDisposition,
                $"Restock disposition must be one of: {string.Join(", ", Codes)}.");
        }

        return disposition;
    }
}
