using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

internal static class PlatformUsernameDerivation
{
    public static string DeriveFromEmail(string email)
    {
        var normalizedEmail = PlatformUser.NormalizeEmail(email);
        var local = normalizedEmail.Split('@')[0];
        return SanitizeUsername(local);
    }

    internal static string SanitizeUsername(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '.')
            .ToArray();
        var cleaned = new string(chars).Trim('.');
        while (cleaned.Contains("..", StringComparison.Ordinal))
        {
            cleaned = cleaned.Replace("..", ".", StringComparison.Ordinal);
        }

        if (cleaned.Length > 64)
        {
            cleaned = cleaned[..64].Trim('.');
        }

        return string.IsNullOrWhiteSpace(cleaned) ? "staff" : cleaned;
    }
}
