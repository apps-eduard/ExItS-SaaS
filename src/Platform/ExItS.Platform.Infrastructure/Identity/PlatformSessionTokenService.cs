using System.Security.Cryptography;
using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.Infrastructure.Identity;

public sealed class PlatformSessionTokenService : IPlatformSessionTokenService
{
    public string CreateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    public string HashToken(string opaqueToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(opaqueToken);
        var hash = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(opaqueToken));
        return Convert.ToHexString(hash);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> data)
    {
        var base64 = Convert.ToBase64String(data);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
