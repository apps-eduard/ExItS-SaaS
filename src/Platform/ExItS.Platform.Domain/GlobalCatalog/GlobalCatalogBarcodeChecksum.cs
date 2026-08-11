namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// GS1 Mod-10 check digit helper shared with POS retail barcode rules.
/// Verified lengths: EAN-8 (8), UPC-A (12), EAN-13 (13), GTIN-14 (14).
/// </summary>
public static class GlobalCatalogBarcodeChecksum
{
    public static IReadOnlyList<int> ChecksumVerifiedLengths { get; } = [8, 12, 13, 14];

    public static bool HasVerifiableCheckDigit(int length) => ChecksumVerifiedLengths.Contains(length);

    public static int ComputeCheckDigit(string payloadWithoutCheckDigit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadWithoutCheckDigit);

        var sum = 0;
        var weight = 3;
        for (var i = payloadWithoutCheckDigit.Length - 1; i >= 0; i--)
        {
            var ch = payloadWithoutCheckDigit[i];
            if (!char.IsAsciiDigit(ch))
            {
                throw new ArgumentException(
                    "Barcode payload must contain digits only.",
                    nameof(payloadWithoutCheckDigit));
            }

            sum += (ch - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }

        return (10 - (sum % 10)) % 10;
    }

    public static bool IsValid(string digitsOnlyBarcode)
    {
        if (string.IsNullOrEmpty(digitsOnlyBarcode) || !digitsOnlyBarcode.All(char.IsAsciiDigit))
        {
            return false;
        }

        if (!HasVerifiableCheckDigit(digitsOnlyBarcode.Length))
        {
            return true;
        }

        var expected = ComputeCheckDigit(digitsOnlyBarcode[..^1]);
        return expected == digitsOnlyBarcode[^1] - '0';
    }

    /// <summary>Builds a digits-only barcode by appending the Mod-10 check digit to the payload.</summary>
    public static string WithCheckDigit(string payloadWithoutCheckDigit) =>
        payloadWithoutCheckDigit + ComputeCheckDigit(payloadWithoutCheckDigit).ToString(System.Globalization.CultureInfo.InvariantCulture);
}
