using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

public enum PosDeviceRegistrationTokenStatus
{
    Active = 0,
    Redeemed = 1,
    Expired = 2,
    Revoked = 3
}

/// <summary>
/// Opaque, org-scoped, one-time POS device registration token (default TTL 15 minutes).
/// Lookup uses <see cref="TokenHash"/> only; plaintext is returned once at creation.
/// </summary>
public sealed class PosDeviceRegistrationToken
{
    public static readonly TimeSpan DefaultLifetimeToLive = TimeSpan.FromMinutes(15);

    public PosDeviceRegistrationTokenId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public string TokenHash { get; }
    public PlatformUserId CreatedByUserId { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public DateTimeOffset? RedeemedAtUtc { get; private set; }
    public string? RedeemedByInstallationDeviceId { get; private set; }
    public PosDeviceId? RedeemedPosDeviceId { get; private set; }
    public PosDeviceRegistrationTokenStatus Status { get; private set; }

    private PosDeviceRegistrationToken(
        PosDeviceRegistrationTokenId id,
        PlatformOrganizationId organizationId,
        string tokenHash,
        PlatformUserId createdByUserId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? redeemedAtUtc,
        string? redeemedByInstallationDeviceId,
        PosDeviceId? redeemedPosDeviceId,
        PosDeviceRegistrationTokenStatus status)
    {
        Id = id;
        OrganizationId = organizationId;
        TokenHash = tokenHash;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        RedeemedAtUtc = redeemedAtUtc;
        RedeemedByInstallationDeviceId = redeemedByInstallationDeviceId;
        RedeemedPosDeviceId = redeemedPosDeviceId;
        Status = status;
    }

    public static PosDeviceRegistrationToken Create(
        PlatformOrganizationId organizationId,
        PlatformUserId createdByUserId,
        string opaqueToken,
        DateTimeOffset utcNow,
        TimeSpan? lifetime = null,
        PosDeviceRegistrationTokenId? id = null)
    {
        EnsureUtc(utcNow);
        var ttl = lifetime ?? DefaultLifetimeToLive;
        if (ttl <= TimeSpan.Zero || ttl > TimeSpan.FromHours(24))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDeviceRegistrationToken,
                "Registration token lifetime is invalid.");
        }

        var hash = HashToken(opaqueToken);
        return new(
            id ?? PosDeviceRegistrationTokenId.New(),
            organizationId,
            hash,
            createdByUserId,
            utcNow,
            utcNow.Add(ttl),
            redeemedAtUtc: null,
            redeemedByInstallationDeviceId: null,
            redeemedPosDeviceId: null,
            PosDeviceRegistrationTokenStatus.Active);
    }

    internal static PosDeviceRegistrationToken Rehydrate(
        PosDeviceRegistrationTokenId id,
        PlatformOrganizationId organizationId,
        string tokenHash,
        PlatformUserId createdByUserId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset? redeemedAtUtc,
        string? redeemedByInstallationDeviceId,
        PosDeviceId? redeemedPosDeviceId,
        PosDeviceRegistrationTokenStatus status) =>
        new(
            id,
            organizationId,
            tokenHash,
            createdByUserId,
            createdAtUtc,
            expiresAtUtc,
            redeemedAtUtc,
            redeemedByInstallationDeviceId,
            redeemedPosDeviceId,
            status);

    public void EnsureRedeemable(DateTimeOffset utcNow, PlatformOrganizationId expectedOrganizationId)
    {
        EnsureUtc(utcNow);
        if (OrganizationId != expectedOrganizationId)
        {
            throw new DomainException(
                DomainErrorCodes.PosDeviceRegistrationTokenOrganizationMismatch,
                "Registration token does not belong to this organization.");
        }

        RefreshExpired(utcNow);

        if (Status == PosDeviceRegistrationTokenStatus.Redeemed)
        {
            throw new DomainException(
                DomainErrorCodes.PosDeviceRegistrationTokenAlreadyRedeemed,
                "Registration token has already been redeemed.");
        }

        if (Status == PosDeviceRegistrationTokenStatus.Revoked)
        {
            throw new DomainException(
                DomainErrorCodes.PosDeviceRegistrationTokenRevoked,
                "Registration token has been revoked.");
        }

        if (Status == PosDeviceRegistrationTokenStatus.Expired || utcNow >= ExpiresAtUtc)
        {
            Status = PosDeviceRegistrationTokenStatus.Expired;
            throw new DomainException(
                DomainErrorCodes.PosDeviceRegistrationTokenExpired,
                "Registration token has expired.");
        }

        if (Status != PosDeviceRegistrationTokenStatus.Active)
        {
            throw new DomainException(
                DomainErrorCodes.PosDeviceRegistrationTokenNotActive,
                "Registration token is not active.");
        }
    }

    public void Redeem(
        PosDeviceId posDeviceId,
        string installationDeviceId,
        DateTimeOffset utcNow,
        PlatformOrganizationId expectedOrganizationId)
    {
        EnsureRedeemable(utcNow, expectedOrganizationId);
        RedeemedAtUtc = utcNow;
        RedeemedByInstallationDeviceId = PosDevice.NormalizeInstallationDeviceId(installationDeviceId);
        RedeemedPosDeviceId = posDeviceId;
        Status = PosDeviceRegistrationTokenStatus.Redeemed;
    }

    public void Revoke(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        RefreshExpired(utcNow);
        if (Status is PosDeviceRegistrationTokenStatus.Redeemed or PosDeviceRegistrationTokenStatus.Revoked)
        {
            return;
        }

        Status = PosDeviceRegistrationTokenStatus.Revoked;
    }

    public void RefreshExpired(DateTimeOffset utcNow)
    {
        if (Status == PosDeviceRegistrationTokenStatus.Active && utcNow >= ExpiresAtUtc)
        {
            Status = PosDeviceRegistrationTokenStatus.Expired;
        }
    }

    public static string HashToken(string opaqueToken)
    {
        var normalized = ExItsQrEnvelope.NormalizeOpaqueToken(opaqueToken);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
