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

    public async Task<PlatformUserId?> FindUserIdByVerifiedRecoveryEmailAsync(
        string normalizedRecoveryEmail,
        CancellationToken cancellationToken = default)
    {
        var email = normalizedRecoveryEmail.Trim().ToLowerInvariant();
        var userId = await _db.PlatformUserCredentials.AsNoTracking()
            .Where(c => c.RecoveryNormalizedEmail == email && c.RecoveryEmailVerifiedAtUtc != null)
            .Select(c => (Guid?)c.UserId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return userId is Guid id ? PlatformUserId.From(id) : null;
    }

    public async Task<bool> IsRecoveryEmailInUseAsync(
        string normalizedRecoveryEmail,
        PlatformUserId? excludingUserId,
        CancellationToken cancellationToken = default)
    {
        var email = normalizedRecoveryEmail.Trim().ToLowerInvariant();
        var query = _db.PlatformUserCredentials.AsNoTracking()
            .Where(c => c.RecoveryNormalizedEmail == email && c.RecoveryEmailVerifiedAtUtc != null);
        if (excludingUserId is not null)
        {
            query = query.Where(c => c.UserId != excludingUserId.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
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
            record.UpdatedAtUtc,
            record.PendingRecoveryNormalizedEmail,
            record.RecoveryNormalizedEmail,
            record.RecoveryEmailVerifiedAtUtc,
            record.RecoveryEmailPromptSkippedAtUtc);

    private static PlatformUserCredentialRecord ToRecord(PlatformUserCredential credential) =>
        new()
        {
            UserId = credential.UserId.Value,
            PasswordHash = credential.PasswordHash,
            PasswordHashAlgorithm = credential.PasswordHashAlgorithm,
            SecurityStamp = credential.SecurityStamp,
            PasswordChangedAtUtc = credential.PasswordChangedAtUtc,
            EmailVerifiedAtUtc = credential.EmailVerifiedAtUtc,
            PendingRecoveryNormalizedEmail = credential.PendingRecoveryNormalizedEmail,
            RecoveryNormalizedEmail = credential.RecoveryNormalizedEmail,
            RecoveryEmailVerifiedAtUtc = credential.RecoveryEmailVerifiedAtUtc,
            RecoveryEmailPromptSkippedAtUtc = credential.RecoveryEmailPromptSkippedAtUtc,
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
        record.PendingRecoveryNormalizedEmail = credential.PendingRecoveryNormalizedEmail;
        record.RecoveryNormalizedEmail = credential.RecoveryNormalizedEmail;
        record.RecoveryEmailVerifiedAtUtc = credential.RecoveryEmailVerifiedAtUtc;
        record.RecoveryEmailPromptSkippedAtUtc = credential.RecoveryEmailPromptSkippedAtUtc;
        record.FailedAccessCount = credential.FailedAccessCount;
        record.LockoutEndUtc = credential.LockoutEndUtc;
        record.UpdatedAtUtc = credential.UpdatedAtUtc;
    }
}
