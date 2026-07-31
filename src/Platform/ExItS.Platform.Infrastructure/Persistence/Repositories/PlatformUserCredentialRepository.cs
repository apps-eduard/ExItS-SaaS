using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformUserCredentialRepository : IPlatformUserCredentialRepository
{
    private readonly PlatformDbContext _db;

    public PlatformUserCredentialRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformUserCredential?> GetByUserIdAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformUserCredentials.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PlatformUserCredential credential, CancellationToken cancellationToken = default)
    {
        _db.PlatformUserCredentials.Add(ToRecord(credential));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformUserCredential credential, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformUserCredentials
            .FirstOrDefaultAsync(c => c.UserId == credential.UserId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CredentialNotFound,
                "Platform User credential was not found.");
        }

        Apply(credential, record);
    }

    private static PlatformUserCredential ToDomain(PlatformUserCredentialRecord record) =>
        PlatformUserCredential.Rehydrate(
            PlatformUserId.From(record.UserId),
            record.PasswordHash,
            record.PasswordHashAlgorithm,
            record.SecurityStamp,
            record.PasswordChangedAtUtc,
            record.EmailVerifiedAtUtc,
            record.FailedAccessCount,
            record.LockoutEndUtc,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    private static PlatformUserCredentialRecord ToRecord(PlatformUserCredential credential) =>
        new()
        {
            UserId = credential.UserId.Value,
            PasswordHash = credential.PasswordHash,
            PasswordHashAlgorithm = credential.PasswordHashAlgorithm,
            SecurityStamp = credential.SecurityStamp,
            PasswordChangedAtUtc = credential.PasswordChangedAtUtc,
            EmailVerifiedAtUtc = credential.EmailVerifiedAtUtc,
            FailedAccessCount = credential.FailedAccessCount,
            LockoutEndUtc = credential.LockoutEndUtc,
            CreatedAtUtc = credential.CreatedAtUtc,
            UpdatedAtUtc = credential.UpdatedAtUtc
        };

    private static void Apply(PlatformUserCredential credential, PlatformUserCredentialRecord record)
    {
        record.PasswordHash = credential.PasswordHash;
        record.PasswordHashAlgorithm = credential.PasswordHashAlgorithm;
        record.SecurityStamp = credential.SecurityStamp;
        record.PasswordChangedAtUtc = credential.PasswordChangedAtUtc;
        record.EmailVerifiedAtUtc = credential.EmailVerifiedAtUtc;
        record.FailedAccessCount = credential.FailedAccessCount;
        record.LockoutEndUtc = credential.LockoutEndUtc;
        record.UpdatedAtUtc = credential.UpdatedAtUtc;
    }
}
