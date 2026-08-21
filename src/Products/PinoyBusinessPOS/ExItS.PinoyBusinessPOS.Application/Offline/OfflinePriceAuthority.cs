using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// A server-issued lease that lets a device sell one product at one price while it is offline.
///
/// The server decides the price, signs it, and refuses to honour anything it did not sign. The
/// device only carries the lease; it can neither mint one nor edit one, because
/// <see cref="Signature"/> is an HMAC over every invariant field including organization, branch,
/// product, selling unit, price and validity window. This is the opposite of a client price
/// snapshot: a snapshot asks the server to trust the device, a lease lets the server trust itself.
/// </summary>
public sealed record OfflinePriceAuthority(
    Guid AuthorityId,
    Guid OrganizationId,
    Guid? BranchId,
    Guid ProductId,
    Guid? SellingUnitId,
    decimal UnitPrice,
    string UnitOfMeasure,
    string SellingMode,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Signature);

/// <summary>Why a presented lease was refused. Every code is a refusal to record money.</summary>
public enum OfflinePriceAuthorityFailure
{
    None = 0,
    Tampered,
    Expired,
    WrongOrganization,
    WrongBranch,
    WrongProductBinding,
    Malformed
}

/// <summary>Outcome of verifying one presented lease. Success carries the price the server signed.</summary>
public sealed record OfflinePriceAuthorityVerification(
    bool IsValid,
    OfflinePriceAuthorityFailure Failure,
    decimal UnitPrice,
    string UnitOfMeasure,
    string SellingMode)
{
    public static OfflinePriceAuthorityVerification Success(OfflinePriceAuthority authority) =>
        new(true, OfflinePriceAuthorityFailure.None, authority.UnitPrice, authority.UnitOfMeasure, authority.SellingMode);

    public static OfflinePriceAuthorityVerification Rejected(OfflinePriceAuthorityFailure failure) =>
        new(false, failure, 0m, string.Empty, string.Empty);
}

/// <summary>
/// Offline price lease configuration.
///
/// The lease window is deliberately shorter than the 30-day offline operate grant and shorter than
/// the 24-hour readiness window: a device may keep selling for a long time, but it may not keep
/// quoting a price the shop owner changed this morning.
/// </summary>
public sealed class OfflinePriceAuthorityOptions
{
    public const string SectionName = "PosOffline";

    /// <summary>
    /// Development-only signing key. Production startup refuses to run with this value, and the
    /// key is never a secret worth protecting here precisely because it cannot reach Production.
    /// </summary>
    public const string DevelopmentSigningKey = "exits-pos-offline-price-authority-dev-only-key";

    /// <summary>HMAC-SHA256 signing key for offline price leases.</summary>
    public string PriceAuthoritySigningKey { get; set; } = DevelopmentSigningKey;

    /// <summary>How long an issued lease may be used to sell offline. Default 8 hours.</summary>
    public int PriceAuthorityValidityHours { get; set; } = 8;
}

/// <summary>
/// Canonical serialization and HMAC for <see cref="OfflinePriceAuthority"/>.
///
/// Every signed field is rendered culture-invariantly and to fixed precision, and timestamps are
/// whole Unix seconds, so a lease that survives a JSON round trip through the browser still hashes
/// to the same bytes it was signed with.
/// </summary>
public static class OfflinePriceAuthoritySigning
{
    /// <summary>Bumped only if the signed field set changes; old leases then fail closed as tampered.</summary>
    public const string CanonicalVersion = "v1";

    private const string Absent = "-";

    public static string Canonicalize(
        Guid authorityId,
        Guid organizationId,
        Guid? branchId,
        Guid productId,
        Guid? sellingUnitId,
        decimal unitPrice,
        string unitOfMeasure,
        string sellingMode,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var fields = new[]
        {
            CanonicalVersion,
            authorityId.ToString("D", CultureInfo.InvariantCulture),
            organizationId.ToString("D", CultureInfo.InvariantCulture),
            branchId?.ToString("D", CultureInfo.InvariantCulture) ?? Absent,
            productId.ToString("D", CultureInfo.InvariantCulture),
            sellingUnitId?.ToString("D", CultureInfo.InvariantCulture) ?? Absent,
            decimal.Round(unitPrice, 4, MidpointRounding.ToEven).ToString("0.0000", CultureInfo.InvariantCulture),
            unitOfMeasure ?? string.Empty,
            sellingMode ?? string.Empty,
            issuedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            expiresAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture)
        };

        return string.Join('|', fields);
    }

    public static string Sign(string signingKey, string canonical)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(signingKey);
        ArgumentNullException.ThrowIfNull(canonical);

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>Constant-time comparison so a wrong signature never leaks how wrong it was.</summary>
    public static bool SignatureMatches(string expectedHex, string? presentedHex)
    {
        if (string.IsNullOrWhiteSpace(presentedHex) || presentedHex.Length != expectedHex.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedHex),
            Encoding.UTF8.GetBytes(presentedHex));
    }
}
