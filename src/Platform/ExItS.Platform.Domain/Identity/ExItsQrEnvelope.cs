using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Canonical ExItS QR envelope parser/builder (v1).
/// Personal: <c>exits://qr/v1/personal/{EX-####-####}</c>
/// Organization: <c>exits://qr/v1/organization/{ORG######}</c>
/// Device registration: <c>exits://qr/v1/pos-device-registration/{opaqueToken}</c>
/// Legacy personal <c>exits://user/v1/{EX-…}</c> is accepted on parse only.
/// </summary>
public static class ExItsQrEnvelope
{
    public const string CanonicalSchemePrefix = "exits://qr/v1/";
    public const string LegacyPersonalSchemePrefix = "exits://user/v1/";
    public const int SupportedVersion = 1;

    public const string PersonalType = "personal";
    public const string OrganizationType = "organization";
    public const string PosDeviceRegistrationType = "pos-device-registration";

    public sealed record Parsed(ExItsQrPurpose Purpose, string Subject, int Version);

    public static string Build(ExItsQrPurpose purpose, string subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExItsQrPayload,
                "QR subject cannot be empty.");
        }

        return purpose switch
        {
            ExItsQrPurpose.Personal =>
                CanonicalSchemePrefix + PersonalType + "/" + PublicUserIdRules.Normalize(subject),
            ExItsQrPurpose.Organization =>
                CanonicalSchemePrefix + OrganizationType + "/" + PublicOrganizationIdRules.Normalize(subject),
            ExItsQrPurpose.PosDeviceRegistration =>
                CanonicalSchemePrefix + PosDeviceRegistrationType + "/" + NormalizeOpaqueToken(subject),
            _ => throw new DomainException(
                DomainErrorCodes.InvalidExItsQrPurpose,
                "Unknown ExItS QR purpose.")
        };
    }

    public static Parsed Parse(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExItsQrPayload,
                "QR payload is empty.");
        }

        var trimmed = payload.Trim();

        // Legacy personal form.
        if (trimmed.StartsWith(LegacyPersonalSchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            var legacySubject = trimmed[LegacyPersonalSchemePrefix.Length..].Trim();
            return new Parsed(ExItsQrPurpose.Personal, PublicUserIdRules.Normalize(legacySubject), SupportedVersion);
        }

        if (!trimmed.StartsWith(CanonicalSchemePrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExItsQrPayload,
                "QR payload scheme is not recognized.");
        }

        var remainder = trimmed[CanonicalSchemePrefix.Length..];
        var slash = remainder.IndexOf('/');
        if (slash <= 0 || slash >= remainder.Length - 1)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExItsQrPayload,
                "QR payload is malformed.");
        }

        var type = remainder[..slash].Trim().ToLowerInvariant();
        var subjectRaw = remainder[(slash + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(subjectRaw))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExItsQrPayload,
                "QR payload subject is empty.");
        }

        // Version is fixed in the scheme prefix (v1). Reject unknown types.
        return type switch
        {
            PersonalType => new Parsed(ExItsQrPurpose.Personal, PublicUserIdRules.Normalize(subjectRaw), SupportedVersion),
            OrganizationType => new Parsed(
                ExItsQrPurpose.Organization,
                PublicOrganizationIdRules.Normalize(subjectRaw),
                SupportedVersion),
            PosDeviceRegistrationType => new Parsed(
                ExItsQrPurpose.PosDeviceRegistration,
                NormalizeOpaqueToken(subjectRaw),
                SupportedVersion),
            _ => throw new DomainException(
                DomainErrorCodes.InvalidExItsQrPurpose,
                "Unknown ExItS QR type.")
        };
    }

    public static bool TryParse(string? payload, out Parsed? parsed)
    {
        try
        {
            parsed = Parse(payload);
            return true;
        }
        catch (DomainException)
        {
            parsed = null;
            return false;
        }
    }

    public static void EnsureExpectedPurpose(Parsed parsed, ExItsQrPurpose expected)
    {
        if (parsed.Purpose != expected)
        {
            throw new DomainException(
                DomainErrorCodes.ExItsQrPurposeMismatch,
                "QR purpose does not match the expected scan context.");
        }
    }

    /// <summary>Rejects empty and obviously non-token subjects; keeps opaque tokens opaque.</summary>
    public static string NormalizeOpaqueToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDeviceRegistrationToken,
                "Registration token cannot be blank.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length < 16 || trimmed.Length > 256)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDeviceRegistrationToken,
                "Registration token length is invalid.");
        }

        // Reject whitespace and path separators that would break the envelope.
        if (trimmed.Contains(' ', StringComparison.Ordinal)
            || trimmed.Contains('/', StringComparison.Ordinal)
            || trimmed.Contains('\\', StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDeviceRegistrationToken,
                "Registration token format is invalid.");
        }

        return trimmed;
    }
}
