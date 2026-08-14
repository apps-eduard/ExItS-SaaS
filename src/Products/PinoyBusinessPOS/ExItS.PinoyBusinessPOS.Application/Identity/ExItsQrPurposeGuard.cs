namespace ExItS.PinoyBusinessPOS.Application.Identity;

/// <summary>
/// Client-side QR purpose helpers. After decode, callers with an expected purpose validate locally
/// before calling Platform; otherwise route by envelope type.
/// </summary>
public static class ExItsQrPurposeGuard
{
    public const string Personal = "Personal";
    public const string Organization = "Organization";
    public const string PosDeviceRegistration = "PosDeviceRegistration";

    public const string MismatchMessage =
        "This QR code is not the right type for this screen. Scan a matching ExItS code.";

    public static bool TryParsePurpose(string? payload, out string purpose, out string subject)
    {
        purpose = string.Empty;
        subject = string.Empty;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        var trimmed = payload.Trim();
        if (trimmed.StartsWith("exits://qr/v1/", StringComparison.OrdinalIgnoreCase))
        {
            var rest = trimmed["exits://qr/v1/".Length..];
            var slash = rest.IndexOf('/');
            if (slash <= 0 || slash >= rest.Length - 1)
            {
                return false;
            }

            var type = rest[..slash];
            subject = rest[(slash + 1)..];
            purpose = type.ToLowerInvariant() switch
            {
                "personal" => Personal,
                "organization" => Organization,
                "pos-device-registration" => PosDeviceRegistration,
                _ => string.Empty
            };
            return purpose.Length > 0 && subject.Length > 0;
        }

        // Legacy personal-only payload still accepted by Platform.
        if (trimmed.StartsWith("exits://user/v1/", StringComparison.OrdinalIgnoreCase))
        {
            purpose = Personal;
            subject = trimmed["exits://user/v1/".Length..];
            return subject.Length > 0;
        }

        return false;
    }

    /// <summary>
    /// When <paramref name="expectedPurpose"/> is set, reject mismatches with a plain message.
    /// When null, returns the detected purpose for caller routing.
    /// </summary>
    public static bool TryValidateOrRoute(
        string? payload,
        string? expectedPurpose,
        out string purpose,
        out string? errorMessage)
    {
        purpose = string.Empty;
        errorMessage = null;
        if (!TryParsePurpose(payload, out purpose, out _))
        {
            // Opaque token or bare public ID — allow Platform resolve when no expected purpose,
            // or when expected purpose can accept opaque registration tokens / bare IDs.
            if (string.IsNullOrWhiteSpace(expectedPurpose))
            {
                purpose = string.Empty;
                return true;
            }

            var expected = expectedPurpose.Trim();
            if (string.Equals(expected, PosDeviceRegistration, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(payload)
                && !payload.Contains("://", StringComparison.Ordinal))
            {
                purpose = PosDeviceRegistration;
                return true;
            }

            if (string.Equals(expected, Personal, StringComparison.OrdinalIgnoreCase)
                && LooksLikePublicUserId(payload))
            {
                purpose = Personal;
                return true;
            }

            if (string.Equals(expected, Organization, StringComparison.OrdinalIgnoreCase)
                && LooksLikePublicOrganizationId(payload))
            {
                purpose = Organization;
                return true;
            }

            errorMessage = MismatchMessage;
            return false;
        }

        if (string.IsNullOrWhiteSpace(expectedPurpose))
        {
            return true;
        }

        if (!string.Equals(purpose, expectedPurpose.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = MismatchMessage;
            return false;
        }

        return true;
    }

    private static bool LooksLikePublicUserId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var t = value.Trim();
        return t.Length == 12
               && t.StartsWith("EX-", StringComparison.OrdinalIgnoreCase)
               && t[5] == '-';
    }

    private static bool LooksLikePublicOrganizationId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var t = value.Trim();
        return t.Length == 9 && t.StartsWith("ORG", StringComparison.OrdinalIgnoreCase);
    }
}
