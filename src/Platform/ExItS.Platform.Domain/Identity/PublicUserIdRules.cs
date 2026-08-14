using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Immutable public ExItS user identifier (format EX-####-####).
/// Separate from database UUID, email, phone, and username. Safe to display and encode in QR.
/// </summary>
public static class PublicUserIdRules
{
    public const string Prefix = "EX-";
    /// <summary>Legacy personal QR prefix (accepted on parse; prefer <see cref="CanonicalQrSchemePrefix"/>).</summary>
    public const string QrSchemePrefix = "exits://user/v1/";
    /// <summary>Canonical personal QR prefix (<c>exits://qr/v1/personal/</c>).</summary>
    public const string CanonicalQrSchemePrefix = ExItsQrEnvelope.CanonicalSchemePrefix + ExItsQrEnvelope.PersonalType + "/";
    public const int GroupDigits = 4;
    /// <summary>Canonical display length including dashes: EX-4827-1936.</summary>
    public const int CanonicalLength = 12;

    private static readonly Regex CanonicalPattern = new(
        @"^EX-\d{4}-\d{4}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Normalize(string? value, bool requireAssigned = true)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (requireAssigned)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPublicUserId,
                    "ExItS ID is required.");
            }

            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith(CanonicalQrSchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[CanonicalQrSchemePrefix.Length..].Trim();
        }
        else if (trimmed.StartsWith(QrSchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[QrSchemePrefix.Length..].Trim();
        }

        trimmed = trimmed.ToUpperInvariant();

        // Accept EX48271936 / EX-4827-1936 / ex-4827-1936
        var compact = trimmed.Replace("-", string.Empty, StringComparison.Ordinal);
        if (compact.StartsWith("EX", StringComparison.OrdinalIgnoreCase) && compact.Length == 10)
        {
            trimmed = $"EX-{compact[2..6]}-{compact[6..10]}";
        }

        if (!CanonicalPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPublicUserId,
                "ExItS ID must match format EX-4827-1936.");
        }

        return trimmed.ToUpperInvariant();
    }

    public static bool TryNormalize(string? value, out string normalized)
    {
        try
        {
            normalized = Normalize(value, requireAssigned: true);
            return true;
        }
        catch (DomainException)
        {
            normalized = string.Empty;
            return false;
        }
    }

    public static string BuildQrPayload(string publicUserId) =>
        ExItsQrEnvelope.Build(ExItsQrPurpose.Personal, publicUserId);

    public static string TryExtractFromQrPayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPublicUserId,
                "QR payload is empty.");
        }

        return Normalize(payload);
    }

    /// <summary>Cryptographically random EX-####-#### (caller must ensure uniqueness).</summary>
    public static string GenerateRandom()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var n = BitConverter.ToUInt32(bytes);
        var left = (n % 10000).ToString("D4");
        var right = ((n / 10000) % 10000).ToString("D4");
        // Mix entropy across both groups
        RandomNumberGenerator.Fill(bytes);
        var m = BitConverter.ToUInt32(bytes);
        left = (m % 10000).ToString("D4");
        right = ((n ^ m) % 10000).ToString("D4");
        return $"{Prefix}{left}-{right}";
    }
}
