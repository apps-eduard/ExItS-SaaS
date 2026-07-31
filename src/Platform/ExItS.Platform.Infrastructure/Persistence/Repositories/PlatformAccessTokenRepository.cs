using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformAccessTokenRepository : IPlatformAccessTokenRepository
{
    private readonly PlatformDbContext _db;

    public PlatformAccessTokenRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformAccessToken?> GetByIdAsync(
        PlatformAccessTokenId tokenId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformAccessTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tokenId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<PlatformAccessToken?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformAccessTokens.AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PlatformAccessToken token, CancellationToken cancellationToken = default)
    {
        _db.PlatformAccessTokens.Add(ToRecord(token));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformAccessToken token, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformAccessTokens
            .FirstOrDefaultAsync(t => t.Id == token.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.AccessTokenInvalid,
                "Platform access token was not found.");
        }

        record.ExpiresAtUtc = token.ExpiresAtUtc;
        record.RevokedAtUtc = token.RevokedAtUtc;
        record.OrganizationId = token.OrganizationId?.Value;
        record.ProductCode = token.ProductCode;
    }

    public async Task<int> RevokeAllActiveForUserAsync(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var active = await _db.PlatformAccessTokens
            .Where(t => t.UserId == userId.Value && t.RevokedAtUtc == null && t.ExpiresAtUtc > utcNow)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in active)
        {
            record.RevokedAtUtc = utcNow;
            if (record.ExpiresAtUtc > utcNow)
            {
                record.ExpiresAtUtc = utcNow;
            }
        }

        return active.Count;
    }

    public async Task<int> ClearOrganizationBindingAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _db.PlatformAccessTokens
            .Where(t =>
                t.UserId == userId.Value
                && t.OrganizationId == organizationId.Value
                && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in tokens)
        {
            record.OrganizationId = null;
            record.ProductCode = null;
        }

        return tokens.Count;
    }

    public async Task<int> ClearOrganizationBindingForOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _db.PlatformAccessTokens
            .Where(t => t.OrganizationId == organizationId.Value && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in tokens)
        {
            record.OrganizationId = null;
            record.ProductCode = null;
        }

        return tokens.Count;
    }

    private static PlatformAccessToken ToDomain(PlatformAccessTokenRecord record) =>
        PlatformAccessToken.Rehydrate(
            PlatformAccessTokenId.From(record.Id),
            PlatformUserId.From(record.UserId),
            record.TokenHash,
            record.SecurityStampAtIssue,
            record.CreatedAtUtc,
            record.ExpiresAtUtc,
            record.RevokedAtUtc,
            record.OrganizationId is null
                ? null
                : PlatformOrganizationId.From(record.OrganizationId.Value),
            record.ProductCode);

    private static PlatformAccessTokenRecord ToRecord(PlatformAccessToken token) =>
        new()
        {
            Id = token.Id.Value,
            UserId = token.UserId.Value,
            TokenHash = token.TokenHash,
            SecurityStampAtIssue = token.SecurityStampAtIssue,
            CreatedAtUtc = token.CreatedAtUtc,
            ExpiresAtUtc = token.ExpiresAtUtc,
            RevokedAtUtc = token.RevokedAtUtc,
            OrganizationId = token.OrganizationId?.Value,
            ProductCode = token.ProductCode
        };
}
