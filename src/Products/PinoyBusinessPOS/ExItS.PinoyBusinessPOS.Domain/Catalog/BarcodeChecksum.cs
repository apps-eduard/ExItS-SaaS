namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>
/// GS1 Mod-10 check digit helper. Only the fixed-length GS1 retail formats carry a verifiable
/// check digit: EAN-8 (8), UPC-A (12), EAN-13 (13) and GTIN-14 (14).
/// </summary>
public static class BarcodeChecksum
{
    public static IReadOnlyList<int> ChecksumVerifiedLengths { get; } = [8, 12, 13, 14];

    public static bool HasVerifiableCheckDigit(int length) => ChecksumVerifiedLengths.Contains(length);

    /// <summary>Computes the Mod-10 check digit for a digits-only payload excluding the check digit.</summary>
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
                throw new ArgumentException("Barcode payload must contain digits only.", nameof(payloadWithoutCheckDigit));
            }

            sum += (ch - '0') * weight;
            weight = weight == 3 ? 1 : 3;
        }

        return (10 - (sum % 10)) % 10;
    }

    /// <summary>
    /// True when the digits-only barcode carries a correct trailing check digit, or when the
    /// length is not one of the checksum-verified GS1 formats.
    /// </summary>
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
}
