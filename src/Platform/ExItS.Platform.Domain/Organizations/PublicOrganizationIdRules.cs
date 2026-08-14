using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Immutable public organization identifier (format ORG######).
/// Safe for staff login hosts (e.g. maria@ORG001842). Not a secret.
/// </summary>
public static class PublicOrganizationIdRules
{
    public const string Prefix = "ORG";
    public const string QrSchemePrefix =
        ExItsQrEnvelope.CanonicalSchemePrefix + ExItsQrEnvelope.OrganizationType + "/";
    public const int SequenceDigits = 6;
    public const int CanonicalLength = 9;

    private static readonly Regex CanonicalPattern = new(
        @"^ORG\d{6}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static string Normalize(string? value, bool requireAssigned = true)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (requireAssigned)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPublicOrganizationId,
                    "Public organization ID is required.");
            }

            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.StartsWith(QrSchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[QrSchemePrefix.Length..].Trim();
        }

        trimmed = trimmed.ToUpperInvariant();
        if (!CanonicalPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPublicOrganizationId,
                "Public organization ID must match format ORG001842.");
        }

        return trimmed;
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

    public static string BuildQrPayload(string publicOrganizationId) =>
        ExItsQrEnvelope.Build(ExItsQrPurpose.Organization, publicOrganizationId);

    public static string TryExtractFromQrPayload(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPublicOrganizationId,
                "QR payload is empty.");
        }

        if (ExItsQrEnvelope.TryParse(payload, out var parsed) && parsed is not null)
        {
            if (parsed.Purpose != ExItsQrPurpose.Organization)
            {
                throw new DomainException(
                    DomainErrorCodes.ExItsQrPurposeMismatch,
                    "QR payload is not an organization identity.");
            }

            return parsed.Subject;
        }

        return Normalize(payload);
    }

    /// <summary>Cryptographically random ORG###### (caller must ensure uniqueness).</summary>
    public static string GenerateRandom()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var n = BitConverter.ToUInt32(bytes) % 1_000_000u;
        if (n == 0)
        {
            n = 1;
        }

        return $"{Prefix}{n.ToString($"D{SequenceDigits}")}";
    }
}
