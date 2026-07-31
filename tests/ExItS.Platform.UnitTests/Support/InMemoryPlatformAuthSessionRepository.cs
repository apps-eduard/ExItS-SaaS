using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformAuthSessionRepository : IPlatformAuthSessionRepository
{
    private readonly Dictionary<Guid, PlatformAuthSession> _byId = new();

    public Task<PlatformAuthSession?> GetByIdAsync(
        PlatformAuthSessionId sessionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.TryGetValue(sessionId.Value, out var session) ? session : null);

    public Task<PlatformAuthSession?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.Values.FirstOrDefault(s => s.TokenHash == tokenHash));

    public Task AddAsync(PlatformAuthSession session, CancellationToken cancellationToken = default)
    {
        _byId[session.Id.Value] = session;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformAuthSession session, CancellationToken cancellationToken = default)
    {
        _byId[session.Id.Value] = session;
        return Task.CompletedTask;
    }

    public Task<int> RevokeAllActiveForUserAsync(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var session in _byId.Values.Where(s => s.UserId == userId && s.IsActive(utcNow)).ToList())
        {
            session.Revoke(utcNow);
            count++;
        }

        return Task.FromResult(count);
    }

    public Task<int> ClearSelectedOrganizationAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var session in _byId.Values
                     .Where(s => s.UserId == userId && s.SelectedOrganizationId == organizationId)
                     .ToList())
        {
            session.ClearSelectedOrganization();
            count++;
        }

        return Task.FromResult(count);
    }

    public Task<int> ClearSelectedOrganizationForOrganizationAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var session in _byId.Values
                     .Where(s => s.SelectedOrganizationId == organizationId)
                     .ToList())
        {
            session.ClearSelectedOrganization();
            count++;
        }

        return Task.FromResult(count);
    }
}
