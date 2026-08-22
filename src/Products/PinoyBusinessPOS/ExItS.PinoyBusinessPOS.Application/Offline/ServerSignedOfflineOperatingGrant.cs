using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Server-issued offline operating grant. The browser stores and verifies the signature;
/// it cannot mint or alter authoritative fields without the server private key.
/// </summary>
public sealed record ServerSignedOfflineOperatingGrant(
    Guid GrantId,
    int SchemaVersion,
    Guid UserId,
    OfflineGrantScopeKind ScopeKind,
    Guid? OrganizationId,
    string OrganizationDisplayName,
    Guid? BranchId,
    string? BranchName,
    string InstallationDeviceId,
    Guid? PosDeviceId,
    string? RoleCode,
    string? DisplayName,
    string? Username,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset LastOnlineValidatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Signature)
{
    public const int CurrentSchemaVersion = 4;
}

public enum ServerSignedOfflineGrantFailure
{
    None = 0,
    Tampered,
    Expired,
    WrongOrganization,
    WrongBranch,
    WrongDevice,
    WrongUser,
    Malformed,
    UnsupportedSchema
}

public sealed record ServerSignedOfflineGrantVerification(
    bool IsValid,
    ServerSignedOfflineGrantFailure Failure,
    ServerSignedOfflineOperatingGrant? Grant = null)
{
    public static ServerSignedOfflineGrantVerification Success(ServerSignedOfflineOperatingGrant grant) =>
        new(true, ServerSignedOfflineGrantFailure.None, grant);

    public static ServerSignedOfflineGrantVerification Rejected(ServerSignedOfflineGrantFailure failure) =>
        new(false, failure);
}

/// <summary>ECDSA P-256 / SHA-256 canonical signing for offline operating grants.</summary>
public static class OfflineOperatingGrantSigning
{
    public const string CanonicalVersion = "v1";
    private const string Absent = "-";

    public static string Canonicalize(
        Guid grantId,
        int schemaVersion,
        Guid userId,
        OfflineGrantScopeKind scopeKind,
        Guid? organizationId,
        string organizationDisplayName,
        Guid? branchId,
        string? branchName,
        string installationDeviceId,
        Guid? posDeviceId,
        string? roleCode,
        string? displayName,
        string? username,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset lastOnlineValidatedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var fields = new[]
        {
            CanonicalVersion,
            grantId.ToString("D", CultureInfo.InvariantCulture),
            schemaVersion.ToString(CultureInfo.InvariantCulture),
            userId.ToString("D", CultureInfo.InvariantCulture),
            ((int)scopeKind).ToString(CultureInfo.InvariantCulture),
            organizationId?.ToString("D", CultureInfo.InvariantCulture) ?? Absent,
            organizationDisplayName ?? string.Empty,
            branchId?.ToString("D", CultureInfo.InvariantCulture) ?? Absent,
            branchName ?? string.Empty,
            installationDeviceId ?? string.Empty,
            posDeviceId?.ToString("D", CultureInfo.InvariantCulture) ?? Absent,
            roleCode ?? string.Empty,
            displayName ?? string.Empty,
            username ?? string.Empty,
            issuedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            lastOnlineValidatedAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
            expiresAtUtc.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
        };

        return string.Join('|', fields);
    }

    public static string Sign(string privateKeyPem, string canonical)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privateKeyPem);
        ArgumentNullException.ThrowIfNull(canonical);

        using var key = ECDsa.Create();
        key.ImportFromPem(privateKeyPem);
        var signature = key.SignData(Encoding.UTF8.GetBytes(canonical), HashAlgorithmName.SHA256);
        return Convert.ToHexStringLower(signature);
    }

    public static bool Verify(string publicKeyPem, string canonical, string? signatureHex)
    {
        if (string.IsNullOrWhiteSpace(publicKeyPem)
            || string.IsNullOrWhiteSpace(signatureHex)
            || string.IsNullOrWhiteSpace(canonical))
        {
            return false;
        }

        byte[] signature;
        try
        {
            signature = Convert.FromHexString(signatureHex);
        }
        catch (FormatException)
        {
            return false;
        }

        using var key = ECDsa.Create();
        key.ImportFromPem(publicKeyPem);
        return key.VerifyData(
            Encoding.UTF8.GetBytes(canonical),
            signature,
            HashAlgorithmName.SHA256);
    }
}
