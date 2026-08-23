using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Settings;

/// <summary>Platform-wide branding defaults (not organization tenant branding).</summary>
public sealed class PlatformBrandingDefaults
{
    private static readonly Regex HexColor = new(
        @"^#[0-9A-Fa-f]{6}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string? LogoUrl { get; }
    public string? PrimaryColor { get; }
    public string? AccentColor { get; }

    private PlatformBrandingDefaults(string? logoUrl, string? primaryColor, string? accentColor)
    {
        LogoUrl = logoUrl;
        PrimaryColor = primaryColor;
        AccentColor = accentColor;
    }

    public static PlatformBrandingDefaults Empty { get; } = new(null, null, null);

    public static PlatformBrandingDefaults Create(string? logoUrl, string? primaryColor, string? accentColor) =>
        new(
            NormalizeLogoUrl(logoUrl),
            NormalizeColor(primaryColor, nameof(primaryColor)),
            NormalizeColor(accentColor, nameof(accentColor)));

    private static string? NormalizeLogoUrl(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
        {
            return null;
        }

        var orgBranding = OrganizationBranding.Create(null, logoUrl, null, null);
        return orgBranding.LogoUrl;
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
                DomainErrorCodes.InvalidPlatformSettings,
                $"{field} must be a #RRGGBB hex color.");
        }

        return trimmed.ToUpperInvariant();
    }
}
