using System.Security.Cryptography;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

public sealed record IssueServerSignedOfflineGrantRequest(
    string InstallationDeviceId,
    string? OrganizationDisplayName = null,
    string? BranchName = null,
    string? DisplayName = null,
    string? Username = null);

public interface IServerSignedOfflineOperatingGrantService
{
    Task<ApplicationResult<ServerSignedOfflineOperatingGrant>> IssueOrganizationGrantAsync(
        Guid actorUserId,
        Guid organizationId,
        Guid branchId,
        Guid posDeviceId,
        string installationDeviceId,
        string? roleCode,
        string organizationDisplayName,
        string? branchName,
        string? displayName,
        string? username,
        CancellationToken cancellationToken = default);

    ServerSignedOfflineGrantVerification Verify(
        ServerSignedOfflineOperatingGrant grant,
        string expectedInstallationDeviceId,
        Guid? expectedUserId = null);
}

public sealed class ServerSignedOfflineOperatingGrantService(
    IClock clock,
    IOptions<OfflinePriceAuthorityOptions> options) : IServerSignedOfflineOperatingGrantService
{
    private readonly OfflinePriceAuthorityOptions _options = options.Value;

    public Task<ApplicationResult<ServerSignedOfflineOperatingGrant>> IssueOrganizationGrantAsync(
        Guid actorUserId,
        Guid organizationId,
        Guid branchId,
        Guid posDeviceId,
        string installationDeviceId,
        string? roleCode,
        string organizationDisplayName,
        string? branchName,
        string? displayName,
        string? username,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
        {
            return Task.FromResult(ApplicationResult<ServerSignedOfflineOperatingGrant>.Failure(
                ApplicationErrorCodes.ActorRequired,
                "An authenticated user is required to issue an offline operating grant."));
        }

        if (organizationId == Guid.Empty || branchId == Guid.Empty || posDeviceId == Guid.Empty)
        {
            return Task.FromResult(ApplicationResult<ServerSignedOfflineOperatingGrant>.Failure(
                ApplicationErrorCodes.OfflineOperatingGrantInvalidScope,
                "Organization, branch, and POS device are required."));
        }

        if (string.IsNullOrWhiteSpace(installationDeviceId))
        {
            return Task.FromResult(ApplicationResult<ServerSignedOfflineOperatingGrant>.Failure(
                ApplicationErrorCodes.OfflineOperatingGrantDeviceRequired,
                "Installation device id is required."));
        }

        var privateKey = _options.OperatingGrantSigningPrivateKeyPem?.Trim();
        if (string.IsNullOrWhiteSpace(privateKey))
        {
            return Task.FromResult(ApplicationResult<ServerSignedOfflineOperatingGrant>.Failure(
                ApplicationErrorCodes.OfflineOperatingGrantSigningUnavailable,
                "Offline operating grant signing is not configured."));
        }

        var now = clock.UtcNow;
        var validityHours = Math.Clamp(_options.OperatingGrantValidityHours, 1, 24 * 365);
        var expiresAt = now.AddHours(validityHours);
        var grantId = Guid.NewGuid();

        var canonical = OfflineOperatingGrantSigning.Canonicalize(
            grantId,
            ServerSignedOfflineOperatingGrant.CurrentSchemaVersion,
            actorUserId,
            OfflineGrantScopeKind.Organization,
            organizationId,
            organizationDisplayName,
            branchId,
            branchName,
            installationDeviceId.Trim(),
            posDeviceId,
            roleCode,
            displayName,
            username,
            now,
            now,
            expiresAt);

        var signature = OfflineOperatingGrantSigning.Sign(privateKey, canonical);
        var grant = new ServerSignedOfflineOperatingGrant(
            grantId,
            ServerSignedOfflineOperatingGrant.CurrentSchemaVersion,
            actorUserId,
            OfflineGrantScopeKind.Organization,
            organizationId,
            organizationDisplayName,
            branchId,
            branchName,
            installationDeviceId.Trim(),
            posDeviceId,
            roleCode,
            displayName,
            username,
            now,
            now,
            expiresAt,
            signature);

        return Task.FromResult(ApplicationResult<ServerSignedOfflineOperatingGrant>.Success(grant));
    }

    public ServerSignedOfflineGrantVerification Verify(
        ServerSignedOfflineOperatingGrant grant,
        string expectedInstallationDeviceId,
        Guid? expectedUserId = null)
    {
        if (grant.SchemaVersion != ServerSignedOfflineOperatingGrant.CurrentSchemaVersion)
        {
            return ServerSignedOfflineGrantVerification.Rejected(
                ServerSignedOfflineGrantFailure.UnsupportedSchema);
        }

        if (clock.UtcNow >= grant.ExpiresAtUtc)
        {
            return ServerSignedOfflineGrantVerification.Rejected(ServerSignedOfflineGrantFailure.Expired);
        }

        if (expectedUserId is Guid userId && userId != Guid.Empty && grant.UserId != userId)
        {
            return ServerSignedOfflineGrantVerification.Rejected(ServerSignedOfflineGrantFailure.WrongUser);
        }

        if (!string.Equals(
                grant.InstallationDeviceId,
                expectedInstallationDeviceId?.Trim(),
                StringComparison.Ordinal))
        {
            return ServerSignedOfflineGrantVerification.Rejected(ServerSignedOfflineGrantFailure.WrongDevice);
        }

        var publicKey = ExtractPublicKeyPem(_options.OperatingGrantSigningPrivateKeyPem);
        if (string.IsNullOrWhiteSpace(publicKey))
        {
            return ServerSignedOfflineGrantVerification.Rejected(ServerSignedOfflineGrantFailure.Malformed);
        }

        var canonical = OfflineOperatingGrantSigning.Canonicalize(
            grant.GrantId,
            grant.SchemaVersion,
            grant.UserId,
            grant.ScopeKind,
            grant.OrganizationId,
            grant.OrganizationDisplayName,
            grant.BranchId,
            grant.BranchName,
            grant.InstallationDeviceId,
            grant.PosDeviceId,
            grant.RoleCode,
            grant.DisplayName,
            grant.Username,
            grant.IssuedAtUtc,
            grant.LastOnlineValidatedAtUtc,
            grant.ExpiresAtUtc);

        if (!OfflineOperatingGrantSigning.Verify(publicKey, canonical, grant.Signature))
        {
            return ServerSignedOfflineGrantVerification.Rejected(ServerSignedOfflineGrantFailure.Tampered);
        }

        return ServerSignedOfflineGrantVerification.Success(grant);
    }

    internal static string? ExtractPublicKeyPem(string? privateKeyPem)
    {
        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            return null;
        }

        using var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(privateKeyPem);
        return ecdsa.ExportSubjectPublicKeyInfoPem();
    }
}
