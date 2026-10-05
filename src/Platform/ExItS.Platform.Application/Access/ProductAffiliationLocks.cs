using System.Security.Cryptography;
using System.Text;

namespace ExItS.Platform.Application.Access;

/// <summary>
/// Stable advisory-lock keys for one active organization affiliation per user and product.
/// </summary>
public static class ProductAffiliationLocks
{
    public static Guid ForProduct(string productCode) =>
        GuidFrom("exits-product-affiliation:" + productCode.Trim().ToLowerInvariant());

    public static Guid ForEmail(string normalizedEmail) =>
        GuidFrom("exits-affiliation-email:" + normalizedEmail.Trim().ToLowerInvariant());

    private static Guid GuidFrom(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return new Guid(hash.AsSpan(0, 16));
    }
}
