using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Friendly organization-scoped staff login names: local@ORG###### (system login, not a mailbox).
/// </summary>
public static class StaffLoginNameRules
{
    private static readonly Regex LocalPartPattern = new(
        @"^[a-z0-9]+(?:[a-z0-9]+)?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string NormalizeLocalPartFromEmail(string contactEmail)
    {
        var email = PlatformUser.NormalizeEmail(contactEmail);
        var at = email.IndexOf('@');
        var local = at > 0 ? email[..at] : email;
        var cleaned = Regex.Replace(local, @"[^a-z0-9]", string.Empty, RegexOptions.CultureInvariant);
        if (cleaned.Length == 0)
        {
            cleaned = "staff";
        }

        if (cleaned.Length > 32)
        {
            cleaned = cleaned[..32];
        }

        return cleaned;
    }

    public static string Build(string localPart, string publicOrganizationId, int collisionSuffix = 0)
    {
        var orgId = PublicOrganizationIdRules.Normalize(publicOrganizationId);
        var local = (localPart ?? string.Empty).Trim().ToLowerInvariant();
        if (local.Length == 0 || !LocalPartPattern.IsMatch(local))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEmail,
                "Staff login local part is invalid.");
        }

        var withSuffix = collisionSuffix <= 0 ? local : $"{local}{collisionSuffix}";
        // Stored via PlatformUser.NormalizeEmail (case-insensitive unique login key).
        return $"{withSuffix}@{orgId}";
    }

    public static string FormatForDisplay(string staffLogin)
    {
        var normalized = PlatformUser.NormalizeEmail(staffLogin);
        var at = normalized.IndexOf('@');
        if (at <= 0 || at >= normalized.Length - 1)
        {
            return normalized;
        }

        var local = normalized[..at];
        var host = normalized[(at + 1)..].ToUpperInvariant();
        return $"{local}@{host}";
    }

    public static string DeriveUsername(string staffLogin)
    {
        var normalized = PlatformUser.NormalizeEmail(staffLogin);
        var at = normalized.IndexOf('@');
        var local = at > 0 ? normalized[..at] : normalized;
        var host = at > 0 ? normalized[(at + 1)..] : "org";
        var candidate = $"{local}_{host}".Replace(".", "_", StringComparison.Ordinal);
        candidate = Regex.Replace(candidate, @"[^a-z0-9._-]", string.Empty, RegexOptions.CultureInvariant);
        if (candidate.Length < 3)
        {
            candidate = $"st_{candidate}";
        }

        if (candidate.Length > 64)
        {
            candidate = candidate[..64];
        }

        // Username pattern requires start/end alnum.
        candidate = candidate.Trim('.', '-', '_');
        if (candidate.Length < 3)
        {
            candidate = "staff001";
        }

        return candidate;
    }
}
