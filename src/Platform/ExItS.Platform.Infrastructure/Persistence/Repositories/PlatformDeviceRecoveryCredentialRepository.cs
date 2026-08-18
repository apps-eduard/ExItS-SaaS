using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformDeviceRecoveryCredentialRepository : IPlatformDeviceRecoveryCredentialRepository
{
    private readonly PlatformDbContext _db;

    public PlatformDeviceRecoveryCredentialRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformDeviceRecoveryCredential?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformDeviceRecoveryCredentials.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<PlatformDeviceRecoveryCredential?> GetActiveByUserAndDeviceAsync(
        PlatformUserId userId,
        string installationDeviceId,
        CancellationToken cancellationToken = default)
    {
        var normalizedDeviceId = installationDeviceId.Trim();
        var utcNow = DateTimeOffset.UtcNow;
        var record = await _db.PlatformDeviceRecoveryCredentials.AsNoTracking()
            .Where(c =>
                c.UserId == userId.Value
                && c.InstallationDeviceId == normalizedDeviceId
                && c.RevokedAtUtc == null
                && c.IdleExpiresAtUtc > utcNow
                && c.AbsoluteExpiresAtUtc > utcNow)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PlatformDeviceRecoveryCredential credential, CancellationToken cancellationToken = default)
    {
        _db.PlatformDeviceRecoveryCredentials.Add(ToRecord(credential));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformDeviceRecoveryCredential credential, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformDeviceRecoveryCredentials
            .FirstOrDefaultAsync(c => c.Id == credential.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.RecoveryCredentialInvalid,
                "Device recovery credential was not found.");
        }

        record.LastUsedAtUtc = credential.LastUsedAtUtc;
        record.IdleExpiresAtUtc = credential.IdleExpiresAtUtc;
        record.RevokedAtUtc = credential.RevokedAtUtc;
        record.RotationVersion = credential.RotationVersion;
    }

    public async Task<int> RevokeActiveForUserAsync(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var active = await _db.PlatformDeviceRecoveryCredentials
            .Where(c => c.UserId == userId.Value && c.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in active)
        {
            record.RevokedAtUtc = utcNow;
        }

        return active.Count;
    }

    public async Task<int> RevokeActiveForUserAndDeviceAsync(
        PlatformUserId userId,
        string installationDeviceId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var normalizedDeviceId = installationDeviceId.Trim();
        var active = await _db.PlatformDeviceRecoveryCredentials
            .Where(c =>
                c.UserId == userId.Value
                && c.InstallationDeviceId == normalizedDeviceId
                && c.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in active)
        {
            record.RevokedAtUtc = utcNow;
        }

        return active.Count;
    }

    private static PlatformDeviceRecoveryCredential ToDomain(PlatformDeviceRecoveryCredentialRecord record) =>
        PlatformDeviceRecoveryCredential.Rehydrate(
            PlatformDeviceRecoveryCredentialId.From(record.Id),
            PlatformUserId.From(record.UserId),
            record.InstallationDeviceId,
            record.TokenHash,
            record.SecurityStampAtIssue,
            record.CreatedAtUtc,
            record.LastUsedAtUtc,
            record.IdleExpiresAtUtc,
            record.AbsoluteExpiresAtUtc,
            record.RevokedAtUtc,
            record.RotationVersion);

    private static PlatformDeviceRecoveryCredentialRecord ToRecord(PlatformDeviceRecoveryCredential credential) =>
        new()
        {
            Id = credential.Id.Value,
            UserId = credential.UserId.Value,
            InstallationDeviceId = credential.InstallationDeviceId,
            TokenHash = credential.TokenHash,
            SecurityStampAtIssue = credential.SecurityStampAtIssue,
            CreatedAtUtc = credential.CreatedAtUtc,
            LastUsedAtUtc = credential.LastUsedAtUtc,
            IdleExpiresAtUtc = credential.IdleExpiresAtUtc,
            AbsoluteExpiresAtUtc = credential.AbsoluteExpiresAtUtc,
            RevokedAtUtc = credential.RevokedAtUtc,
            RotationVersion = credential.RotationVersion
        };
}
