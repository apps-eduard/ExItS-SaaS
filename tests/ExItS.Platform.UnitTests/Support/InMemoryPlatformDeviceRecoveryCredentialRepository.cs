using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryPlatformDeviceRecoveryCredentialRepository : IPlatformDeviceRecoveryCredentialRepository
{
    private readonly List<PlatformDeviceRecoveryCredential> _credentials = [];

    public Task<PlatformDeviceRecoveryCredential?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_credentials.FirstOrDefault(c =>
            string.Equals(c.TokenHash, tokenHash, StringComparison.Ordinal)));

    public Task<PlatformDeviceRecoveryCredential?> GetActiveByUserAndDeviceAsync(
        PlatformUserId userId,
        string installationDeviceId,
        CancellationToken cancellationToken = default)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var match = _credentials
            .Where(c =>
                c.UserId == userId
                && c.InstallationDeviceId == installationDeviceId.Trim()
                && c.RevokedAtUtc is null
                && c.IdleExpiresAtUtc > utcNow
                && c.AbsoluteExpiresAtUtc > utcNow)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefault();
        return Task.FromResult(match);
    }

    public Task AddAsync(PlatformDeviceRecoveryCredential credential, CancellationToken cancellationToken = default)
    {
        _credentials.Add(credential);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PlatformDeviceRecoveryCredential credential, CancellationToken cancellationToken = default)
    {
        var index = _credentials.FindIndex(c => c.Id == credential.Id);
        if (index >= 0)
        {
            _credentials[index] = credential;
        }

        return Task.CompletedTask;
    }

    public Task<int> RevokeActiveForUserAsync(
        PlatformUserId userId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        foreach (var credential in _credentials.Where(c => c.UserId == userId && c.RevokedAtUtc is null))
        {
            credential.Revoke(utcNow);
            count++;
        }

        return Task.FromResult(count);
    }

    public Task<int> RevokeActiveForUserAndDeviceAsync(
        PlatformUserId userId,
        string installationDeviceId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var normalized = installationDeviceId.Trim();
        var count = 0;
        foreach (var credential in _credentials.Where(c =>
                     c.UserId == userId
                     && c.InstallationDeviceId == normalized
                     && c.RevokedAtUtc is null))
        {
            credential.Revoke(utcNow);
            count++;
        }

        return Task.FromResult(count);
    }
}
