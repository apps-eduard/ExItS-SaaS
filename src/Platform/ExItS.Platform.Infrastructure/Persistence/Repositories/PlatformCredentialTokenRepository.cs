using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformCredentialTokenRepository : IPlatformCredentialTokenRepository
{
    private readonly PlatformDbContext _db;

    public PlatformCredentialTokenRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformCredentialToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformCredentialTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PlatformCredentialToken token, CancellationToken cancellationToken = default)
    {
        _db.PlatformCredentialTokens.Add(ToRecord(token));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformCredentialToken token, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformCredentialTokens
            .FirstOrDefaultAsync(t => t.Id == token.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Credential token was not found.");
        }

        record.ConsumedAtUtc = token.ConsumedAtUtc;
        record.ExpiresAtUtc = token.ExpiresAtUtc;
    }

    public async Task InvalidateActiveForUserAsync(
        PlatformUserId userId,
        PlatformCredentialTokenPurpose purpose,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var purposeName = purpose.ToString();
        var active = await _db.PlatformCredentialTokens
            .Where(t => t.UserId == userId.Value
                        && t.Purpose == purposeName
                        && t.ConsumedAtUtc == null
                        && t.ExpiresAtUtc > utcNow)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in active)
        {
            record.ConsumedAtUtc = utcNow;
            if (record.ExpiresAtUtc > utcNow)
            {
                record.ExpiresAtUtc = utcNow;
            }
        }
    }

    private static PlatformCredentialToken ToDomain(PlatformCredentialTokenRecord record) =>
        PlatformCredentialToken.Rehydrate(
            PlatformCredentialTokenId.From(record.Id),
            PlatformUserId.From(record.UserId),
            Enum.Parse<PlatformCredentialTokenPurpose>(record.Purpose),
            record.TokenHash,
            record.CreatedAtUtc,
            record.ExpiresAtUtc,
            record.ConsumedAtUtc);

    private static PlatformCredentialTokenRecord ToRecord(PlatformCredentialToken token) =>
        new()
        {
            Id = token.Id.Value,
            UserId = token.UserId.Value,
            Purpose = token.Purpose.ToString(),
            TokenHash = token.TokenHash,
            CreatedAtUtc = token.CreatedAtUtc,
            ExpiresAtUtc = token.ExpiresAtUtc,
            ConsumedAtUtc = token.ConsumedAtUtc
        };
}
