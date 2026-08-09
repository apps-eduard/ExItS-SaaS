namespace ExItS.PinoyBusinessPOS.Application.Commercial;

/// <summary>Suggests organization URL slugs from a display name (editable by the user).</summary>
public static class OrganizationSlug
{
    public static string SuggestFromDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return string.Empty;
        }

        var chars = displayName.Trim().ToLowerInvariant()
            .Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        return slug.Trim('-');
    }

    public static bool IsValidFormat(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return false;
        }

        var value = slug.Trim();
        if (value.Length is < 2 or > 64)
        {
            return false;
        }

        // Lowercase letters, numbers, and single hyphens (no leading/trailing hyphen).
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c))
            {
                continue;
            }

            if (c == '-' && i > 0 && i < value.Length - 1 && value[i - 1] != '-')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
