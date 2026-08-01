using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Tenant branding metadata for Admin chrome fallbacks. No arbitrary CSS/scripts;
/// logo is an HTTPS URL only (binary upload deferred).
/// </summary>
public sealed class OrganizationBranding
{
    private static readonly Regex HexColor = new(
        @"^#[0-9A-Fa-f]{6}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string? BrandDisplayName { get; }
    public string? LogoUrl { get; }
    public string? PrimaryColor { get; }
    public string? AccentColor { get; }

    private OrganizationBranding(
        string? brandDisplayName,
        string? logoUrl,
        string? primaryColor,
        string? accentColor)
    {
        BrandDisplayName = brandDisplayName;
        LogoUrl = logoUrl;
        PrimaryColor = primaryColor;
        AccentColor = accentColor;
    }

    public static OrganizationBranding Empty { get; } = new(null, null, null, null);

    public static OrganizationBranding Create(
        string? brandDisplayName,
        string? logoUrl,
        string? primaryColor,
        string? accentColor)
    {
        string? name = null;
        if (!string.IsNullOrWhiteSpace(brandDisplayName))
        {
            name = PlatformOrganization.NormalizeOptionalDisplayName(brandDisplayName);
        }

        return new OrganizationBranding(
            name,
            NormalizeLogoUrl(logoUrl),
            NormalizeColor(primaryColor, nameof(PrimaryColor)),
            NormalizeColor(accentColor, nameof(AccentColor)));
    }

    private static string? NormalizeLogoUrl(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return null;
        }

        var trimmed = logoUrl.Trim();
        if (trimmed.Length > 2048)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationBranding,
                "Logo URL must be at most 2048 characters.");
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.Fragment)
            || trimmed.Contains('<', StringComparison.Ordinal)
            || trimmed.Contains('>', StringComparison.Ordinal)
            || trimmed.Contains('"', StringComparison.Ordinal)
            || trimmed.Contains('\'', StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationBranding,
                "Logo URL must be an absolute https:// URL without scripts or markup.");
        }

        return uri.AbsoluteUri;
    }

    private static string? NormalizeColor(string? color, string field)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return null;
        }

        var trimmed = color.Trim();
        if (!HexColor.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationBranding,
                $"{field} must be a #RRGGBB hex color.");
        }

        return trimmed.ToUpperInvariant();
    }
}
