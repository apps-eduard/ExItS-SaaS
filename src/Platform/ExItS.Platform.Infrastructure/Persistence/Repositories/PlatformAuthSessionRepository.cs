using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformAuthSessionRepository : IPlatformAuthSessionRepository
{
    private readonly PlatformDbContext _db;

    public PlatformAuthSessionRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformAuthSession?> GetByIdAsync(
        PlatformAuthSessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformAuthSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<PlatformAuthSession?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformAuthSessions.AsNoTracking()
            .FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public Task AddAsync(PlatformAuthSession session, CancellationToken cancellationToken = default)
    {
        _db.PlatformAuthSessions.Add(ToRecord(session));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformAuthSession session, CancellationToken cancellationToken = default)
    {
        var record = await _db.PlatformAuthSessions
            .FirstOrDefaultAsync(s => s.Id == session.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.SessionInvalid,
                "Platform auth session was not found.");
        }

        record.ExpiresAtUtc = session.ExpiresAtUtc;
        record.LastActivityAtUtc = session.LastActivityAtUtc;
        record.RevokedAtUtc = session.RevokedAtUtc;
        record.SelectedOrganizationId = session.SelectedOrganizationId?.Value;
    }

    public async Task<int> RevokeAllActiveForUserAsync(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var active = await _db.PlatformAuthSessions
            .Where(s => s.UserId == userId.Value && s.RevokedAtUtc == null && s.ExpiresAtUtc > utcNow)
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

    public async Task<int> ClearSelectedOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _db.PlatformAuthSessions
            .Where(s =>
                s.UserId == userId.Value
                && s.SelectedOrganizationId == organizationId.Value
                && s.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in sessions)
        {
            record.SelectedOrganizationId = null;
        }

        return sessions.Count;
    }

    public async Task<int> ClearSelectedOrganizationForOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _db.PlatformAuthSessions
            .Where(s => s.SelectedOrganizationId == organizationId.Value && s.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var record in sessions)
        {
            record.SelectedOrganizationId = null;
        }

        return sessions.Count;
    }

    private static PlatformAuthSession ToDomain(PlatformAuthSessionRecord record) =>
        PlatformAuthSession.Rehydrate(
            PlatformAuthSessionId.From(record.Id),
            PlatformUserId.From(record.UserId),
            record.TokenHash,
            record.SecurityStampAtIssue,
            record.CreatedAtUtc,
            record.ExpiresAtUtc,
            record.AbsoluteExpiresAtUtc,
            record.LastActivityAtUtc,
            record.RevokedAtUtc,
            record.IpAddress,
            record.UserAgentHash,
            record.SelectedOrganizationId is null
                ? null
                : PlatformOrganizationId.From(record.SelectedOrganizationId.Value));

    private static PlatformAuthSessionRecord ToRecord(PlatformAuthSession session) =>
        new()
        {
            Id = session.Id.Value,
            UserId = session.UserId.Value,
            TokenHash = session.TokenHash,
            SecurityStampAtIssue = session.SecurityStampAtIssue,
            CreatedAtUtc = session.CreatedAtUtc,
            ExpiresAtUtc = session.ExpiresAtUtc,
            AbsoluteExpiresAtUtc = session.AbsoluteExpiresAtUtc,
            LastActivityAtUtc = session.LastActivityAtUtc,
            RevokedAtUtc = session.RevokedAtUtc,
            IpAddress = session.IpAddress,
            UserAgentHash = session.UserAgentHash,
            SelectedOrganizationId = session.SelectedOrganizationId?.Value
        };
}
